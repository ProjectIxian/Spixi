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

[assembly: Dependency(typeof(PowerManager_Android))]


public class PowerManager_Android : IPowerManager
{
    PowerManager.WakeLock wl = null;

    public bool AquireLock()
    {
        if (wl == null)
        {
            PowerManager pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
            wl = pm.NewWakeLock(WakeLockFlags.ScreenDim, "Spixi");

            wl.Acquire();
            return true;
        }
        return false;
    }

    public bool ReleaseLock()
    {
        if (wl != null)
        {
            wl.Release();
            return true;
        }

        return false;
    }
}