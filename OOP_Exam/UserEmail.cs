using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace OOP_Exam
{
    [Activity(Label = "UserEmail")]
    public class UserEmail : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.user_email);
            EditText user_email = FindViewById<EditText>(Resource.Id.edit_email);
            user_email.KeyPress += (sender, e) => {
                e.Handled = false;
                if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
                {
                    Toast.MakeText(this, user_email.Text, ToastLength.Short).Show();
                    e.Handled = true;
                }
            };
            Button confirmButton = FindViewById<Button>(Resource.Id.confirm_e);
            confirmButton.Click += (sender, e) =>
            {
                try
                {
                    MailAddress test = new MailAddress($"{user_email.Text}");
                    var intent = new Intent(this, typeof(MainActivity));
                    intent.PutExtra("user_email", user_email.Text);
                    StartActivity(intent);
                }
                catch (Exception)
                {
                    Toast.MakeText(this, "Input valid email", ToastLength.Short).Show();
                }
            };
            // Create your application here
        }
    }
}