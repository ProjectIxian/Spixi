using System;
using Android.Content;
using Android.Runtime;
using Android.Webkit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using IXICore.Meta;
using Android.Util;
using Android.Views.InputMethods;
using AndroidX.Core.View.InputMethod;
using Android.OS;
using IXICore;
using System.Linq;
using System.Text.RegularExpressions;

[assembly: ExportRenderer(typeof(Xamarin.Forms.WebView), typeof(SPIXI.Droid.Renderers.SpixiWebviewRenderer))]

namespace SPIXI.Droid.Renderers
{
    public class SpixiWebView : Android.Webkit.WebView, InputConnectionCompat.IOnCommitContentListener
    {
        public SpixiWebView(Context context) : base(context)
        {
        }

        public SpixiWebView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SpixiWebView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        [Obsolete]
        public SpixiWebView(Context context, IAttributeSet attrs, int defStyleAttr, bool privateBrowsing) : base(context, attrs, defStyleAttr, privateBrowsing)
        {
        }

        public SpixiWebView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected SpixiWebView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public bool OnCommitContent(InputContentInfoCompat inputContentInfo, int flags, Bundle opts)
        {
            bool permission_requested = false;
            bool processed = false;

            // read and display inputContentInfo asynchronously
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.NMr1
                && (flags & InputConnectionCompat.InputContentGrantReadUriPermission) != 0)
            {
                try
                {
                    inputContentInfo.RequestPermission();
                    permission_requested = true;
                }
                catch (Exception)
                {
                    return processed;
                }
            }

            if (inputContentInfo.LinkUri != null)
            {
                string url = inputContentInfo.LinkUri.ToString();

                Page p = App.Current.MainPage.Navigation.NavigationStack.Last();
                if (p != null && p.GetType() == typeof(SingleChatPage))
                {
                    string rx_pattern = @"^https://[A-Za-z0-9]+\.(tenor|giphy)\.com/[A-Za-z0-9_/=%\?\-\.\&]+$";

                    if (Regex.IsMatch(url, rx_pattern))
                    {
                        ((SingleChatPage)p).onSend(url);
                        processed = true;
                    }
                }
            }else
            {
                Logging.error("Error adding keyboard content, LinkUri is null");
            }

            if(permission_requested)
            {
                inputContentInfo.ReleasePermission();
            }

            return processed;
        }

        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            var ic = base.OnCreateInputConnection(outAttrs);
            outAttrs.ImeOptions = outAttrs.ImeOptions | Android.Views.InputMethods.ImeFlags.NoPersonalizedLearning;

            if (ic != null)
            {
                // allow image insertion
                EditorInfoCompat.SetContentMimeTypes(outAttrs, new string[] { "image/gif" });

                ic = InputConnectionCompat.CreateWrapper(ic, outAttrs, this);
            }

            return ic;
        }
    }


    // Implemented our own SPIXI webview android renderer to handle known issues in Xamarin Forms 3.3
    // Partially based on  https://github.com/xamarin/Xamarin.Forms/pull/3780/commits/29735675a674a5c972459aa3a7fe88b10772ea55
    // More information about the issue https://github.com/xamarin/Xamarin.Forms/issues/3778

    public class SpixiWebviewRenderer : WebViewRenderer
    {
        public SpixiWebviewRenderer(Context context) : base(context)
        {

        }

        protected override Android.Webkit.WebView CreateNativeControl()
        {
            return new SpixiWebView(Context);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);

            if (Control != null)
            {
                var _webview = new SpixiWebViewClient(this);
                Control.Settings.AllowFileAccess = true;
                Control.SetWebViewClient(_webview);
            }
        }

        class SpixiWebViewClient : WebViewClient
        {
            WebNavigationResult _navigationResult = WebNavigationResult.Success;
            SpixiWebviewRenderer _renderer;

            public SpixiWebViewClient(SpixiWebviewRenderer renderer)
            {
                if (renderer == null)
                    throw new ArgumentNullException("renderer");
                _renderer = renderer;
            }

            // Override the page started call to trigger the Navigating callback
            /*public override void OnPageStarted(global::Android.Webkit.WebView view, string url, Bitmap favicon)
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
            }*/
            

            // Hackish solution to the Xamarin ERR_UNKNOWN_URL_SCHEME issue plaguing the latest releases
            // TODO: find a better way to handle the Navigating event without triggering a page load
            [Obsolete]
            public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView view, string url)
            {
                var args = new WebNavigatingEventArgs(WebNavigationEvent.NewPage, new UrlWebViewSource { Url = url }, url);
                try
                {
                    _renderer.ElementController.SendNavigating(args);
                }catch(Exception e)
                {
                    Logging.error("Exception in should override url loading {0}", e);
                }
                if (args.Cancel)
                {
                    return true;
                }
                return false;
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

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                    _renderer = null;
            }
        }
    }

}