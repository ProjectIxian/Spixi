using SPIXI.Interfaces;
using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using SPIXI;
using IXICore.Meta;

[assembly: Dependency(typeof(PushService_iOS))]

public class PushService_iOS : IPushService
{
    public void initialize()
    {
        OneSignal.Current.StartInit(SPIXI.Meta.Config.oneSignalAppId)
            .InFocusDisplaying(Com.OneSignal.Abstractions.OSInFocusDisplayOption.None)
            .HandleNotificationReceived(handleNotificationReceived)
            .HandleNotificationOpened(handleNotificationOpened)
            .EndInit();
    }

    public void setTag(string tag)
    {
        OneSignal.Current.SendTag("ixi", tag);
    }

    public void clearNotifications()
    {
        UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
    }

    public void showLocalNotification(string title, string message, string data)
    {
        Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
        {
            var notification = new UILocalNotification();
            notification.FireDate = NSDate.FromTimeIntervalSinceNow(1);
            notification.AlertAction = title;
            notification.AlertBody = message;
            notification.ApplicationIconBadgeNumber = 1;
            notification.SoundName = UILocalNotification.DefaultSoundName;
            UIApplication.SharedApplication.ScheduleLocalNotification(notification);
        });
    }


    static void handleNotificationReceived(OSNotification notification)
    {
        OneSignal.Current.ClearAndroidOneSignalNotifications();
        OfflinePushMessages.fetchPushMessages(true);
    }

    static void handleNotificationOpened(OSNotificationOpenedResult inNotificationOpenedDelegate)
    {
        if (inNotificationOpenedDelegate.notification.payload.additionalData.ContainsKey("fa"))
        {
            var fa = inNotificationOpenedDelegate.notification.payload.additionalData["fa"];
            if (fa != null)
            {
                try
                {
                    App.startingScreen = Convert.ToString(fa);
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in handleNotificationOpened: {0}", e);
                }
            }
        }
    }
}