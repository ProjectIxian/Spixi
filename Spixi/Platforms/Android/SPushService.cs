using Android.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneSignalSDK.DotNet.Core;
using OneSignalSDK.DotNet;
using AndroidX.Core.App;
using Android.Graphics;
using Android.Content;
using Android.OS;
using Com.OneSignal.Android;
using IXICore.Meta;
using SPIXI;

namespace Spixi
{
    public class SPushService
    {
        const string channelId = "default";
        const string channelName = "Default";
        const string channelDescription = "Spixi local notifications channel.";
        const int pendingIntentId = 0;

        static bool channelInitialized = false;
        static int messageId = -1;
        static NotificationManager manager;
        public const string TitleKey = "title";
        public const string MessageKey = "message";

        public static void initialize()
        {
            OneSignalSDK.DotNet.OneSignal.Default.LogLevel = LogLevel.VERBOSE;
            OneSignalSDK.DotNet.OneSignal.Default.AlertLevel = LogLevel.NONE;


            OneSignalSDK.DotNet.OneSignal.Default.RequiresPrivacyConsent = true;

            OneSignalSDK.DotNet.OneSignal.Default.Initialize(SPIXI.Meta.Config.oneSignalAppId);
            OneSignalSDK.DotNet.OneSignal.Default.PromptForPushNotificationsWithUserResponse();

            OneSignalSDK.DotNet.OneSignal.Default.PrivacyConsent = true;

            OneSignalSDK.DotNet.OneSignal.Default.InAppMessagesArePaused = true;
            OneSignalSDK.DotNet.OneSignal.Default.InAppMessageWillDisplay += _inAppMessageWillDisplay;


            /// TODO
            /*OneSignal.Default.StartInit(SPIXI.Meta.Config.oneSignalAppId)
    .InFocusDisplaying(Com.OneSignal.Abstractions.OSInFocusDisplayOption.None)
    .HandleNotificationReceived(handleNotificationReceived)
    .HandleNotificationOpened(handleNotificationOpened)
    .EndInit();
            OneSignal.Current.SetLocationShared(false);*/
        }
        private static void _inAppMessageWillDisplay(InAppMessage message)
        {
            if (OfflinePushMessages.fetchPushMessages(true))
            {
                OneSignalSDK.DotNet.OneSignal.Default.ClearOneSignalNotifications();
            }
        }

        public static void setTag(string tag)
        {
            OneSignalSDK.DotNet.OneSignal.Default.SendTag("ixi", tag);
        }

        public static void clearNotifications()
        {
            var notificationManager = NotificationManagerCompat.From(Android.App.Application.Context);
            notificationManager.CancelAll();
        }

        public static void showLocalNotification(string title, string message, string data)
        {
            if (!channelInitialized)
            {
                CreateNotificationChannel();
            }

            messageId++;

            Intent intent = new Intent(Android.App.Application.Context, typeof(MainActivity));
            intent.PutExtra(TitleKey, title);
            intent.PutExtra(MessageKey, message);
            intent.PutExtra("fa", data);

            PendingIntent pendingIntent = PendingIntent.GetActivity(Android.App.Application.Context, pendingIntentId, intent, PendingIntentFlags.OneShot);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(Android.App.Application.Context, channelId)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetPriority(1)
                .SetLargeIcon(BitmapFactory.DecodeResource(Android.App.Application.Context.Resources, Resource.Drawable.statusicon))
                .SetSmallIcon(Resource.Drawable.statusicon)
                .SetDefaults((int)NotificationDefaults.Sound | (int)NotificationDefaults.Vibrate);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                builder.SetGroup("NEWMSGL");
            }


            var notification = builder.Build();
            manager.Notify(messageId, notification);
        }


        static void CreateNotificationChannel()
        {
            manager = (NotificationManager)Android.App.Application.Context.GetSystemService("notification");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannelGroup group = new NotificationChannelGroup("NEWMSGL", "New Message");
                manager.CreateNotificationChannelGroup(group);

                var channelNameJava = new Java.Lang.String(channelName);
                var channel = new NotificationChannel(channelId, channelNameJava, NotificationImportance.High)
                {
                    Description = channelDescription,
                    Group = "NEWMSGL"
                };
                manager.CreateNotificationChannel(channel);
            }

            channelInitialized = true;
        }

        static void handleNotificationReceived(OSNotification notification)
        {
            if (OfflinePushMessages.fetchPushMessages(true))
            {
                OneSignalSDK.DotNet.OneSignal.Default.ClearOneSignalNotifications();
            }
        }

        static void handleNotificationOpened(OSNotificationOpenedResult inNotificationOpenedDelegate)
        {
           /* if (inNotificationOpenedDelegate.Notification.Payload.additionalData.ContainsKey("fa"))
            {
                var fa = inNotificationOpenedDelegate.Notification.payload.additionalData["fa"];
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
            }*/

        }
    }

}
