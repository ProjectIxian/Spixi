using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using System;
using System.IO;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LaunchPage : SpixiContentPage
	{
		public LaunchPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "intro.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:create", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new LaunchCreatePage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:restore", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new LaunchRestorePage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.StartsWith("ixian:language:", StringComparison.Ordinal))
            {
                string lang = current_url.Substring("ixian:language:".Length);
                SpixiLocalization.loadLanguage(lang);
                loadPage(webView, "intro.html");
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        public void onCreateAccount(object sender, EventArgs e)
        {
            Navigation.PushAsync(new LaunchCreatePage(), Config.defaultXamarinAnimations);
        }

        public void onRestoreAccount(object sender, EventArgs e)
        {
            Navigation.PushAsync(new LaunchRestorePage(), Config.defaultXamarinAnimations);
        }
    }
}