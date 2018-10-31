using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using SPIXI.iOS.Classes;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]
namespace SPIXI.iOS.Classes
{
    internal class SpixiWebViewRenderer : Xamarin.Forms.Platform.iOS.WebViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            var view = NativeView as UIKit.UIWebView;

            if (view != null)
            {
                view.ScrollView.ScrollEnabled = true;
                view.ScrollView.Bounces = false;
                //view.SetNeedsLayout();
                //view.ScrollView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
            }
        }


    }
}