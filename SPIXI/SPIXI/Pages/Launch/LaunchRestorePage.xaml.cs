using System;
using SPIXI.Interfaces;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using DLT.Meta;
using Plugin.FilePicker.Abstractions;
using Plugin.FilePicker;
using System.IO;

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
            else if (current_url.Equals("ixian:selectfile", StringComparison.Ordinal))
            {
                onSelectFile();
            }
            else if (current_url.Contains("ixian:restore:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:restore:" }, StringSplitOptions.None);
                if (split.Count() < 1)
                {
                    e.Cancel = true;
                    return;
                }

                string password = split[1]; // Todo: secure this
                onRestore(password);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        // Shows a file picker to select the wallet file
        private async void onSelectFile()
        {
            byte[] _data = null;
            try
            {
                FileData fileData = await CrossFilePicker.Current.PickFile();
                if (fileData == null)
                    return; // User canceled file picking

                string fileName = fileData.FileName;
                _data = fileData.DataArray;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception choosing file: " + ex.ToString());
                await DisplayAlert("Error", "Cannot select file", "OK");
            }

            if(_data == null)
            {
                await DisplayAlert("Error", "Cannot read file", "OK");
                return;
            }

            string docpath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string filepath = Path.Combine(docpath, Config.walletFile);
            try
            {
                File.WriteAllBytes(filepath, _data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
                await DisplayAlert("Error", "Cannot prepare wallet file", "OK");
                return;
            }

            webView.Eval("enableRestore()");
        }

        // Attempt to restore the wallet
        private void onRestore(string pass)
        {
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