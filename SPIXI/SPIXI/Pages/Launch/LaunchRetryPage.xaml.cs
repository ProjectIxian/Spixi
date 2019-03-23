using System;
using SPIXI.Interfaces;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using DLT.Meta;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LaunchRetryPage : SpixiContentPage
    {
		public LaunchRetryPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/intro_retry.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Contains("ixian:proceed:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:proceed:" }, StringSplitOptions.None);
                if(split.Count() < 1)
                {
                    e.Cancel = true;
                    return;
                }

                string password = split[1]; // Todo: secure this
                proceed(password);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void proceed(string pass)
        {
            // TODO: encrypt the password
            Application.Current.Properties["walletpass"] = pass;
            Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

            bool wallet_decrypted = Node.loadWallet();

            if (wallet_decrypted == false)
            {
                DisplayAlert("Error", "Cannot decrypt wallet. Please try again.", "OK");
                // Remove overlay
                webView.Eval("removeLoadingOverlay()");
                return;
            }

            Navigation.PushAsync(new HomePage(), Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);
        }

    }
}