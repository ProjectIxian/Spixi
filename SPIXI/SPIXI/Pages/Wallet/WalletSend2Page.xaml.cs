using DLT;
using DLT.Meta;
using DLT.Network;
using IXICore;
using SPIXI.Interfaces;
using SPIXI.Storage;
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
    public partial class WalletSend2Page : SpixiContentPage
    {
        SortedDictionary<byte[], IxiNumber> to_list = new SortedDictionary<byte[], IxiNumber>();

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
                    DisplayAlert("Invalid address checksum", "Please make sure you typed the address correctly.", "OK");
                    return;
                }
                string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                if (amount_split.Length > 2)
                {
                    DisplayAlert("SPIXI", "Please type a correct decimal amount.", "OK");
                    return;
                }

                IxiNumber _amount = amount;

                if (_amount < (long)0)
                {
                    DisplayAlert("SPIXI", "Please type a positive amount.", "OK");
                    return;
                }

                to_list.Add(_address, _amount);
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
            webView.Eval(string.Format("setBalance('{0}')", Node.balance.ToString()));
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
            IxiNumber fee = CoreConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();
            Logging.info("Preparing tx");

            Transaction transaction = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, null, pubKey, Node.getLastBlockHeight());
            Logging.info("Broadcasting tx");

            NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.newTransaction, transaction.getBytes(), null);
            Logging.info("Adding to cache");

            // Add the unconfirmed transaction the the cache
            TransactionCache.addUnconfirmedTransaction(transaction);
            Logging.info("Showing payment details");

            // Show the payment details
            Navigation.PushAsync(new WalletSentPage(transaction), Config.defaultXamarinAnimations);
        }

    }
}