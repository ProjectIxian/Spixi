using DLT;
using DLT.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletSendPage : SpixiContentPage
	{
		public WalletSendPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_send.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            webView.Eval(string.Format("setBalance('{0}')", Node.balance.ToString()));


            /*         webView.Eval(string.Format("setAddress(\"{0}\")", Node.walletStorage.address));

                     // Check if this page is accessed from the home wallet
                     if (local_friend == null)
                     {
                         webView.Eval("hideRequest()");
                     }
                     else
                     {
                         webView.Eval(string.Format("setContactAddress(\"{0}\", \"{1}\")", local_friend.wallet_address, local_friend.nickname));
                     }*/
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync();
            }
            else if (current_url.Equals("ixian:pick", StringComparison.Ordinal))
            {
                var recipientPage = new WalletRecipientPage();
                recipientPage.pickSucceeded += HandlePickSucceeded;
                Navigation.PushModalAsync(recipientPage);
            }
            else if (current_url.Equals("ixian:quickscan", StringComparison.Ordinal))
            {
                quickScan();
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI", "Please type a wallet address.", "OK");
            }
            else if (current_url.Contains("ixian:next:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:next:" }, StringSplitOptions.None);
                byte[] wal = Base58Check.Base58CheckEncoding.DecodePlain(split[1]);
                
                if (Address.validateChecksum(wal) == false)
                {
                    e.Cancel = true;
                    DisplayAlert("Invalid checksum", "Please make sure you typed the address correctly.", "OK");                    
                    return;
                }

                Navigation.PushAsync(new WalletSend2Page(wal));
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                //Navigation.PushAsync(new LaunchRestorePage());
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        public async void quickScan()
        {

            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();

            // Restrict to QR codes only
            options.PossibleFormats = new List<ZXing.BarcodeFormat>() {
                ZXing.BarcodeFormat.QR_CODE
            };

            var ScannerPage = new ZXingScannerPage(options);


            ScannerPage.OnScanResult += (result) => {

                ScannerPage.IsScanning = false;

                Device.BeginInvokeOnMainThread(() => {
                    Navigation.PopAsync();
                    //DisplayAlert("New contact", result.Text, "OK");

                    if (result.Text.Contains(":ixi"))
                    {
                        string[] split = result.Text.Split(new string[] { ":ixi" }, StringSplitOptions.None);
                        if (split.Count() < 1)
                            return;
                        string wal = split[0];
                        webView.Eval(string.Format("setRecipient(\"{0}\")", wal));
                        return;
                    }
                    else if (result.Text.Contains(":send"))
                    {
                        // Check for transaction request
                        string[] split = result.Text.Split(new string[] { ":send:" }, StringSplitOptions.None);
                        if (split.Count() > 1)
                        {
                            string wallet_to_send = split[0];
                            webView.Eval(string.Format("setRecipient(\"{0}\")", wallet_to_send));
                            return;
                        }
                    }
                    else
                    {
                        // Handle direct addresses
                        string wallet_to_send = result.Text;
                        if(Address.validateChecksum(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send)))
                        {
                            webView.Eval(string.Format("setRecipient(\"{0}\")", wallet_to_send));
                            return;
                        }
                    }

                });
            };

            await Navigation.PushAsync(ScannerPage);
        }

        private void HandlePickSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            //MainPage = new MainPage();
            string id = e.Value;
            webView.Eval(string.Format("setRecipient(\"{0}\")", id));
            Navigation.PopModalAsync();
        }

    }
}