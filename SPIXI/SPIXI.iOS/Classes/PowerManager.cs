using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using SPIXI.Interfaces;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;

[assembly: Dependency(typeof(PowerManager_iOS))]


public class PowerManager_iOS : IPowerManager
{
    public bool AquireLock(string lock_type = "screenDim")
    {
        switch (lock_type)
        {
            case "screenDim":
                UIApplication.SharedApplication.IdleTimerDisabled = true;
                return true;
            case "partial":
                return true;
            case "proximityScreenOff":
                UIDevice.CurrentDevice.ProximityMonitoringEnabled = true;
                return true;
            case "wifi":
                return true;
        }
        return false;
    }

    public bool ReleaseLock(string lock_type = "screenDim")
    {
        switch (lock_type)
        {
            case "screenDim":
                UIApplication.SharedApplication.IdleTimerDisabled = false;
                return true;
            case "partial":
                return true;
            case "proximityScreenOff":
                UIDevice.CurrentDevice.ProximityMonitoringEnabled = false;
                return true;
            case "wifi":
                return true;
        }
        return false;
    }
}