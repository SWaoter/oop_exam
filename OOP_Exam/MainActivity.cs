using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Places;
using Xamarin.Essentials;
using SQLite;
using Environment = System.Environment;
using ILocationListener = Android.Gms.Location.ILocationListener;
using Location = Xamarin.Essentials.Location;

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
        public bool Availability { get; set; }

        public MarkerDataTable(string title, double lat, double lon, double dist, bool availability)
        {
            Title = title;
            Lat = lat;
            Lon = lon;
            Dist = dist;
            Availability = availability;
        }
        public MarkerDataTable() { }
    }

    [Table("Items2")]
    public class UserEmailString
    {
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int ID { get; set; }
        public string Email { get; set; }

        public UserEmailString(string email)
        {
            Email = email;
        }
        public UserEmailString() { }
    }
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback, Android.Locations.ILocationListener
    {
        public GoogleMap MMap { get; private set; }
        private readonly List<MarkerDataTable> _markerList = new List<MarkerDataTable>();
        private readonly List<Circle> _circlesList = new List<Circle>();
        private Marker _currentLocationMarker;
        private const double Delta = 0.001;
        private string user_email;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "userDataBD_fin.db3");
            var db = new SQLiteConnection(dbPath);
            var table = db.Table<UserEmailString>();
            db.CreateTable<UserEmailString>();
            foreach (var s in table)
            {
                user_email = s.Email;
                break;
            }
            
            if (string.IsNullOrEmpty(user_email))
            {
                user_email = Intent.GetStringExtra("user_email");
                if (user_email == null)
                {
                    StartActivity(typeof(UserEmail));
                    this.Finish();
                }
                else
                {
                    var email = new UserEmailString(user_email);
                    db.Insert(email);
                }
            }
            

            PrepareDatabase();
            SetUpMap();
        }

        private void SetUpMap()
        {
            if (MMap == null) FragmentManager.FindFragmentById<MapFragment>(Resource.Id.map).GetMapAsync(this);
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
                if (e.Marker.Title != "Your Position")
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
                            string geoAddress =
                                await GetPlaceName(e.Marker.Position.Latitude, e.Marker.Position.Longitude);
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
                }
                e.Marker.ShowInfoWindow();
            };
            MMap = googleMap;
            MMap.UiSettings.ZoomControlsEnabled = true;

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
            LocationManager locationManager = (LocationManager)GetSystemService(Context.LocationService);
            locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 2000, 1, this);
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
            MMap.MoveCamera(newCameraUpdate);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void RemoveCircleByLatLong(LatLng latLng)
        {
            foreach (var t in _circlesList.Where(t => Math.Abs(t.Center.Longitude - latLng.Longitude) < Delta 
                                                     && Math.Abs(t.Center.Latitude - latLng.Latitude) < Delta))
            {
                t.Remove();
                break;
            }
            foreach (var t in _markerList.Where(t => Math.Abs(t.Lon - latLng.Longitude) < Delta
                                                      && Math.Abs(t.Lat - latLng.Latitude) < Delta))
            {
                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "markerDataBD_fin.db3");
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
                "markerDataBD_fin.db3");
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
            var data = new MarkerDataTable(title, lat, lon, dist, false);
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "markerDataBD_fin.db3");
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
            foreach (var marker in _markerList.Where(marker => Math.Abs(marker.Lat - lat) < Delta && Math.Abs(marker.Lon - lon) < Delta))
                    return $"{marker.Dist}";
            return "";
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            MarkerOptions currentPos = new MarkerOptions();
            currentPos.SetPosition(new LatLng(location.Latitude, location.Longitude));
            currentPos.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            currentPos.SetTitle("Your Position");
            _currentLocationMarker?.Remove();
            if(MMap != null)
                _currentLocationMarker = MMap.AddMarker(currentPos);
            foreach (var marker in _markerList)
            {
                if (Location.CalculateDistance(new Location(location.Latitude, location.Longitude),
                        new Location(marker.Lat, marker.Lon), DistanceUnits.Kilometers) < marker.Dist)
                {
                    if (!marker.Availability)
                    {
                        try
                        {
                            MailAddress from = new MailAddress("oop.exam.xam@gmail.com", "XamarinApp");
                            MailAddress to = new MailAddress(user_email);
                            MailMessage m = new MailMessage(from, to)
                            {
                                Subject = "Location Tracking",
                                Body = $"<h2>You are near place named: {marker.Title}<h2>" +
                                       $"Your latitude is {location.Latitude}, your longitude is {location.Longitude}",
                                IsBodyHtml = true
                            };
                            var smtp = new SmtpClient("smtp.gmail.com")
                            {
                                Credentials = new NetworkCredential("oop.exam.xam", ""),
                                EnableSsl = true
                            }; // password is not pulic.
                            smtp.Send(m);
                            marker.Availability = true;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
                else if (marker.Availability)
                    marker.Availability = false;
            }
        }
        public void OnProviderDisabled(string locationProvider)
        {
            // called when the user disables the provider
        }

        public void OnProviderEnabled(string locationProvider)
        {
            // called when the user enables the provider
        }

        public void OnStatusChanged(string locationProvider, Availability status, Bundle extras)
        {
            // called when the status of the provider changes (there are a variety of reasons for this)
        }
    }
}
