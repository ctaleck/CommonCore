﻿#if __ANDROID__
using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Xamarin.Forms.CommonCore;
using Xamarin.Forms;
using Net = Android.Net;

[assembly: Xamarin.Forms.Dependency(typeof(MapNavigate))]
namespace Xamarin.Forms.CommonCore
{
    public class MapNavigate : IMapNavigate
    {
        public void NavigateWithAddress(string address)
        {
            try
            {
                var activity = (Activity)Xamarin.Forms.Forms.Context;
                address = System.Net.WebUtility.UrlEncode(address);
                var gmmIntentUri = Net.Uri.Parse("google.navigation:q=" + address);
                var mapIntent = new Intent(Intent.ActionView, gmmIntentUri);
                mapIntent.SetPackage("com.google.android.apps.maps");
                activity.StartActivity(mapIntent);
            }
            catch (Exception ex)
            {
                Toast toast = Toast.MakeText(Xamarin.Forms.Forms.Context, "This activity is not supported", ToastLength.Long);
                toast.Show();
            }

        }

        public void NavigateLatLong(double latitude, double longtitude)
        {

        }
    }
}
#endif
