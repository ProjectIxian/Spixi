using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Linq;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ContactDetails : SpixiContentPage
	{
        private Friend friend = null;
        private bool customChatBtn = false;

		public ContactDetails (Friend lfriend, bool customChatButton = false)
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            friend = lfriend;
            customChatBtn = customChatButton;

            loadPage(webView, "contact_details.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setAddress", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));

            updateScreen();
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (onNavigatingGlobal(current_url))
            {
                e.Cancel = true;
                return;
            }

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:remove", StringComparison.Ordinal))
            {
                onRemove();
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:removehistory", StringComparison.Ordinal))
            {
                onRemoveHistory();
            }
            else if (current_url.Equals("ixian:request", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletReceivePage(friend), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletSendPage(friend.walletAddress), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:chat", StringComparison.Ordinal))
            {
                if (customChatBtn)
                {
                    Navigation.PopAsync(Config.defaultXamarinAnimations);
                    e.Cancel = true;
                    return;
                }
                else
                {
                    Navigation.PushAsync(new SingleChatPage(friend), Config.defaultXamarinAnimations);
                }
            }
            else if (current_url.Contains("ixian:txdetails:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:txdetails:" }, StringSplitOptions.None);
                string id = split[1];

                Transaction transaction = null;
                foreach (Transaction tx in TransactionCache.transactions)
                {
                    if (tx.id.Equals(id, StringComparison.Ordinal))
                    {
                        transaction = tx;
                        break;
                    }
                }

                if (transaction == null)
                {
                    foreach (Transaction tx in TransactionCache.unconfirmedTransactions)
                    {
                        if (tx.id.Equals(id, StringComparison.Ordinal))
                        {
                            transaction = tx;
                            break;
                        }
                    }

                    if (transaction == null)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                Navigation.PushAsync(new WalletSentPage(transaction), Config.defaultXamarinAnimations);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        private void onRemove()
        {
            if (friend.bot && friend.metaData.botInfo != null)
            {
                friend.pendingDeletion = true;
                friend.save();
                Node.shouldRefreshContacts = true;
                StreamProcessor.sendLeave(friend, null);
                displaySpixiAlert(SpixiLocalization._SL("contact-details-removedcontact-title"), SpixiLocalization._SL("contact-details-removedcontact-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
            else
            {
                if (FriendList.removeFriend(friend) == true)
                {
                    displaySpixiAlert(SpixiLocalization._SL("contact-details-removedcontact-title"), SpixiLocalization._SL("contact-details-removedcontact-text"), SpixiLocalization._SL("global-dialog-ok"));
                }
            }
        }

        private void onRemoveHistory()
        {
            // Remove history file
            if(friend.deleteHistory())
            {
                displaySpixiAlert(SpixiLocalization._SL("contact-details-deletedhistory-title"), SpixiLocalization._SL("contact-details-deletedhistory-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
        }

        public void loadTransactions()
        {
            Utils.sendUiCommand(webView, "clearRecentActivity");

            foreach (Transaction utransaction in TransactionCache.unconfirmedTransactions)
            {
                byte[] from_address = new Address(utransaction.pubKey).address;
                // Filter out unrelated transactions
                if(from_address.SequenceEqual(friend.walletAddress) == false)
                {
                    if (utransaction.toList.ContainsKey(friend.walletAddress) == false)
                        continue;
                }

                string tx_type = SpixiLocalization._SL("global-received");
                if (from_address.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
                {
                    tx_type = SpixiLocalization._SL("global-sent");
                }
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(utransaction.timeStamp));
                Utils.sendUiCommand(webView, "addPaymentActivity", utransaction.id, tx_type, time, utransaction.amount.ToString(), "false");
            }

            for (int i = TransactionCache.transactions.Count - 1; i >= 0; i--)
            {
                Transaction transaction = TransactionCache.transactions[i];

                byte[] from_address = new Address(transaction.pubKey).address;
                // Filter out unrelated transactions
                if (from_address.SequenceEqual(friend.walletAddress) == false)
                {
                    if (transaction.toList.ContainsKey(friend.walletAddress) == false)
                        continue;
                }

                string tx_type = SpixiLocalization._SL("global-received");
                if (from_address.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
                {
                    tx_type = SpixiLocalization._SL("global-sent");
                }
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(transaction.timeStamp));

                string confirmed = "true";
                if (transaction.applied == 0)
                {
                    confirmed = "error";
                }

                Utils.sendUiCommand(webView, "addPaymentActivity", transaction.id, tx_type, time, transaction.amount.ToString(), confirmed);
            }
        }

        // Executed every second
        public override void updateScreen()
        {
            Utils.sendUiCommand(webView, "setNickname", friend.nickname);

            string avatar = Node.localStorage.getAvatarPath(Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), false);
            if (avatar == null)
            {
                avatar = "";
            }

            Utils.sendUiCommand(webView, "setAvatar", avatar);

            if (friend.online)
            {
                Utils.sendUiCommand(webView, "showIndicator", "true");
            }
            else
            {
                Utils.sendUiCommand(webView, "showIndicator", "false");
            }

            loadTransactions();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}