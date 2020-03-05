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
        private FriendMessage requestMsg = null;
        private string amount = null;
        private string date = null;

        public WalletContactRequestPage (FriendMessage request_msg, Friend fr, string am, string dt)
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            friend = fr;
            amount = am;
            date = dt;
            if(request_msg != null)
            {
                requestMsg = request_msg;
            }

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
            else if (current_url.Equals("ixian:decline", StringComparison.Ordinal))
            {
                onDecline();
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                onSend();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onDecline()
        {
            if (requestMsg != null)
            {
                string msgId = Crypto.hashToString(requestMsg.id);

                // send decline
                if (!requestMsg.message.StartsWith(":"))
                {
                    SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestFundsResponse, Encoding.UTF8.GetBytes(msgId));

                    requestMsg.message = "::" + requestMsg.message;

                    StreamMessage message = new StreamMessage();
                    message.type = StreamMessageCode.info;
                    message.recipient = friend.walletAddress;
                    message.sender = Node.walletStorage.getPrimaryAddress();
                    message.transaction = new byte[1];
                    message.sigdata = new byte[1];
                    message.data = spixi_message.getBytes();

                    StreamProcessor.sendMessage(friend, message);

                    Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);
                }
            }
            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        private void onSend()
        {
            string msgId = Crypto.hashToString(requestMsg.id);

            // send tx details to the request
            if (!requestMsg.message.StartsWith(":"))
            {
                // Create an ixian transaction and send it to the dlt network
                byte[] to = friend.walletAddress;

                IxiNumber amounti = new IxiNumber(amount);
                IxiNumber fee = ConsensusConfig.transactionPrice;
                byte[] from = Node.walletStorage.getPrimaryAddress();
                byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();

                Transaction transaction = new Transaction((int)Transaction.Type.Normal, amount, fee, to, from, null, pubKey, IxianHandler.getLastBlockHeight());

                NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.newTransaction, transaction.getBytes(), null);

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestFundsResponse, Encoding.UTF8.GetBytes(msgId + ":" + transaction.id));

                requestMsg.message = ":" + transaction.id;

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.info;
                message.recipient = to;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message);

                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);
            }

            Navigation.PopAsync(Config.defaultXamarinAnimations);

        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }

    }
}