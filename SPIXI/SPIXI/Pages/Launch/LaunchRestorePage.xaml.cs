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
	public partial class LaunchRestorePage : SpixiContentPage
	{
		public LaunchRestorePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/intro_restore.html", DependencyService.Get<IBaseUrl>().Get());
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
            else if (current_url.Equals("ixian:restore", StringComparison.Ordinal))
            {
                //Navigation.PushAsync(new LaunchRestorePage(), Config.defaultXamarinAnimations);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }


        public void onRestoreAccount(object sender, EventArgs e)
        {
            DisplayAlert("Button", "Account restored", "Ok");
            //Navigation.PopAsync();
            //Navigation.PopAsync();

            Navigation.PushAsync(new HomePage(), Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);
        }
    }
}