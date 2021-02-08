using System;
using Foundation;
using SPIXI.iOS.Services;
using UIKit;
using UserNotifications;
using SPIXI.Meta;
using System.IO;
using MediaPlayer;
using SPIXI.VoIP;
using AVFoundation;
using IXICore.Meta;
using System.Threading;
using SPIXI.Lang;

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

            prepareStorage();

            NSNotificationCenter.DefaultCenter.AddObserver(MPMusicPlayerController.VolumeDidChangeNotification, onVolumeChanged);

            SpixiLocalization.addCustomString("Platform", "Xamarin-iOS");

            LoadApplication(App.Instance());

            prepareBackgroundService();

            return base.FinishedLaunching(app, options);
        }

        void onVolumeChanged(NSNotification notification)
        {
            VoIPManager.setVolume(AVAudioSession.SharedInstance().OutputVolume);
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

        private void prepareStorage()
        {
            string source_html = Path.Combine(NSBundle.MainBundle.BundlePath, "html");
            string dest_html = Path.Combine(Config.spixiUserFolder, "html");

            if (!Directory.Exists(dest_html))
            {
                Directory.CreateDirectory(dest_html);
            }

            prepareSymbolicLinks(new DirectoryInfo(source_html), new DirectoryInfo(dest_html));
        }


        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            // Check for new data, and display it
  
            // Inform system of fetch results
            completionHandler(UIBackgroundFetchResult.NewData);
        }

        // Cleans up and links contents of the source directory to target directory.
        private static void prepareSymbolicLinks(DirectoryInfo source, DirectoryInfo target)
        {
            var fm = new NSFileManager();
            fm.ChangeCurrentDirectory(target.FullName);

            NSError ns_error = new NSError();

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                var tmp_path = Path.Combine(target.FullName, dir.Name);
                if (Directory.Exists(tmp_path))
                {
                    Directory.Delete(tmp_path, true);
                }
                if (File.Exists(tmp_path))
                {
                    File.Delete(tmp_path);
                }
                fm.CreateSymbolicLink(dir.Name, dir.FullName, out ns_error);
            }

            foreach (FileInfo file in source.GetFiles())
            {
                var tmp_path = Path.Combine(target.FullName, file.Name);
                if (Directory.Exists(tmp_path))
                {
                    Directory.Delete(tmp_path, true);
                }
                if (File.Exists(tmp_path))
                {
                    File.Delete(tmp_path);
                }
                fm.CreateSymbolicLink(file.Name, file.FullName, out ns_error);
            }
        }

        public override void ReceiveMemoryWarning(UIApplication application)
        {
            App.Instance().onLowMemory();
        }

        public override void WillTerminate(UIApplication uiApplication)
        {
            IxianHandler.shutdown();
            while (IxianHandler.status != NodeStatus.stopped)
            {
                Thread.Sleep(10);
            }
            base.WillTerminate(uiApplication);
        }
    }
}
