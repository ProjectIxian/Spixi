using DLT;
using DLT.Meta;
using DLT.Network;
using SPIXI.Interfaces;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WalletSentPage : SpixiContentPage
    {
        private Transaction transaction = null;

        public WalletSentPage(Transaction tx)
        {
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
            //webView.Eval(string.Format("setBalance('{0}')", Node.balance));
            if (transaction == null)
            {
                // This should never happen. Perhaps close this screen?
            }
            else
            {
                string nickname = "Unknown";
                Friend friend = null;
                byte[] addr = transaction.to;
                // Check if this is a received payment
                if (transaction.to.SequenceEqual(Node.walletStorage.address))
                {
                    webView.Eval("setReceivedMode()");
                    friend = FriendList.getFriend(transaction.from);
                    addr = transaction.from;
                }
                else
                {
                    // This is a sent payment
                    friend = FriendList.getFriend(transaction.to);
                }

                if (friend != null)
                    nickname = friend.nickname;

                // Convert unix timestamp
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(transaction.timeStamp));

                webView.Eval(string.Format("setInitialData('{0}', '{1}', '{2}', '{3}')", transaction.amount.ToString(), addr, nickname, time));
            }

            checkTransaction();

            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                return checkTransaction(); // True = Repeat again, False = Stop the timer
            });
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
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
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(transaction.id);

                    // TODOSPIXI
                    //NetworkClientManager.sendDLTData(ProtocolMessageCode.getTransaction, m.ToArray());
                }
            }

        }

        // Retrieve the transaction from local cache storage
        private bool checkTransaction()
        {
            Transaction ctransaction = TransactionCache.getTransaction(transaction.id);
            if (ctransaction == null)
            {
                requestTransactionData();
                return true;
            }

            string nickname = "Unknown";
            Friend friend = null;
            byte[] addr = ctransaction.to;
            // Check if this is a received payment
            if (ctransaction.to.SequenceEqual(Node.walletStorage.address))
            {
                webView.Eval("setReceivedMode()");
                friend = FriendList.getFriend(transaction.from);
                addr = ctransaction.from;
            }
            else
            {
                // This is a sent payment
                friend = FriendList.getFriend(transaction.to);
            }

            if (friend != null)
                nickname = friend.nickname;

            // Convert unix timestamp
            string time = Utils.UnixTimeStampToString(Convert.ToDouble(ctransaction.timeStamp));

            webView.Eval(string.Format("setConfirmedData('{0}', '{1}', '{2}', '{3}')", ctransaction.amount.ToString(), addr, nickname, time));
            return false;
        }


    }
}