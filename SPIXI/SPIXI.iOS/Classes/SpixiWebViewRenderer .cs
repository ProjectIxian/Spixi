using Foundation;
using IXICore.Meta;
using SPIXI.iOS.Classes;
using SPIXI.Meta;
using System.IO;
using UIKit;
using WebKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]
namespace SPIXI.iOS.Classes
{
    internal class SpixiWebViewRenderer : Xamarin.Forms.Platform.iOS.WkWebViewRenderer
    {
        public override WKNavigation LoadRequest(NSUrlRequest request)
        {
            return LoadFileUrl(request.Url, new NSUrl(Path.Combine(Config.spixiUserFolder, "html"), true));
        }

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            
            var view = NativeView as WebKit.WKWebView;
            
            if (view != null)
            {
                view.Configuration.DataDetectorTypes = WebKit.WKDataDetectorTypes.None;
                view.ScrollView.ScrollEnabled = false;
                view.ScrollView.Bounces = false;
            }
        }
    }
}