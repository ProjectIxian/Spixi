using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Threading.Tasks;
using System.IO;
using Android.Content;
using Plugin.LocalNotifications;
using Xamarin.Forms;
using SPIXI.Interfaces;

namespace SPIXI.Droid
{
    [Activity(Label = "SPIXI", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleInstance)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        // Field, property, and method for Picture Picker
        public static readonly int PickImageId = 1000;

        public TaskCompletionSource<Stream> PickImageTaskCompletionSource { set; get; }

        internal static MainActivity Instance { get; private set; }


        protected override void OnCreate(Bundle bundle)
        {
            Instance = this;

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            
            ZXing.Net.Mobile.Forms.Android.Platform.Init();
            ZXing.Mobile.MobileBarcodeScanner.Initialize(this.Application);

            string fa = Intent.GetStringExtra("fa");
            if (fa != null)
            {
                Intent.RemoveExtra("fa");
                App.startingScreen = fa;
            }else
            {
                App.startingScreen = "";
            }
            
            // Initialize Push Notification service
            DependencyService.Get<IPushService>().initialize();

            // CLear notifications
            DependencyService.Get<IPushService>().clearNotifications();

            LoadApplication(App.Instance());

            LocalNotificationsImplementation.NotificationIconId = Resource.Drawable.statusicon;
            IXICore.CryptoManager.initLib(new CryptoLibs.BouncyCastleAndroid());
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

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            // Handle local notification tap
            string fa = intent.GetStringExtra("fa");
            if (fa != null)
            {
                HomePage.Instance().onChat(fa, null);
            }

            // CLear notifications
            DependencyService.Get<IPushService>().clearNotifications();
        }
    }
}

