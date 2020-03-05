using SPIXI.Interfaces;
using Xamarin.Forms;
using Com.OneSignal;
using Android.Support.V4.App;
using Android.App;
using Android.OS;
using AndroidApp = Android.App.Application;
using Android.Content;
using SPIXI.Droid;
using Android.Graphics;
using Com.OneSignal.Abstractions;
using SPIXI;
using IXICore.Meta;
using System.Threading;
using System;

[assembly: Dependency(typeof(PushService_Android))]

public class PushService_Android : IPushService
{
    const string channelId = "default";
    const string channelName = "Default";
    const string channelDescription = "Spixi local notifications channel.";
    const int pendingIntentId = 0;

    bool channelInitialized = false;
    int messageId = -1;
    NotificationManager manager;
    public const string TitleKey = "title";
    public const string MessageKey = "message";

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
        var notificationManager = NotificationManagerCompat.From(Android.App.Application.Context);
        notificationManager.CancelAll();
    }

    public void showLocalNotification(string title, string message, string data)
    {
        MainActivity activity = MainActivity.Instance;


        if (!channelInitialized)
        {
            CreateNotificationChannel();
        }

        messageId++;

        Intent intent = new Intent(AndroidApp.Context, typeof(MainActivity));
        intent.PutExtra(TitleKey, title);
        intent.PutExtra(MessageKey, message);

        PendingIntent pendingIntent = PendingIntent.GetActivity(AndroidApp.Context, pendingIntentId, intent, PendingIntentFlags.OneShot);

        Bundle extras = new Bundle();
        extras.PutString("fa", data);

        NotificationCompat.Builder builder = new NotificationCompat.Builder(AndroidApp.Context, channelId)
            .SetContentIntent(pendingIntent)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetGroup("NEWMSG")
            .AddExtras(extras)
            .SetLargeIcon(BitmapFactory.DecodeResource(AndroidApp.Context.Resources, Resource.Drawable.statusicon))
            .SetSmallIcon(Resource.Drawable.statusicon)
            .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate);

        var notification = builder.Build();
        manager.Notify(messageId, notification);
    }

    void CreateNotificationChannel()
    {
        manager = (NotificationManager)AndroidApp.Context.GetSystemService(AndroidApp.NotificationService);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channelNameJava = new Java.Lang.String(channelName);
            var channel = new NotificationChannel(channelId, channelNameJava, NotificationImportance.Default)
            {
                Description = channelDescription
            };
            manager.CreateNotificationChannel(channel);
        }

        channelInitialized = true;
    }

    static void handleNotificationReceived(OSNotification notification)
    {
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
                    HomePage.Instance.onChat(Convert.ToString(fa), null);
                }catch(Exception e)
                {
                    Logging.error("Exception occured in handleNotificationOpened: {0}", e);
                }
            }
        }
    }
}