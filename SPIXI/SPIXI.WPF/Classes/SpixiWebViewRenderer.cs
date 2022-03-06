using CefSharp;
using CefSharp.Handler;
using CefSharp.SchemeHandler;
using CefSharp.Wpf;
using IXICore.Meta;
using SPIXI.WPF.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]

namespace SPIXI.WPF.Classes
{
    public class CustomProtocolSchemeHandler : ResourceHandler
    {
        private string frontendFolderPath;

        public CustomProtocolSchemeHandler()
        {
            frontendFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./html/");            
        }

        // Process request and craft response
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            var uri = new Uri(request.Url);
            var fileName = uri.AbsolutePath;
            var requestedFilePath = frontendFolderPath + fileName;

            if (File.Exists(requestedFilePath))
            {
                byte[] bytes = File.ReadAllBytes(requestedFilePath);
                Stream = new MemoryStream(bytes);

                var fileExtension = Path.GetExtension(fileName);
                MimeType = GetMimeType(fileExtension);

                callback.Continue();
                return CefReturnValue.Continue;
            }

            callback.Dispose();
            return CefReturnValue.Cancel;
        }
    }

    public class CustomRequestHandler : RequestHandler
    {
        SpixiWebViewRenderer _renderer;

        public CustomRequestHandler(SpixiWebViewRenderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            _renderer = renderer;
        }
        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            var navEvent = WebNavigationEvent.NewPage;
            var lastUrl = request.Url.ToString();
            var args = new WebNavigatingEventArgs(navEvent, new UrlWebViewSource { Url = lastUrl }, lastUrl);

            bool done = false;
            Device.BeginInvokeOnMainThread(() =>
            {
                _renderer.Element.SendNavigating(args);
                done = true;
            });

            while(!done)
            {
                Thread.Sleep(10);
            }

            if (args.Cancel)
                return true;

            return false;
        }

        protected override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }
    }

    public class SpixiWebViewRenderer : ViewRenderer<WebView, ChromiumWebBrowser>, IWebViewDelegate
    {
        bool _disposed;
        ChromiumWebBrowser webView = null;

        public SpixiWebViewRenderer() : base()
        {
        }

        protected async override void OnElementChanged(ElementChangedEventArgs<WebView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    UrlWebViewSource source = (UrlWebViewSource)e.NewElement.Source;
                    string src = source.Url;

                    var settings = new CefSettings();
                    settings.RegisterScheme(new CefCustomScheme
                    {
                        IsStandard = true,
                        SchemeName = "file",
                        DomainName = "spixi",
                        SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                            rootFolder: @".\\html",
                            hostName: "spixi",
                            defaultPage: source.Url // default
                        )
                    });
                    settings.UserAgent = "Spixi " + Cef.CefSharpVersion;

                    if (!Cef.IsInitialized)
                    {
                        Cef.Initialize(settings);
                    }
                    string aurl = "file://spixi/" + source.Url;
                    webView = new ChromiumWebBrowser(aurl);
                    webView.RequestHandler = new CustomRequestHandler(this);
                    webView.Visibility = System.Windows.Visibility.Visible;
                    webView.Loaded += onLoadFinished;
                    webView.LoadingStateChanged += onLoading;
                    //webView.LoadStarted += OnLoadStarted;
                    //webView.LoadFinished += OnLoadFinished;

                    Element.EvalRequested += OnEvalRequested;
                    Element.EvaluateJavaScriptRequested += OnEvaluateJavaScriptRequested;
                    //Element.GoBackRequested += OnGoBackRequested;
                    //Element.GoForwardRequested += OnGoForwardRequested;
                    //Element.ReloadRequested += OnReloadRequested;
                  
                    SetNativeControl(webView);
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                Element.EvalRequested -= OnEvalRequested;
                Element.EvaluateJavaScriptRequested -= OnEvaluateJavaScriptRequested;
                //Element.GoBackRequested -= OnGoBackRequested;
                //Element.GoForwardRequested -= OnGoForwardRequested;
                //Element.ReloadRequested -= OnReloadRequested;
                //Control.FrameLoadDelegate = null;
                //Control.PolicyDelegate = null;
                //webView.RequestContext.Dispose();
                webView.Dispose();
                
            }
            base.Dispose(disposing);
        }

        private void onLoadFinished(object o, EventArgs args)
        {

        }

        private void onLoading(object o, EventArgs args)
        {

        }

        void OnEvalRequested(object sender, EvalRequested eventArg)
        {
            if (webView != null)
                if (webView.CanExecuteJavascriptInMainFrame)
                {
                    webView.GetMainFrame().EvaluateScriptAsync(eventArg?.Script);
                }
        }

        async Task<string> OnEvaluateJavaScriptRequested(string script)
        {
            var tcr = new TaskCompletionSource<string>();
            var task = tcr.Task;

            Device.BeginInvokeOnMainThread(async () => {
                var result = await webView.GetMainFrame().EvaluateScriptAsync(script)
                 .ContinueWith(t =>
                 {
                     var res = (JavascriptResponse)t.Result;
                     return (string)res.ToString();
                 });
                });

            return await task.ConfigureAwait(false);
        }


        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == WebView.SourceProperty.PropertyName)
            {

            }
        }

        void IWebViewDelegate.LoadHtml(string html, string baseUrl)
        {

        }

        void IWebViewDelegate.LoadUrl(string url)
        {

        }
    }
}
