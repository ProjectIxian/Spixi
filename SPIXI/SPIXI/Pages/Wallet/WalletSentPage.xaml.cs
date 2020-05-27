using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.IO;
using System.Linq;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WalletSentPage : SpixiContentPage
    {
        private Transaction transaction = null;

        private bool viewOnly = true;

        public WalletSentPage(Transaction tx, bool view_only = true)
        {
            viewOnly = view_only;

            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);


            transaction = tx;

            loadPage(webView, "wallet_sent.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            //webView.Eval(string.Format("setBalance('{0}')", Node.balance));
            if (transaction == null)
            {
                // This should never happen. Perhaps close this screen?
            }
            else
            {
                checkTransaction();
            }
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if(current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:dismiss", StringComparison.Ordinal))
            {
                onDismiss();
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        // Retrieve the transaction from local cache storage
        private void checkTransaction()
        {
            string confirmed_text = SpixiLocalization._SL("wallet-sent-confirmed");
            Transaction ctransaction = TransactionCache.getTransaction(transaction.id);
            if (ctransaction == null || ctransaction.applied == 0)
            {
                ctransaction = transaction;
                confirmed_text = SpixiLocalization._SL("wallet-sent-unconfirmed");
            }

            IxiNumber amount = ctransaction.amount;

            // Convert unix timestamp
            string time = Utils.UnixTimeStampToString(Convert.ToDouble(ctransaction.timeStamp));

            byte[] addr = new Address(ctransaction.pubKey).address;
            if (addr.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
            {
                // this is a sent payment

                foreach (var entry in ctransaction.toList)
                {
                    Friend friend = FriendList.getFriend(entry.Key);
                    if (friend != null)
                    {
                        Utils.sendUiCommand(webView, "addSender", friend.nickname + ": " + entry.Value.ToString(), time);
                    }
                    else
                    {
                        Utils.sendUiCommand(webView, "addSender", Base58Check.Base58CheckEncoding.EncodePlain(entry.Key) + ": " + entry.Value.ToString(), time);
                    }
                }
            }
            else
            {
                // this is a received payment

                amount = 0;

                Utils.sendUiCommand(webView, "setReceivedMode");
                byte[] sender_address = new Address(ctransaction.pubKey).address;
                Friend friend = FriendList.getFriend(sender_address);
                if (friend != null)
                {
                    Utils.sendUiCommand(webView, "addSender", friend.nickname, time);
                }
                else
                {
                    Utils.sendUiCommand(webView, "addSender", Base58Check.Base58CheckEncoding.EncodePlain(sender_address), time);
                }
                foreach (var entry in ctransaction.toList)
                {
                    if (IxianHandler.getWalletStorage().isMyAddress(entry.Key))
                    {
                        // TODO show this as well under sent to; also do the reverse for sent payment
                        //addresses += Base58Check.Base58CheckEncoding.EncodePlain(entry.Key) + ": " + entry.Value.ToString() + "|";
                        amount += entry.Value;
                    }
                }
            }

            Utils.sendUiCommand(webView, "setData", amount.ToString(), ctransaction.fee.ToString(),
                time, confirmed_text, (ctransaction.fee/ctransaction.amount).ToString() + "%", transaction.id);
            return;
        }

        public override void updateScreen()
        {
            checkTransaction();
        }

        private void onDismiss()
        {
            if (!viewOnly)
            {
                Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
            }
            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        protected override bool OnBackButtonPressed()
        {
            onDismiss();
            return true;
        }
    }
}