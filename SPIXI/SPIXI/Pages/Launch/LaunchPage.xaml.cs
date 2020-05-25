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

            // Load the platform specific home page url
            //var source = new UrlWebViewSource();
            //source.Url = string.Format("{0}html/intro.html", DependencyService.Get<IBaseUrl>().Get());
            var source = new HtmlWebViewSource();
            source.BaseUrl = string.Format("{0}html/", DependencyService.Get<IBaseUrl>().Get());
            source.Html = SpixiLocalization.getLocalizedHtml(Path.Combine("html", "intro.html"), source.BaseUrl);
            Logging.info(source.BaseUrl);
            webView.Source = source;

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