using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SPIXI.Interfaces;
using SPIXI.Droid.Classes;
using Xamarin.Forms;

[assembly: Dependency(typeof(BaseUrl_Android))]


public class BaseUrl_Android : IBaseUrl
{
    public string Get()
    {
        return "file:///android_asset/";
    }
}