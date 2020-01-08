using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace OOP_Exam
{
    [Activity(Label = "MarkerAddingActivity")]
    public class MarkerAddingActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.marker_adding);
            string geocodeAddress = Intent.GetStringExtra("geocode");
            TextView adress = FindViewById<TextView>(Resource.Id.adresstxt);
            adress.Text = geocodeAddress;
            EditText name = FindViewById<EditText>(Resource.Id.edittext1);
            name.KeyPress += (sender, e) => {
                e.Handled = false;
                if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
                {
                    Toast.MakeText(this, name.Text, ToastLength.Short).Show();
                    e.Handled = true;
                }
            };
            EditText rad = FindViewById<EditText>(Resource.Id.edittext2);
            rad.KeyPress += (sender, e) => {
                e.Handled = false;
                if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
                {
                    Toast.MakeText(this, rad.Text, ToastLength.Short).Show();
                    e.Handled = true;
                }
            };
            Button add = FindViewById<Button>(Resource.Id.add_m);
            add.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MainActivity));
                intent.PutExtra("marker_name", name.Text);
                intent.PutExtra("marker_rad", rad.Text);
                intent.PutExtra("lat", Intent.GetStringExtra("lat"));
                intent.PutExtra("lon", Intent.GetStringExtra("lon"));
                StartActivity(intent);
            };
            Button close = FindViewById<Button>(Resource.Id.cancel_m);
            close.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            };
        }
    }
}