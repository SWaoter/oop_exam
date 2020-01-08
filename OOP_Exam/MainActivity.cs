using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Places;
using Xamarin.Essentials;
using SQLite;
using Environment = System.Environment;

namespace OOP_Exam
{
    [Table("Items")]
    public class MarkerDataTable
    {
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int ID { get; set; }
        public string Title { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Dist { get; set; }

        public MarkerDataTable(string title, double lat, double lon, double dist)
        {
            Title = title;
            Lat = lat;
            Lon = lon;
            Dist = dist;
        }
        public MarkerDataTable() { }
    }
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        private GoogleMap _mMap;
        private readonly List<MarkerDataTable> _markerList = new List<MarkerDataTable>();
        private readonly List<Circle> _circlesList = new List<Circle>();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);
            PrepareDatabase();
            SetUpMap();
        }

        private void SetUpMap()
        {
            if (_mMap == null) FragmentManager.FindFragmentById<MapFragment>(Resource.Id.map).GetMapAsync(this);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            googleMap.MapClick += async (sender, e) =>
            {
                string geocodeAddress = await GetPlaceName(e.Point.Latitude, e.Point.Longitude);
                var intent = new Intent(this, typeof(MarkerAddingActivity));
                intent.PutExtra("geocode", geocodeAddress);
                string lat = $"{e.Point.Latitude}";
                string lon = $"{e.Point.Longitude}";
                intent.PutExtra("lat", lat);
                intent.PutExtra("lon", lon);
                StartActivity(intent);
            };
            googleMap.MarkerClick += (sender, e) =>
            {
                Button test = FindViewById<Button>(Resource.Id.hlpbutton);
                var menu = new PopupMenu(this, test);
                menu.Inflate(Resource.Menu.marker_click_menu);
                menu.MenuItemClick += async (s1, arg1) =>
                {
                    if (arg1.Item.ToString() == "Delete")
                    {
                        RemoveCircleByLatLong(e.Marker.Position);
                        e.Marker.Remove();
                    }

                    if (arg1.Item.ToString() == "Show Info")
                    {
                        string geoAddress = await GetPlaceName(e.Marker.Position.Latitude, e.Marker.Position.Longitude);
                        var intent = new Intent(this, typeof(MarkerInfoActivity));
                        intent.PutExtra("geocode", geoAddress);
                        intent.PutExtra("m_title", e.Marker.Title);
                        intent.PutExtra("m_rad",
                            GetRadByLatLon(e.Marker.Position.Latitude, e.Marker.Position.Longitude));
                        StartActivity(intent);
                    }
                };
                menu.DismissEvent += (s2, arg2) => { };
                menu.Show();
                e.Marker.ShowInfoWindow();
            };
            _mMap = googleMap;
            _mMap.UiSettings.ZoomControlsEnabled = true;

            ChangeCameraPositionToCurrentLocation();
            
            foreach (var t in _markerList)
            {
                var markerOption2 = new MarkerOptions();
                markerOption2.SetPosition(new LatLng(t.Lat, t.Lon));
                markerOption2.SetTitle(t.Title);
                googleMap.AddMarker(markerOption2);
                var circleOptions2 = new CircleOptions();
                circleOptions2.InvokeCenter(new LatLng(t.Lat, t.Lon));
                circleOptions2.InvokeRadius(t.Dist);
                circleOptions2.InvokeStrokeColor(-65536);
                var circle = googleMap.AddCircle(circleOptions2);
                _circlesList.Add(circle);
            }

            try
            {
                using (var markerOption = new MarkerOptions())
                {
                    var point = new LatLng(double.Parse(Intent.GetStringExtra("lat")), double.Parse( Intent.GetStringExtra("lon")));
                    markerOption.SetPosition(point);
                    markerOption.SetTitle(Intent.GetStringExtra("marker_name"));
                    var marker = googleMap.AddMarker(markerOption);
                    marker.ShowInfoWindow();
                    CircleOptions circleOptions = new CircleOptions();
                    circleOptions.InvokeCenter(point);
                    circleOptions.InvokeRadius(double.Parse(Intent.GetStringExtra("marker_rad")));
                    circleOptions.InvokeStrokeColor(-65536);
                    var circle = googleMap.AddCircle(circleOptions);
                    _circlesList.Add(circle);
                    ExpandData(marker.Title, circle.Center.Latitude, circle.Center.Longitude, double.Parse(Intent.GetStringExtra("marker_rad")));
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async void ChangeCameraPositionToCurrentLocation()
        {
            var request = new GeolocationRequest(GeolocationAccuracy.High);
            var location = await Geolocation.GetLocationAsync(request);
            LatLng currentLatLng = new LatLng(location.Latitude, location.Longitude);
            CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
            builder.Target(currentLatLng);
            builder.Zoom(10);
            CameraPosition newCameraPosition = builder.Build();
            CameraUpdate newCameraUpdate = CameraUpdateFactory.NewCameraPosition(newCameraPosition);
            _mMap.MoveCamera(newCameraUpdate);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void RemoveCircleByLatLong(LatLng latLng)
        {
            foreach (var t in _circlesList.Where(t => Math.Abs(t.Center.Longitude - latLng.Longitude) < 0.001 
                                                     && Math.Abs(t.Center.Latitude - latLng.Latitude) < 0.001))
            {
                t.Remove();
                break;
            }
            foreach (var t in _markerList.Where(t => Math.Abs(t.Lon - latLng.Longitude) < 0.001
                                                      && Math.Abs(t.Lat - latLng.Latitude) < 0.001))
            {
                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "markerDataBD2.db3");
                var db = new SQLiteConnection(dbPath);
                db.Delete<MarkerDataTable>(t.ID);
                _markerList.Remove(t);
                break;
            }
        }

        private void PrepareDatabase()
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "markerDataBD2.db3");
            var db = new SQLiteConnection(dbPath);
            var table = db.Table<MarkerDataTable>();
            db.CreateTable<MarkerDataTable>();
            foreach (var s in table)
            {
                _markerList.Add(s);
            }
        }

        private void ExpandData(string title, double lat, double lon, double dist)
        {
            var data = new MarkerDataTable(title, lat, lon, dist);
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "markerDataBD2.db3");
            var db = new SQLiteConnection(dbPath);
            db.Insert(data);
        }

        private async Task<string> GetPlaceName(double lat, double lon)
        {
            string geocodeAddress = "";
            try
            {
                var placemarks = await Geocoding.GetPlacemarksAsync(lat, lon);

                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    geocodeAddress =
                        $"AdminArea:       {placemark.AdminArea}\n" +
                        $"CountryCode:     {placemark.CountryCode}\n" +
                        $"CountryName:     {placemark.CountryName}\n" +
                        $"FeatureName:     {placemark.FeatureName}\n" +
                        $"Locality:        {placemark.Locality}\n" +
                        $"PostalCode:      {placemark.PostalCode}\n" +
                        $"SubAdminArea:    {placemark.SubAdminArea}\n" +
                        $"SubLocality:     {placemark.SubLocality}\n" +
                        $"SubThoroughfare: {placemark.SubThoroughfare}\n" +
                        $"Thoroughfare:    {placemark.Thoroughfare}\n";

                    Console.WriteLine(geocodeAddress);
                }
            }
            catch (Exception)
            {
                geocodeAddress = "";
            }

            return geocodeAddress;
        }

        private string GetRadByLatLon(double lat, double lon)
        {
            foreach (var marker in _markerList.Where(marker => Math.Abs(marker.Lat - lat) < 0.001 && Math.Abs(marker.Lon - lon) < 0.001))
                    return $"{marker.Dist}";
            return "";
        }
    }
}