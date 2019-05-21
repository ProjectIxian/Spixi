using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using SPIXI.iOS.Services;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using SPIXI;
//using SPIXI.Notifications;

namespace SPIXI.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {

        BackgroundTaskService backgroundTaskService;

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // Ask the user for permission to get notifications on iOS 10.0+
                UNUserNotificationCenter.Current.RequestAuthorization(
                        UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                        (approved, error) => { });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                // Ask the user for permission to get notifications on iOS 8.0+
                var settings = UIUserNotificationSettings.GetSettingsForTypes(
                        UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                        new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }
            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalMinimum);

            global::Xamarin.Forms.Forms.Init();
            global::ZXing.Net.Mobile.Forms.iOS.Platform.Init();
            LoadApplication(new App());

            prepareBackgroundService();

            return base.FinishedLaunching(app, options);
        }

        void prepareBackgroundService()
        {
            /*
            MessagingCenter.Subscribe<StartMessage>(this, "StartMessage", async message => {
                backgroundTaskService = new BackgroundTaskService();
                await backgroundTaskService.Start();
            });

            MessagingCenter.Subscribe<StopMessage>(this, "StopMessage", message => {
                backgroundTaskService.Stop();
            });
            */
        }

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            // Check for new data, and display it
  
            // Inform system of fetch results
            completionHandler(UIBackgroundFetchResult.NewData);
        }
    }
}
