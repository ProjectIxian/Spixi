using IXICore;
using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletSendPage : SpixiContentPage
	{
        private byte[] recipient = null;

        public WalletSendPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_send.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        public WalletSendPage(byte[] wal)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            recipient = wal.ToArray();

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_send.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            webView.Eval(string.Format("setBalance('{0}')", Node.balance.ToString()));

            // If we have a pre-set recipient, fill out the recipient wallet address and nickname
            if (recipient != null)
            {
                string nickname = Base58Check.Base58CheckEncoding.EncodePlain(recipient);

                Friend friend = FriendList.getFriend(recipient);
                if (friend != null)
                    nickname = friend.nickname;

                webView.Eval(string.Format("addRecipient('{0}','{1}')", nickname, 
                    Base58Check.Base58CheckEncoding.EncodePlain(recipient)));
            }
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
            else if (current_url.Equals("ixian:error2", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI", "Please type an amount.", "OK");
            }
            else if (current_url.Contains("ixian:send:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:send:" }, StringSplitOptions.None);

                // Extract all addresses and amounts
                string[] addresses_split = split[1].Split(new string[] { "%7C" }, StringSplitOptions.None);

                // Go through each entry
                foreach(string address_and_amount in addresses_split)
                {
                    if (address_and_amount.Length < 1)
                        continue;

                    // Extract the address and amount
                    string[] asplit = address_and_amount.Split(new string[] { ":" }, StringSplitOptions.None);
                    if (asplit.Count() < 2)
                        continue;

                    string address = asplit[0];
                    string amount = asplit[1];

                    if (Address.validateChecksum(Base58Check.Base58CheckEncoding.DecodePlain(address)) == false)
                    {
                        e.Cancel = true;
                        DisplayAlert("Invalid address checksum", "Please make sure you typed the address correctly.", "OK");
                        return;
                    }
                    string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                    if (amount_split.Length > 2)
                    {
                        DisplayAlert("SPIXI", "Please type a correct decimal amount.", "OK");
                        e.Cancel = true;
                        return;
                    }
                    // Add decimals if none found
                    if (amount_split.Length == 1)
                        amount = String.Format("{0}.0", amount);

                    IxiNumber _amount = amount;

                    if (_amount < (long)0)
                    {
                        DisplayAlert("SPIXI", "Please type a positive amount.", "OK");
                        e.Cancel = true;
                        return;
                    }
                }

                Navigation.PushAsync(new WalletSend2Page(addresses_split));
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
                    Navigation.PopAsync(Config.defaultXamarinAnimations);

                    if (result.Text.Contains(":ixi"))
                    {
                        string[] split = result.Text.Split(new string[] { ":ixi" }, StringSplitOptions.None);
                        if (split.Count() < 1)
                            return;
                        string wallet_to_send = split[0];
                        string nickname = wallet_to_send;

                        Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                        if (friend != null)
                            nickname = friend.nickname;
                        webView.Eval(string.Format("addRecipient('{0}','{1}')", nickname, wallet_to_send));
                        return;
                    }
                    else if (result.Text.Contains(":send"))
                    {
                        // Check for transaction request
                        string[] split = result.Text.Split(new string[] { ":send:" }, StringSplitOptions.None);
                        if (split.Count() > 1)
                        {
                            string wallet_to_send = split[0];
                            string nickname = wallet_to_send;

                            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                            if (friend != null)
                                nickname = friend.nickname;
                            webView.Eval(string.Format("addRecipient('{0}','{1}')", nickname, wallet_to_send));
                            return;
                        }
                    }
                    else
                    {
                        // Handle direct addresses
                        string wallet_to_send = result.Text;
                        if(Address.validateChecksum(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send)))
                        {
                            string nickname = wallet_to_send;

                            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                            if (friend != null)
                                nickname = friend.nickname;

                            webView.Eval(string.Format("addRecipient('{0}','{1}')", nickname, wallet_to_send));
                            return;
                        }
                    }

                });
            };

            await Navigation.PushAsync(ScannerPage, Config.defaultXamarinAnimations);
        }

        private void HandlePickSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            string wallet_to_send = e.Value;
            string nickname = wallet_to_send;

            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
            if (friend != null)
                nickname = friend.nickname;
            webView.Eval(string.Format("addRecipient('{0}','{1}')", nickname, wallet_to_send));
            Navigation.PopModalAsync();
        }

    }
}