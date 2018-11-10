using DLT;
using DLT.Meta;
using DLT.Network;
using SPIXI.Interfaces;
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
	public partial class WalletContactRequestPage : SpixiContentPage
	{
        private Friend friend = null;
        private string amount = null;
        private string date = null;

        public WalletContactRequestPage (Friend fr, string am, string dt)
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            friend = fr;
            amount = am;
            date = dt;

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_contact_request.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            webView.Eval(string.Format("setData('{0}','{1}','{2}','{3}')", friend.walletAddress, friend.nickname, amount, date));
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync();
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                onSend();
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onSend()
        {
            // TODOSPIXI
            /*
            // Create an ixian transaction and send it to the dlt network
            byte[] from = Node.walletStorage.address;
            ulong _amount = Convert.ToUInt64(amount);

            if(_amount > Node.balance)
            {
                DisplayAlert("Failed", "Insufficient funds", "OK");
                return;
            }

            Transaction transaction = new Transaction(_amount, friend.wallet_address, from);
            NetworkClientManager.sendDLTData(ProtocolMessageCode.newTransaction, transaction.getBytes());

            // Add the unconfirmed transaction the the cache
            TransactionCache.addUnconfirmedTransaction(transaction);

            FriendList.addMessageWithType(FriendMessageType.sentFunds, friend.wallet_address, transaction.id);

            Navigation.PopAsync();*/
        }


    }
}