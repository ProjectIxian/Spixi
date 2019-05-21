using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using System.IO;
using Android.Content;
using Xamarin.Forms;
using SPIXI.Droid.Services;
using Plugin.LocalNotifications;
//using SPIXI.Notifications;

namespace SPIXI.Droid
{
    [Activity(Label = "SPIXI", Icon = "@drawable/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        // Field, property, and method for Picture Picker
        public static readonly int PickImageId = 1000;

        public TaskCompletionSource<Stream> PickImageTaskCompletionSource { set; get; }

        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
            LocalNotificationsImplementation.NotificationIconId = Resource.Drawable.statusicon;
            DLT.CryptoManager.initLib(new CryptoLibs.BouncyCastleAndroid());

            prepareBackgroundService();
        }

        void prepareBackgroundService()
        {
 /*           MessagingCenter.Subscribe<StartMessage>(this, "StartMessage", message => {
                var intent = new Intent(this, typeof(BackgroundTaskService));
                StartService(intent);
            });

            MessagingCenter.Subscribe<StopMessage>(this, "StopMessage", message => {
                var intent = new Intent(this, typeof(BackgroundTaskService));
                StopService(intent);
            });*/
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            if (requestCode == PickImageId)
            {
                if ((resultCode == Result.Ok) && (intent != null))
                {
                    Android.Net.Uri uri = intent.Data;
                    Stream stream = ContentResolver.OpenInputStream(uri);

                    // Set the Stream as the completion of the Task
                    PickImageTaskCompletionSource.SetResult(stream);
                }
                else
                {
                    PickImageTaskCompletionSource.SetResult(null);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            ZXing.Net.Mobile.Forms.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


    }
}

