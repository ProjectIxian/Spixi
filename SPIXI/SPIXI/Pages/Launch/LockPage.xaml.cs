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

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/lock.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
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
            Navigation.PushAsync(HomePage.Instance, Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);
        }

    }
}