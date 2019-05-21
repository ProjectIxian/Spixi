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
	public partial class WalletRecipientPage : SpixiContentPage
	{
        public event EventHandler<SPIXI.EventArgs<string>> pickSucceeded;

        public WalletRecipientPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_recipient.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            loadContacts();
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
                Navigation.PopModalAsync();
            }
            else if (current_url.Contains("ixian:select:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:select:" }, StringSplitOptions.None);
                string id = split[1];
                // TODO check for | separator
                onPickSucceeded(id);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onPickSucceeded(string id)
        {
            if (pickSucceeded != null)
            {
                pickSucceeded(this, new SPIXI.EventArgs<string>(id));
            }
        }

        public void loadContacts()
        {
            webView.Eval("clearContacts()");

            foreach (Friend friend in FriendList.friends)
            {
                string str_online = "false";
                if (friend.online)
                    str_online = "true";

                webView.Eval(string.Format("addContact(\"{0}\", \"{1}\", \"{2}\", {3})", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, "avatar.png", str_online));
            }
        }

    }
}