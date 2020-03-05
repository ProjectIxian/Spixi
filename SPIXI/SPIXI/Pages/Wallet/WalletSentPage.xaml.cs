using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
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

            // Retrieve transaction network status
            //requestTransactionData();

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_sent.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
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
                if (!viewOnly)
                {
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                }
                Navigation.PopAsync();
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        // Request transaction data
        private void requestTransactionData()
        {
            Logging.info("Requesting transaction data for: {0}", transaction.id);
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(transaction.id);
                    writer.Write((ulong)0);

                    CoreProtocolMessage.broadcastProtocolMessageToSingleRandomNode(new char[] { 'M' }, IXICore.Network.ProtocolMessageCode.getTransaction, m.ToArray(), 0, null);
                }
            }

        }

        // Retrieve the transaction from local cache storage
        private void checkTransaction()
        {
            string confirmed_text = "CONFIRMED";
            Transaction ctransaction = TransactionCache.getTransaction(transaction.id);
            if (ctransaction == null || ctransaction.applied == 0)
            {
                requestTransactionData();
                ctransaction = transaction;
                confirmed_text = "UNCONFIRMED";
            }

            IxiNumber amount = ctransaction.amount;

            string addresses = "";
            byte[] addr = new Address(ctransaction.pubKey).address;
            if (addr.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
            {
                // this is a sent payment

                foreach (var entry in ctransaction.toList)
                {
                    Friend friend = FriendList.getFriend(entry.Key);
                    if (friend != null)
                    {
                        addresses += friend.nickname + ": " + entry.Value.ToString() + "|";
                    }
                    else
                    {
                        addresses += Base58Check.Base58CheckEncoding.EncodePlain(entry.Key) + ": " + entry.Value.ToString() + "|";
                    }
                }
            }
            else
            {
                // this is a received payment

                amount = 0;

                Utils.sendUiCommand(webView, "setReceivedMode");
                foreach (var entry in ctransaction.toList)
                {
                    if (entry.Key.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
                    {
                        addresses += Base58Check.Base58CheckEncoding.EncodePlain(entry.Key) + ": " + entry.Value.ToString() + "|";
                        amount += entry.Value;
                    }
                }
            }

            // Convert unix timestamp
            string time = Utils.UnixTimeStampToString(Convert.ToDouble(ctransaction.timeStamp));

            Utils.sendUiCommand(webView, "setData", amount.ToString(), ctransaction.fee.ToString(),
                addresses, time, confirmed_text, (ctransaction.fee/amount).ToString() + "%");
            return;
        }

        public override void updateScreen()
        {
            Logging.info("Updating wallet send");

            checkTransaction();
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}