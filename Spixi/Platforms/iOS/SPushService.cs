using Foundation;
using IXICore.Meta;
using OneSignalSDK.DotNet;
using OneSignalSDK.DotNet.Core.Debug;
using UIKit;
using UserNotifications;

namespace Spixi
{
    public class SPushService
    {
        public static void initialize()
        {
            OneSignal.Debug.LogLevel = LogLevel.WARN;
            OneSignal.Debug.AlertLevel = LogLevel.NONE;

            OneSignal.Initialize(SPIXI.Meta.Config.oneSignalAppId);

            OneSignal.Notifications.RequestPermissionAsync(true);
        }

        public static void setTag(string tag)
        {
            OneSignal.User.AddTag("ixi", tag);
        }

        public static void clearNotifications()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(16, 0))
                {
                    // For iOS 16+, use UNUserNotificationCenter
                    UNUserNotificationCenter.Current.SetBadgeCount(0, (err) =>
                    {
                        if (err != null)
                        {
                            Logging.warn("Set badge count failed");
                            Logging.warn(err.ToString());
                        }
                    });
                }
                else
                {
                    // For older versions, use UIApplication
                    UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
                }
            });
        }

        public static void showLocalNotification(string title, string message, string data)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var content = new UNMutableNotificationContent
                {
                    Title = title,
                    Body = message,
                    Badge = 1,
                    Sound = UNNotificationSound.Default,
                    UserInfo = new NSDictionary(nameof(data), data)
                };

                var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(1, false);
                var request = UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), content, trigger);

                UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
                {
                    if (err != null)
                    {
                        Logging.warn("Local notification add request failed");
                        Logging.warn(err.ToString());
                    }
                });
            });
        }
    }
}
