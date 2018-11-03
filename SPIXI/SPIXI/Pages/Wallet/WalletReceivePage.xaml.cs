using DLT.Meta;
using SPIXI.Interfaces;
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
            webView.Eval(string.Format("setAddress(\"{0}\")", Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.address)));

            // Check if this page is accessed from the home wallet
            if (local_friend == null)
            {
                //webView.Eval("hideRequest()");
            }
            else
            {
                webView.Eval(string.Format("setContactAddress(\"{0}\", \"{1}\")", Base58Check.Base58CheckEncoding.EncodePlain(local_friend.wallet_address), local_friend.nickname));
            }
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
            else if (current_url.Contains("ixian:request:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:request:" }, StringSplitOptions.None);
                string amount = split[1];
                onRequest(amount);
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

        private void onRequest(string amount)
        {
            // Make sure we have a valid friend to send to
            if (local_friend == null)
            {
                return;
            }

            // TODOSPIXI
            /*
            // Prepare and send the request message
            byte[] encrypted_message = StreamProcessor.prepareSpixiMessage(SpixiMessageCode.requestFunds, amount, local_friend.pubkey);
            Message message = new Message();
            message.recipientAddress = local_friend.wallet_address;
            message.data = encrypted_message;
            StreamProcessor.sendMessage(message);

            Navigation.PopAsync();
            */
        }
    }
}