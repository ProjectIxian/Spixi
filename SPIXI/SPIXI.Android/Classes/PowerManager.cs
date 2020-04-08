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
using Android.Net.Wifi;

[assembly: Dependency(typeof(PowerManager_Android))]


public class PowerManager_Android : IPowerManager
{
    Dictionary<string, object> wakeLocks = new Dictionary<string, object>();


    public bool AquireLock(string lock_type = "screenDim")
    {
        switch(lock_type)
        {
            case "screenDim":
                if (wakeLocks.ContainsKey(lock_type))
                {
                    return false;
                }
                PowerManager pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
                var pm_lock = pm.NewWakeLock(WakeLockFlags.ScreenDim, "Spixi");
                pm_lock.Acquire();
                wakeLocks.Add(lock_type, pm_lock);
                return true;
            case "partial":
                if (wakeLocks.ContainsKey(lock_type))
                {
                    return false;
                }
                pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
                pm_lock = pm.NewWakeLock(WakeLockFlags.ScreenDim, "Spixi");
                pm_lock.Acquire();
                wakeLocks.Add(lock_type, pm_lock);
                return true;
            case "wifi":
                if (wakeLocks.ContainsKey(lock_type))
                {
                    return false;
                }
                WifiManager wm = (WifiManager)Android.App.Application.Context.GetSystemService(Context.WifiService);
                var wm_lock = wm.CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Spixi");
                wm_lock.Acquire();
                wakeLocks.Add(lock_type, wm_lock);
                return true;
        }
        return false;
    }

    public bool ReleaseLock(string lock_type = "screenDim")
    {
        if (wakeLocks.ContainsKey(lock_type))
        {
            switch(lock_type)
            {
                case "screenDim":
                case "partial":
                    PowerManager.WakeLock pm_lock = (PowerManager.WakeLock)wakeLocks[lock_type];
                    pm_lock.Release();
                    break;
                case "wifi":
                    WifiManager.WifiLock wm_lock = (WifiManager.WifiLock)wakeLocks[lock_type];
                    wm_lock.Release();
                    break;
            }
            wakeLocks.Remove(lock_type);
            return true;
        }

        return false;
    }
}