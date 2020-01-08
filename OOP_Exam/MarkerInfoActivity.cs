using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace OOP_Exam
{
    [Activity(Label = "Marker_Info_Activity")]
    public class MarkerInfoActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.marker_info);
            string geocodeAddress = Intent.GetStringExtra("geocode");
            string title = Intent.GetStringExtra("m_title");
            string radius = Intent.GetStringExtra("m_rad");
            TextView title_v = FindViewById<TextView>(Resource.Id.ttext1);
            TextView radius_v = FindViewById<TextView>(Resource.Id.ttext2);
            TextView address_v = FindViewById<TextView>(Resource.Id.ttext3);
            Button cancel = FindViewById<Button>(Resource.Id.ret_b);
            cancel.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            };
            title_v.Text = title;
            radius_v.Text = radius;
            address_v.Text = geocodeAddress;
        }
    }
}