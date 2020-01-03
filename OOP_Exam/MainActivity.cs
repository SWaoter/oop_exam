using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
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
            googleMap.MapClick += (sender, e) =>
            {
                using (var markerOption = new MarkerOptions())
                {
                    markerOption.SetPosition(e.Point);
                    googleMap.AddMarker(markerOption);
                    CircleOptions circleOptions = new CircleOptions();
                    circleOptions.InvokeCenter(e.Point);
                    circleOptions.InvokeRadius(1000);
                    circleOptions.InvokeStrokeColor(-65536);
                    var circle = googleMap.AddCircle(circleOptions);
                    _circlesList.Add(circle);
                    ExpandData("test", circle.Center.Latitude, circle.Center.Longitude, 1000);
                    
                }
            };
            googleMap.MarkerClick += (sender, e) =>
            {
                RemoveCircleByLatLong(e.Marker.Position);
                e.Marker.Remove();
            };
            _mMap = googleMap;
            _mMap.UiSettings.ZoomControlsEnabled = true;

            ChangeCameraPositionToCurrentLocation();


            foreach (var t in _markerList)
            {
                var markerOption2 = new MarkerOptions();
                markerOption2.SetPosition(new LatLng(t.Lat, t.Lon));
                googleMap.AddMarker(markerOption2);
                var circleOptions2 = new CircleOptions();
                circleOptions2.InvokeCenter(new LatLng(t.Lat, t.Lon));
                circleOptions2.InvokeRadius(t.Dist);
                circleOptions2.InvokeStrokeColor(-65536);
                var circle = googleMap.AddCircle(circleOptions2);
                _circlesList.Add(circle);
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
                    "markerDataBD.db3");
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
                "markerDataBD.db3");
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
                "markerDataBD.db3");
            var db = new SQLiteConnection(dbPath);
            db.Insert(data);
        }
    }
}