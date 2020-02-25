using SPIXI.Interfaces;
using Xamarin.Forms;
using Com.OneSignal;
using Android.Support.V4.App;

[assembly: Dependency(typeof(PushService_Android))]

public class PushService_Android : IPushService
{
    public void initialize()
    {
        OneSignal.Current.StartInit(SPIXI.Meta.Config.oneSignalAppId)
            .InFocusDisplaying(Com.OneSignal.Abstractions.OSInFocusDisplayOption.None)
            .EndInit();
    }

    public void setTag(string tag)
    {
        OneSignal.Current.SendTag("ixi", tag);
    }

    public void clearNotifications()
    {
        var notificationManager = NotificationManagerCompat.From(Android.App.Application.Context);
        notificationManager.CancelAll();
    }
}