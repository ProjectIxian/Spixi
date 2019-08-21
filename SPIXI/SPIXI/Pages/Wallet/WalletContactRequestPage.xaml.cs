using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Text;
using System.Web;
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
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setData", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, amount, ConsensusConfig.transactionPrice.ToString(), date);
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
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
            // Create an ixian transaction and send it to the dlt network
            byte[] to = friend.walletAddress;

            IxiNumber amounti = new IxiNumber(amount);
            IxiNumber fee = ConsensusConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();

            Transaction transaction = new Transaction((int)Transaction.Type.Normal, amount, fee, to, from, null, pubKey, IxianHandler.getLastBlockHeight());

            NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.newTransaction, transaction.getBytes(), null);

            // Add the unconfirmed transaction the the cache
            TransactionCache.addUnconfirmedTransaction(transaction);
            FriendMessage friend_message = FriendList.addMessageWithType(null, FriendMessageType.sentFunds, friend.walletAddress, transaction.id, true);

            SpixiMessage spixi_message = new SpixiMessage(friend_message.id, SpixiMessageCode.sentFunds, Encoding.UTF8.GetBytes(amount));

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = to;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            StreamProcessor.sendMessage(friend, message);

            Navigation.PopAsync(Config.defaultXamarinAnimations);

        }


    }
}