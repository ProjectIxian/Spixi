using DLT.Meta;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing;
using ZXing.Common;
using ZXing.Net.Mobile.Forms;
using ZXing.Rendering;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BackupPage : SpixiContentPage
	{
		public BackupPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/settings_backup.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {

        }


        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI Account", "Please type a password.", "OK");
            }
            else if (current_url.Equals("ixian:backup", StringComparison.Ordinal))
            {
                onBackup();
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }



        private async void onBackup()
        {
            try
            {
                string docpath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string filepath = Path.Combine(docpath, Config.walletFile);
                await DependencyService.Get<IFileOperations>().share(filepath, "spixi.wal");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception fetching wallet: " + ex.ToString());
            }
        }
    }
}