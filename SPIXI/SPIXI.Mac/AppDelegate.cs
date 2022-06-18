using AppKit;
using Foundation;
using SPIXI.Interfaces;
using SPIXI.VoIP;
using Xamarin.Forms;
using Xamarin.Forms.Platform.MacOS;

namespace SPIXI.Mac
{
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        NSWindow window;
        public AppDelegate()
        {
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;

            var rect = new CoreGraphics.CGRect(200, 1000, 450, 700);
            window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
            window.Title = "Spixi";
            window.TitleVisibility = NSWindowTitleVisibility.Hidden;
        }

        public override NSWindow MainWindow
        {
            get { return window; }
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            Forms.Init();
            LoadApplication(App.Instance());

            // Manually register interfaces on macOS
            DependencyService.RegisterSingleton<IPlatformUtils>(new PlatformUtils());
            DependencyService.RegisterSingleton<IPowerManager>(new PowerManager_Mac());
            DependencyService.RegisterSingleton<IPushService>(new PushService_Mac());
            DependencyService.RegisterSingleton<ISpixiCodecInfo>(new SpixiCodecInfo());
            DependencyService.RegisterSingleton<ISpixiPermissions>(new SpixiPermissions());



            base.DidFinishLaunching(notification);
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }

}
