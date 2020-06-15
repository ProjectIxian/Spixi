using UIKit;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PowerManager_iOS))]


public class PowerManager_iOS : IPowerManager
{
    public bool AquireLock(string lock_type = "screenDim")
    {
        switch (lock_type)
        {
            case "screenDim":
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.IdleTimerDisabled = true;
                });
                return true;
            case "partial":
                return true;
            case "proximityScreenOff":
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    UIDevice.CurrentDevice.ProximityMonitoringEnabled = true;
                });
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
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.IdleTimerDisabled = false;
                });
                return true;
            case "partial":
                return true;
            case "proximityScreenOff":
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    UIDevice.CurrentDevice.ProximityMonitoringEnabled = false;
                });
                return true;
            case "wifi":
                return true;
        }
        return false;
    }
}