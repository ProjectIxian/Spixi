using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Text;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WalletReceivePage : SpixiContentPage
	{
        private Friend local_friend = null;

		public WalletReceivePage ()
		{
			InitializeComponent ();
//            pubkeyAddress.Text = Node.walletStorage.address;
            NavigationPage.SetHasNavigationBar(this, false);


            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_request.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        public WalletReceivePage(Friend friend)
        {
            InitializeComponent();
            //            pubkeyAddress.Text = Node.walletStorage.address;
            NavigationPage.SetHasNavigationBar(this, false);

            local_friend = friend;

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_request.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setAddress", Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.getPrimaryAddress()));

            // Check if this page is accessed from the home wallet
            if (local_friend == null)
            {
                //webView.Eval("hideRequest()");
            }
            else
            {
                Utils.sendUiCommand(webView, "addRecipient", local_friend.nickname, Base58Check.Base58CheckEncoding.EncodePlain(local_friend.walletAddress));
            }
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
            else if (current_url.Equals("ixian:pick", StringComparison.Ordinal))
            {
                var recipientPage = new WalletRecipientPage();
                recipientPage.pickSucceeded += HandlePickSucceeded;
                Navigation.PushModalAsync(recipientPage);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert("SPIXI", "Please type an amount.", "OK");
            }
            else if (current_url.Contains("ixian:sendrequest:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:sendrequest:" }, StringSplitOptions.None);

                // Extract all addresses and amounts
                string[] addresses_split = split[1].Split(new string[] { "|" }, StringSplitOptions.None);

                string[] split_address = addresses_split[0].Split(':');

                string recipient = split_address[0];
                string amount = split_address[1];
                onRequest(recipient, amount);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onRequest(string recipient, string amount)
        {
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestFunds, Encoding.UTF8.GetBytes(amount));

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = Base58Check.Base58CheckEncoding.DecodePlain(recipient);
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            string relayip = FriendList.getRelayHostname(message.recipient);
            StreamProcessor.sendMessage(message, relayip);

            Navigation.PopAsync(Config.defaultXamarinAnimations);

        }

        private void HandlePickSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            string wallets_to_send = e.Value;

            string[] wallet_arr = wallets_to_send.Split('|');

            foreach (string wallet_to_send in wallet_arr)
            {
                Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(wallet_to_send));

                string nickname = wallet_to_send;
                if (friend != null)
                    nickname = friend.nickname;

                Utils.sendUiCommand(webView, "addRecipient", nickname, wallet_to_send);
            }
            Navigation.PopModalAsync();
        }
    }
}