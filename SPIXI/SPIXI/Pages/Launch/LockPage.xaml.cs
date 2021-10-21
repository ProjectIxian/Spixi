using IXICore;
using Plugin.Fingerprint.Abstractions;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LockPage : SpixiContentPage
    {
        private CancellationTokenSource _cancel;
        public LockPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "lock.html");
        }

        private async void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (Device.RuntimePlatform == Device.WPF)
                return;

            // Show biometric and alternative authentication methods
            await AuthenticateAsync(SpixiLocalization._SL("global-lock-auth-text"));
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
                // Retrieve the password and unlock
                string[] split = current_url.Split(new string[] { "ixian:unlock:" }, StringSplitOptions.None);
                string pass = split[1];
                if (pass != null)
                    doUnlock(pass);
            }
            else if (current_url.Equals("ixian:change", StringComparison.Ordinal))
            {
                // Show the launch screen
                Navigation.PushAsync(new SPIXI.LaunchPage(), Config.defaultXamarinAnimations);
                Navigation.RemovePage(this);
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
            string target_filepath = Path.Combine(Config.spixiUserFolder, Config.walletFile);
            WalletStorage ws = new WalletStorage(target_filepath);
            if (!ws.verifyWallet(target_filepath, pass))
            {
                displaySpixiAlert(SpixiLocalization._SL("intro-restore-file-invalidpassword-title"), SpixiLocalization._SL("intro-restore-file-invalidpassword-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
            else
            {
                performUnlock();
            }
        }

        private void performUnlock()
        {
            Navigation.PushAsync(HomePage.Instance(true), Config.defaultXamarinAnimations);
            Navigation.RemovePage(this);
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        private async Task AuthenticateAsync(string reason, string cancel = null, string fallback = null, string tooFast = null)
        {
            _cancel = new CancellationTokenSource();

            var dialogConfig = new AuthenticationRequestConfiguration("SPIXI", reason)
            {
                CancelTitle = cancel,
                FallbackTitle = fallback,
                AllowAlternativeAuthentication = true,
                ConfirmationRequired = true
            };

            dialogConfig.HelpTexts.MovedTooFast = tooFast;

            var result = await Plugin.Fingerprint.CrossFingerprint.Current.AuthenticateAsync(dialogConfig, _cancel.Token);

            await SetResultAsync(result);
        }

        private Task SetResultAsync(FingerprintAuthenticationResult result)
        {
            if (result.Authenticated)
            {
                performUnlock();
            }
            else
            {
                _ = displaySpixiAlert(SpixiLocalization._SL("global-lock-invalidpassword-title"), SpixiLocalization._SL("global-lock-invalidpassword-text"), "Cancel");
            }

            return Task.CompletedTask;
        }

    }
}