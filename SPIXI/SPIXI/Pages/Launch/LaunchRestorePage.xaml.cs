using IXICore.Meta;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using System;
using System.IO;
using System.Linq;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LaunchRestorePage : SpixiContentPage
	{
		public LaunchRestorePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "intro_restore.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

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
                    Utils.sendUiCommand(webView, "removeLoadingOverlay");
                    displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-invalidpassword-title"), SpixiLocalization._SL("intro-restore-file-invalidpassword-text"), SpixiLocalization._SL("global-dialog-ok"));
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
                Logging.error("Exception choosing file: " + ex.ToString());
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-error-title"), SpixiLocalization._SL("intro-restore-file-selecterror-text"), SpixiLocalization._SL("global-dialog-ok"));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                return;
            }

            if (_data == null)
            {
                await displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-error-title"), SpixiLocalization._SL("intro-restore-file-readerror-text"), SpixiLocalization._SL("global-dialog-ok"));
                return;
            }

            string docpath = Config.spixiUserFolder;
            string filepath = Path.Combine(docpath, Config.walletFile + ".tmp");
            try
            {
                File.WriteAllBytes(filepath, _data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process: {0}", ex);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-error-title"), SpixiLocalization._SL("intro-restore-file-writeerror-text"), SpixiLocalization._SL("global-dialog-ok"));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                return;
            }

            Utils.sendUiCommand(webView, "enableRestore");
        }

        // Attempt to restore the wallet
        private void onRestore(string pass)
        {
            Application.Current.Properties["walletpass"] = pass;
            Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

            string filepath = Path.Combine(Config.spixiUserFolder, Config.walletFile);
            if (!Node.walletStorage.verifyWallet(filepath + ".tmp", pass))
            {
                displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-invalidpassword-title"), SpixiLocalization._SL("intro-restore-file-invalidpassword-text"), SpixiLocalization._SL("global-dialog-ok"));
                // Remove overlay
                Utils.sendUiCommand(webView, "removeLoadingOverlay");
                return;
            }else
            {
                File.Move(filepath + ".tmp", filepath);
                Node.loadWallet();
            }

            Navigation.PushAsync(HomePage.Instance(), Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);

            Node.start();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}