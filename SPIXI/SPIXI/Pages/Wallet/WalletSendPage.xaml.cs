using DLT;
using DLT.Meta;
using DLT.Network;
using IXICore;
using SPIXI.Interfaces;
using SPIXI.Storage;
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
                string nickname = "Unknown";

                Friend friend = FriendList.getFriend(recipient);
                if (friend != null)
                    nickname = friend.nickname;

                webView.Eval(string.Format("setRecipient('{0}','{1}')",
                    Base58Check.Base58CheckEncoding.EncodePlain(recipient), nickname));
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
                string first_part = split[1];
                string[] inner_split = first_part.Split(new string[] { ":" }, StringSplitOptions.None);

                string wallet = inner_split[0];

                if (Address.validateChecksum(Base58Check.Base58CheckEncoding.DecodePlain(wallet)) == false)
                {
                    e.Cancel = true;
                    DisplayAlert("Invalid checksum", "Please make sure you typed the address correctly.", "OK");
                    return;
                }
                string amount = inner_split[1];

                string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                if (amount_split.Length > 2)
                {
                    DisplayAlert("SPIXI", "Please type a correct decimal amount.", "OK");
                    e.Cancel = true;
                    return;
                }

                IxiNumber _amount = amount;

                if (_amount < (long)0)
                {
                    DisplayAlert("SPIXI", "Please type a positive amount.", "OK");
                    e.Cancel = true;
                    return;
                }
                else if (_amount > Node.balance)
                {
                    DisplayAlert("SPIXI", "Insufficient funds.", "OK");
                    e.Cancel = true;
                    return;
                }

                sendPayment(wallet, _amount.ToString());
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
                    //DisplayAlert("New contact", result.Text, "OK");

                    if (result.Text.Contains(":ixi"))
                    {
                        string[] split = result.Text.Split(new string[] { ":ixi" }, StringSplitOptions.None);
                        if (split.Count() < 1)
                            return;
                        string wallet_to_send = split[0];
                        string nickname = "Unknown";

                        Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                        if (friend != null)
                            nickname = friend.nickname;
                        webView.Eval(string.Format("setRecipient('{0}','{1}')", wallet_to_send, nickname));
                        return;
                    }
                    else if (result.Text.Contains(":send"))
                    {
                        // Check for transaction request
                        string[] split = result.Text.Split(new string[] { ":send:" }, StringSplitOptions.None);
                        if (split.Count() > 1)
                        {
                            string wallet_to_send = split[0];
                            string nickname = "Unknown";

                            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                            if (friend != null)
                                nickname = friend.nickname;
                            webView.Eval(string.Format("setRecipient('{0}','{1}')", wallet_to_send, nickname));
                            return;
                        }
                    }
                    else
                    {
                        // Handle direct addresses
                        string wallet_to_send = result.Text;
                        if(Address.validateChecksum(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send)))
                        {
                            string nickname = "Unknown";

                            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
                            if (friend != null)
                                nickname = friend.nickname;

                            webView.Eval(string.Format("setRecipient('{0}','{1}')", wallet_to_send, nickname));
                            return;
                        }
                    }

                });
            };

            await Navigation.PushAsync(ScannerPage, Config.defaultXamarinAnimations);
        }

        private void HandlePickSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            //MainPage = new MainPage();
            string wallet_to_send = e.Value;
            string nickname = "Unknown";

            Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));
            if (friend != null)
                nickname = friend.nickname;
            webView.Eval(string.Format("setRecipient('{0}','{1}')", wallet_to_send, nickname));
            Navigation.PopModalAsync();
        }

        private void sendPayment(string wallet, string amount_string)
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            // Create an ixian transaction and send it to the dlt network
            byte[] to = Base58Check.Base58CheckEncoding.DecodePlain(wallet);

            IxiNumber amount = new IxiNumber(amount_string);
            IxiNumber fee = CoreConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();

            Transaction transaction = new Transaction((int)Transaction.Type.Normal, amount, fee, to, from, null, pubKey, Node.getLastBlockHeight());

            NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.newTransaction, transaction.getBytes());

            // Add the unconfirmed transaction the the cache
            TransactionCache.addUnconfirmedTransaction(transaction);

            // Show the payment details
            Navigation.PushAsync(new WalletSentPage(transaction), Config.defaultXamarinAnimations);
        }

    }
}