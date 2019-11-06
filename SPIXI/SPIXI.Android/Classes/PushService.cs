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
using SPIXI.Interfaces;
using SPIXI.Droid.Classes;
using Xamarin.Forms;
using Com.OneSignal;

[assembly: Dependency(typeof(PushService_Android))]

public class PushService_Android : IPushService
{
    public void initialize()
    {
        OneSignal.Current.StartInit("44d96ce3-5d33-4e8b-997d-d1ad786b96a1").EndInit();
    }

    public void setTag(string tag)
    {
        OneSignal.Current.SendTag("ixi", tag);
    }


}