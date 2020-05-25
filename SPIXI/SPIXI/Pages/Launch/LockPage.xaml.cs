using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LockPage : SpixiContentPage
	{
		public LockPage ()
		{
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "lock.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                // No back button for this screen
            }
            else if (current_url.Contains("ixian:unlock:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:unlock:" }, StringSplitOptions.None);
                string pass = split[1];
                if(pass != null)
                    doUnlock(pass);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void doUnlock(string pass)
        {
            Navigation.PushAsync(HomePage.Instance(), Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}