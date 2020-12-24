using IXICore;
using IXICore.Meta;
using IXICore.Utils;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WIXISendPage : SpixiContentPage
    {
        SortedDictionary<byte[], IxiNumber> to_list = new SortedDictionary<byte[], IxiNumber>(new ByteArrayComparer());

        Transaction transaction = null;


        public WIXISendPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "wixi_send.html");
        }



        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            Utils.sendUiCommand(webView, "setBalance", Node.balance.balance.ToString());

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
            else if (current_url.Equals("ixian:quickscan", StringComparison.Ordinal))
            {
                ICustomQRScanner scanner = DependencyService.Get<ICustomQRScanner>();
                if (scanner != null && scanner.useCustomQRScanner())
                {
                    Logging.error("Custom scanner not implemented");
                    e.Cancel = true;
                    return;
                }
                quickScan();
            }
            else if (current_url.Contains("ixian:qrresult:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:qrresult:" }, StringSplitOptions.None);
                string result = split[1];
                processQRResult(result);
                e.Cancel = true;
                return;
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert(SpixiLocalization._SL("global-invalid-address-title"), SpixiLocalization._SL("global-invalid-address-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
            else if (current_url.Equals("ixian:error2", StringComparison.Ordinal))
            {
                displaySpixiAlert(SpixiLocalization._SL("wallet-error-amount-title"), SpixiLocalization._SL("wallet-error-amount-text"), SpixiLocalization._SL("global-dialog-ok"));
            }
            else if (current_url.Contains("ixian:send:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:send:" }, StringSplitOptions.None);
                if (split.Count() < 2)
                    return;
                string address_and_amount = split[1];

                //displaySpixiAlert(split[0], address_and_amount, SpixiLocalization._SL("global-dialog-ok"));

                string[] asplit = address_and_amount.Split(new string[] { ":" }, StringSplitOptions.None);
                if (asplit.Count() < 2)
                    return;

                string address = asplit[0];
                string _amount = asplit[1];
                IxiNumber amount = _amount;

                if (amount <= (long)0)
                {
                    displaySpixiAlert(SpixiLocalization._SL("wallet-error-amount-title"), SpixiLocalization._SL("wallet-error-amount-text"), SpixiLocalization._SL("global-dialog-ok"));
                    e.Cancel = true;
                    return;
                }

                sendPayment(address, amount);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
        }


        private void sendPayment(string ethaddress, IxiNumber amount)
        {
            IxiNumber fee = ConsensusConfig.transactionPrice;
            byte[] from = Node.walletStorage.getPrimaryAddress();
            byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();


            byte[] _address = Base58Check.Base58CheckEncoding.DecodePlain(Config.bridgeAddress);
            to_list.AddOrReplace(_address, amount);

            byte[] _txdata = Encoding.ASCII.GetBytes(ethaddress);

            transaction = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, _txdata, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());

            IxiNumber total_amount = transaction.amount + transaction.fee;

            if (Node.balance.balance < total_amount)
            {
                displaySpixiAlert(SpixiLocalization._SL("wallet-error-balance-title"), string.Format(SpixiLocalization._SL("wallet-error-balance-text"), total_amount.ToString(), Node.balance.balance.ToString()), SpixiLocalization._SL("global-dialog-ok"));
                Navigation.PopAsync(Config.defaultXamarinAnimations);
                return;
            }

            Logging.info("Preparing to send payment");

            Logging.info("Broadcasting tx");

            IxianHandler.addTransaction(transaction, true);
            Logging.info("Adding to cache");

            // Add the unconfirmed transaction to the cache
            TransactionCache.addUnconfirmedTransaction(transaction);
       

            Navigation.PushAsync(new WIXISentPage(transaction), Config.defaultXamarinAnimations);
        }

        public async void quickScan()
        {
            ICustomQRScanner scanner = DependencyService.Get<ICustomQRScanner>();
            if (scanner != null && scanner.needsPermission())
            {
                if (!await scanner.requestPermission())
                {
                    return;
                }
            }

            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();

            // Restrict to QR codes only
            options.PossibleFormats = new List<ZXing.BarcodeFormat>() {
                ZXing.BarcodeFormat.QR_CODE
            };

            var ScannerPage = new ZXingScannerPage(options);

            ScannerPage.OnScanResult += (result) => {

                ScannerPage.IsScanning = false;

                Device.BeginInvokeOnMainThread(() => {
                    Navigation.PopAsync(Config.defaultXamarinAnimations);

                    processQRResult(result.Text);
                });
            };


            await Navigation.PushAsync(ScannerPage, Config.defaultXamarinAnimations);
        }

        public void processQRResult(string result)
        {
            // Handle direct addresses
            string wallet_to_send = result;

            // Check ethereum address length
            if(wallet_to_send.Length != 42)
            {
                return;
            }

            Utils.sendUiCommand(webView, "setAddress", wallet_to_send);
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}