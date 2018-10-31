using DLT;
using DLT.Meta;
using DLT.Network;
using SPIXI.Interfaces;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletSend2Page : SpixiContentPage
	{
        private string recipient = "";
        private bool directPayment = false; // false if called from the wallet, true if called from a chat window

		public WalletSend2Page (string wal, bool direct = false)
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            recipient = wal;
            directPayment = direct;

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_send_2.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            webView.Eval(string.Format("setBalance('{0}')", Node.balance.ToString()));

            string nickname = "Unknown";

            Friend friend = FriendList.getFriend(recipient);
            if (friend != null)
                nickname = friend.nickname;

            webView.Eval(string.Format("setRecipient('{0}','{1}')", recipient, nickname));

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync();
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI", "Please type an amount.", "OK");
            }
            else if (current_url.Contains("ixian:send:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:send:" }, StringSplitOptions.None);
                string first_part = split[1];
                string[] inner_split = first_part.Split(new string[] { ":" }, StringSplitOptions.None);

                string wallet = inner_split[0];
                string amount = inner_split[1];

                string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                if(amount_split.Length > 2)
                {
                    DisplayAlert("SPIXI", "Please type a correct decimal amount.", "OK");
                    e.Cancel = true;
                    return;
                }

                IxiNumber _amount = amount;

                if (_amount < (long) 0)
                {
                    DisplayAlert("SPIXI", "Please type a positive amount.", "OK");
                    e.Cancel = true;
                    return;
                }
                else if(_amount > Node.balance)
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

        private void sendPayment(string wallet, string amount)
        {
            Navigation.PopAsync();

            // Double-pop the navigation if called from the wallet
            if (directPayment == false)
                Navigation.PopAsync();

            // Create an ixian transaction and send it to the dlt network
            string from = Node.walletStorage.address;

            Transaction transaction = new Transaction(amount, wallet, from);
            NetworkClientManager.sendDLTData(ProtocolMessageCode.newTransaction, transaction.getBytes());

            // Add the unconfirmed transaction the the cache
            TransactionCache.addUnconfirmedTransaction(transaction);

            // Show the payment details
            Navigation.PushAsync(new WalletSentPage(transaction));
        }

    }
}