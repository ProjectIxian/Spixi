﻿using IXICore;
using IXICore.Meta;
using SPIXI.CustomApps;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.VoIP;
using Spixi;
#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace SPIXI
{
	public class SpixiContentPage : ContentPage
	{
        public bool CancelsTouchesInView = true;
        public bool pageLoaded = false;
        private Queue<string> messageQueue = new Queue<string>();
        protected WebView? _webView = null;
        public WebView WebView
        {
            get
            {
                return _webView!;
            }
        }

        public void loadPage(WebView web_view, string html_file_name)
        {
            pageLoaded = false;
            _webView = web_view;
            _webView.Source = generatePage(html_file_name);
            _webView.Navigated += webViewNavigated;
            _webView.Navigating += webViewNavigating;
        }

        private void webViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
#if WINDOWS
            if (_webView == null) return;
            CoreWebView2 coreWebView2 = (_webView.Handler.PlatformView as Microsoft.Maui.Platform.MauiWebView).CoreWebView2;
            coreWebView2.Settings.IsStatusBarEnabled = false;
#endif
        }

        private async void webViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (pageLoaded = await checkIfPageLoaded())
            {
                processMessageQueue();
            }
        }

        private void evaluateJavascript(string script)
        {
            if (_webView == null)
                return;

            MainThread.BeginInvokeOnMainThread(() => {
                _webView.EvaluateJavaScriptAsync("try{ " + script + " }catch(e){  }");
            });
        }

        private void processMessageQueue()
        {
            if (_webView == null)
                return;

            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                evaluateJavascript(message);
            }
        }

        public void sendMessage(string msg)
        {
            if (pageLoaded && _webView != null)
            {
                evaluateJavascript(msg);
            }
            else
            {
                messageQueue.Enqueue(msg);
            }
        }

        private async Task<bool> checkIfPageLoaded()
        {
            if (_webView == null)
                return false;

            var readyState = await _webView.EvaluateJavaScriptAsync("document.readyState");
            return readyState.Trim('\"') == "complete";
        }

        public virtual void reload()
        {
            if (_webView != null)
            {
                pageLoaded = false;
                _webView.Reload();
            }
        }

        public WebViewSource generatePage(string html_file_name)
        {
            if (OperatingSystem.IsAndroid())
            {
                var source = new HtmlWebViewSource();
                Stream stream = SPlatformUtils.getAsset(Path.Combine("html", html_file_name));
                source.BaseUrl = SPlatformUtils.getAssetsBaseUrl() + "html/";
                source.Html = SpixiLocalization.localizeHtml(stream);
                stream.Close();
                stream.Dispose();
                return source;
            }
            else
            {
                string assets_file_path = Path.Combine(SPlatformUtils.getAssetsPath(), "html", html_file_name);
                string localized_file_path = Path.Combine(SPlatformUtils.getHtmlPath(), "ll_" + html_file_name);
                SpixiLocalization.localizeHtml(assets_file_path, localized_file_path);
                return new UrlWebViewSource
                {
                    Url = SPlatformUtils.getHtmlBaseUrl() + "ll_" + html_file_name
                };
            }
        }


        public virtual void recalculateLayout()
        {

        }

        public Task displaySpixiAlert(string title, string message, string cancel)
        {
            try { 
                return DisplayAlert(title, message, cancel);
            }catch(Exception e)
            {
                Logging.error("Exception occured in displaySpixiAlert: " + e);
            }
            return null;
        }

        public void displayCallBar(byte[] session_id, string text, long call_started_time)
        {
            if (_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(this, "displayCallBar", Crypto.hashToString(session_id), text, "<div style='background:#de0a61;border-radius:16px;width:64px;height:64px;display:table-cell;vertical-align:middle;text-align:center;'><i class='fas fa-phone-slash'></i></div>", call_started_time.ToString());
        }

        public void hideCallBar()
        {
            if (_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(this, "hideCallBar");
        }

        public void displayAppRequests()
        {
            if(_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(this, "clearAppRequests");
            var app_pages = Node.customAppManager.getAppPages();
            lock (app_pages)
            {
                foreach (CustomAppPage page in app_pages.Values)
                {
                    if(page.accepted)
                    {
                        continue;
                    }
                    Friend f = FriendList.getFriend(page.hostUserAddress);
                    CustomApp app = Node.customAppManager.getApp(page.appId);
                    string text = string.Format(SpixiLocalization._SL("global-app-wants-to-use"), f.nickname, app.name);
                    Utils.sendUiCommand(this, "addAppRequest", Crypto.hashToString(page.sessionId), text, SpixiLocalization._SL("global-app-accept"), SpixiLocalization._SL("global-app-reject"));
                }
                if(VoIPManager.isInitiated())
                {
                    if(VoIPManager.currentCallAccepted)
                    {
                        if(VoIPManager.currentCallCalleeAccepted)
                        {
                            displayCallBar(VoIPManager.currentCallSessionId, SpixiLocalization._SL("global-call-in-call") + " - " + VoIPManager.currentCallContact.nickname, VoIPManager.currentCallStartedTime);
                        }else
                        {
                            displayCallBar(VoIPManager.currentCallSessionId, SpixiLocalization._SL("global-call-dialing") + " " + VoIPManager.currentCallContact.nickname + "...", 0);
                        }
                    }
                    else
                    {
                        Friend f = VoIPManager.currentCallContact;
                        string text = SpixiLocalization._SL("global-call-incoming") + " - " + f.nickname;
                        string accept_html = "<div style='background:#2fd63b;border-radius:16px;width:64px;height:64px;display:table-cell;vertical-align:middle;text-align:center;'><i class='fas fa-phone'></i></div>";
                        string reject_html = "<div style='background:#de0a61;border-radius:16px;width:64px;height:64px;display:table-cell;vertical-align:middle;text-align:center;'><i class='fas fa-phone-slash'></i></div>";
                        Utils.sendUiCommand(this, "addAppRequest", Crypto.hashToString(VoIPManager.currentCallSessionId), text, accept_html, reject_html);
                    }
                }
            }
        }

        public void onAppAccept(string session_id)
        {
            byte[] b_session_id = Crypto.stringToHash(session_id);
            if(VoIPManager.hasSession(b_session_id))
            {
                VoIPManager.acceptCall(b_session_id);
                return;
            }
            CustomAppPage app_page = Node.customAppManager.acceptAppRequest(b_session_id);
            if (app_page != null)
            {
                Navigation.PushAsync(app_page, Config.defaultXamarinAnimations);
            }// TODO else error?
        }

        public void onAppReject(string session_id)
        {
            byte[] b_session_id = Crypto.stringToHash(session_id);
            if (VoIPManager.hasSession(b_session_id))
            {
                VoIPManager.rejectCall(b_session_id);
                return;
            }
            Node.customAppManager.rejectAppRequest(b_session_id);
        }

        public virtual void updateScreen()
        {
            if (Node.refreshAppRequests)
            {
                displayAppRequests();
                Node.refreshAppRequests = false;
            }
        }

        public virtual void onResume()
        {

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Node.refreshAppRequests = true;
            updateScreen();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (!Navigation.NavigationStack.Contains(this))
            {
                pageLoaded = false;
                messageQueue.Clear();

                if (_webView != null)
                {
                    _webView.Navigated -= webViewNavigated;
                    _webView = null;
                }
            }
            
            GC.Collect();
        }

        protected bool onNavigatingGlobal(string url)
        {
            if (url.StartsWith("ixian:appAccept:"))
            {
                string session_id = url.Substring("ixian:appAccept:".Length);
                onAppAccept(session_id);
            }
            else if (url.StartsWith("ixian:appReject:"))
            {
                string session_id = url.Substring("ixian:appReject:".Length);
                onAppReject(session_id);
            }
            else if (url.StartsWith("ixian:hangUp:"))
            {
                string session_id = url.Substring("ixian:hangUp:".Length);
                VoIPManager.hangupCall(Crypto.stringToHash(session_id));
            }else
            {
                return false;
            }
            return true;
        }
    }
} 