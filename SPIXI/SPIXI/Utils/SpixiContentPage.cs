using IXICore;
using SPIXI.CustomApps;
using SPIXI.Interfaces;
using SPIXI.Meta;
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

        public void displayAppRequests()
        {
            if(_webView == null)
            {
                return;
            }
            Utils.sendUiCommand(_webView, "clearAppRequests");
            var app_pages = FriendList.getAppPages();
            lock (app_pages)
            {
                foreach (CustomAppPage page in app_pages.Values)
                {
                    Friend f = FriendList.getFriend(page.hostUserAddress);
                    CustomApp app = Node.customAppManager.getApp(page.appId);
                    Utils.sendUiCommand(_webView, "addAppRequest", Crypto.hashToString(page.sessionId), f.nickname, app.name);
                }
            }
        }

        public void onAppAccept(string session_id)
        {
            byte[] b_session_id = Crypto.stringToHash(session_id);
            CustomAppPage app_page = FriendList.getAppPage(b_session_id);
            app_page.accepted = true;
            Navigation.PushAsync(app_page, Config.defaultXamarinAnimations);
        }

        public void onAppReject(string session_id)
        {
            byte[] b_session_id = Crypto.stringToHash(session_id);
            FriendList.removeAppPage(b_session_id);
        }

        public virtual void updateScreen()
        {

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
 