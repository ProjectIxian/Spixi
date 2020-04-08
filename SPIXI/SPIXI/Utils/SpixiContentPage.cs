using IXICore;
using IXICore.Meta;
using SPIXI.CustomApps;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.VoIP;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPIXI
{
	public class SpixiContentPage : ContentPage
	{
        public bool CancelsTouchesInView = true;

        protected WebView _webView = null;

        public virtual void recalculateLayout()
        {

        }

        public Task displaySpixiAlert(string title, string message, string cancel)
        {
            ISystemAlert alert = DependencyService.Get<ISystemAlert>();
            if (alert != null)
            {
                alert.displayAlert(title, message, cancel);
                return null;
            }

            return DisplayAlert(title, message, cancel);
        }

        public void displayCallBar(byte[] session_id, string text)
        {
            if (_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(_webView, "displayCallBar", Crypto.hashToString(session_id), text);
        }

        public void hideCallBar()
        {
            if (_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(_webView, "hideCallBar");
        }

        public void displayAppRequests()
        {
            if(_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(_webView, "clearAppRequests");
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
                    string text = f.nickname + " wants to use " + app.name + " with you.";
                    Utils.sendUiCommand(_webView, "addAppRequest", Crypto.hashToString(page.sessionId), text);
                }
                if(VoIPManager.isInitiated() && !VoIPManager.currentCallAccepted)
                {
                    Friend f = VoIPManager.currentCallContact;
                    string text = f.nickname + " is calling you.";
                    Utils.sendUiCommand(_webView, "addAppRequest", Crypto.hashToString(VoIPManager.currentCallSessionId), text);
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
    }
} 