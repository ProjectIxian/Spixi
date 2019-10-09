using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Threading.Tasks;
using System.IO;
using Android.Content;
using Plugin.LocalNotifications;
using Android.Content.Res;
using Xamarin.Forms;
using Com.OneSignal;
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

            ZXing.Net.Mobile.Forms.Android.Platform.Init();
            ZXing.Mobile.MobileBarcodeScanner.Initialize(this.Application);

            LoadApplication(new App());
            LocalNotificationsImplementation.NotificationIconId = Resource.Drawable.statusicon;
            IXICore.CryptoManager.initLib(new CryptoLibs.BouncyCastleAndroid());

            OneSignal.Current.StartInit("44d96ce3-5d33-4e8b-997d-d1ad786b96a1")
                .EndInit();

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
            if (permissions[0] == "android.permission.CAMERA")
            {
                // prevent ZXing related crash on denied
                if (grantResults[0] == Permission.Denied)
                {
                    Xamarin.Forms.Application.Current.MainPage.Navigation.PopAsync();
                    Xamarin.Forms.Application.Current.MainPage.DisplayAlert("Permission error", "Permission '" + permissions[0] + "' must be allowed to use this feature.", "OK");
                    return;
                }
            }
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

