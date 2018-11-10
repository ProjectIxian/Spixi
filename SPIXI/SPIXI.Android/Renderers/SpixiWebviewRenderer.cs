using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using System.Threading.Tasks;
using Android.Graphics;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Xamarin.Forms.WebView), typeof(SPIXI.Droid.Renderers.SpixiWebviewRenderer))]

namespace SPIXI.Droid.Renderers
{
    // Implemented our own SPIXI webview android renderer to handle known issues in Xamarin Forms 3.3
    // Partially based on  https://github.com/xamarin/Xamarin.Forms/pull/3780/commits/29735675a674a5c972459aa3a7fe88b10772ea55
    // More information about the issue https://github.com/xamarin/Xamarin.Forms/issues/3778

    public class SpixiWebviewRenderer : WebViewRenderer
    {
        IWebViewController ElementController => Element;

        public SpixiWebviewRenderer(Context context) : base(context)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                Control.SetWebViewClient(new WebClient(this));
            }
        }

        class WebClient : WebViewClient
        {
            WebNavigationResult _navigationResult = WebNavigationResult.Success;
            SpixiWebviewRenderer _renderer;

            public WebClient(SpixiWebviewRenderer renderer)
            {
                if (renderer == null)
                    throw new ArgumentNullException("renderer");
                _renderer = renderer;
            }

            // Override the page started call to trigger the Navigating callback
            public override void OnPageStarted(global::Android.Webkit.WebView view, string url, Bitmap favicon)
            {
                if (_renderer.Element == null || url == WebViewRenderer.AssetBaseUrl)
                    return;
                var args = new WebNavigatingEventArgs(WebNavigationEvent.NewPage, new UrlWebViewSource { Url = url }, url);
                _renderer.ElementController.SendNavigating(args);
                _navigationResult = WebNavigationResult.Success;
                _renderer.UpdateCanGoBackForward();

                if (args.Cancel)
                {
                    _renderer.Control.StopLoading();
                }
                else
                {
                    base.OnPageStarted(view, url, favicon);
                }
            }

            public override void OnPageFinished(global::Android.Webkit.WebView view, string url)
            {
                if (_renderer.Element == null || url == WebViewRenderer.AssetBaseUrl)
                    return;
                var source = new UrlWebViewSource { Url = url };
                var args = new WebNavigatedEventArgs(WebNavigationEvent.NewPage, source, url, _navigationResult);
                _renderer.ElementController.SendNavigated(args);
                _renderer.UpdateCanGoBackForward();
                base.OnPageFinished(view, url);
            }


        }
    }

}