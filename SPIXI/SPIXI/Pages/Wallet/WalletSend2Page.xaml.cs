using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WalletSend2Page : SpixiContentPage
    {
        SortedDictionary<byte[], IxiNumber> to_list = new SortedDictionary<byte[], IxiNumber>(new ByteArrayComparer());
        IxiNumber totalAmount = 0;

        public WalletSend2Page(string[] addresses_with_amounts)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            // Go through each entry
            foreach (string address_and_amount in addresses_with_amounts)
            {
                if (address_and_amount.Length < 1)
                    continue;

                // Extract the address and amount
                string[] asplit = address_and_amount.Split(new string[] { ":" }, StringSplitOptions.None);
                if (asplit.Count() < 2)
                    continue;

                string address = asplit[0];
                string amount = asplit[1];

                byte[] _address = Base58Check.Base58CheckEncoding.DecodePlain(address);
                if (Address.validateChecksum(_address) == false)
                {
                    displaySpixiAlert("Invalid address checksum", "Please make sure you typed the address correctly.", "OK");
                    return;
                }
                string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                if (amount_split.Length > 2)
                {
                    displaySpixiAlert("SPIXI", "Please type a correct decimal amount.", "OK");
                    return;
                }

                IxiNumber _amount = amount;

                if (_amount < (long)0)
                {
                    displaySpixiAlert("SPIXI", "Please type a positive amount.", "OK");
                    return;
                }

                to_list.AddOrReplace(_address, _amount);
                totalAmount = totalAmount + _amount;
            }

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/wallet_send_2.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;

        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            IxiNumber fee = ConsensusConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();

            Transaction tmp_tx = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, null, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());

            IxiNumber total_amount = tmp_tx.amount + tmp_tx.fee;

            if (Node.balance.balance < total_amount)
            {
                displaySpixiAlert("Insufficient balance", "Your balance is insufficient for this transaction. Total cost of the transaction is " + total_amount.ToString() + ", while your balance is " + Node.balance.balance.ToString() + ".", "OK");
                Navigation.PopAsync(Config.defaultXamarinAnimations);
                return;
            }

            Utils.sendUiCommand(webView, "setFees", tmp_tx.fee.ToString());
            Utils.sendUiCommand(webView, "setBalance", Node.balance.balance.ToString());
            Utils.sendUiCommand(webView, "setTotalAmount", tmp_tx.amount.ToString());
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
            else if (current_url.Contains("ixian:send:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:send:" }, StringSplitOptions.None);
                // Extract the fee

                // Send the payment
                sendPayment("");
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }


        private void sendPayment(string txfee)
        {
            Logging.info("Preparing to send payment");
            //Navigation.PopAsync(Config.defaultXamarinAnimations);

            // Create an ixian transaction and send it to the dlt network
            IxiNumber fee = ConsensusConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();
            Logging.info("Preparing tx");

            Transaction transaction = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, null, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());
            Logging.info("Broadcasting tx");

            IxianHandler.addTransaction(transaction);
            Logging.info("Adding to cache");

            // Add the unconfirmed transaction to the cache
            TransactionCache.addUnconfirmedTransaction(transaction);
            Logging.info("Showing payment details");

            // Send message to recipients
            foreach (var entry in to_list)
            {
                Friend friend = FriendList.getFriend(entry.Key);

                if (friend != null)
                {
                    FriendMessage friend_message = FriendList.addMessageWithType(null, FriendMessageType.sentFunds, entry.Key, transaction.id, true);

                    SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.sentFunds, Encoding.UTF8.GetBytes(transaction.id));

                    StreamMessage message = new StreamMessage();
                    message.type = StreamMessageCode.info;
                    message.recipient = friend.walletAddress;
                    message.sender = Node.walletStorage.getPrimaryAddress();
                    message.transaction = new byte[1];
                    message.sigdata = new byte[1];
                    message.data = spixi_message.getBytes();
                    message.id = friend_message.id;

                    StreamProcessor.sendMessage(friend, message);
                }
            }

            // Show the payment details
            Navigation.PushAsync(new WalletSentPage(transaction, false), Config.defaultXamarinAnimations);
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}