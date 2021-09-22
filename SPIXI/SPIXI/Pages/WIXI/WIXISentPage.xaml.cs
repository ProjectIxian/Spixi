using IXICore;
using IXICore.Meta;
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

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WIXISentPage : SpixiContentPage
    {
        private Transaction transaction = null;

        public WIXISentPage(Transaction tx)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            transaction = tx;

            loadPage(webView, "wixi_sent.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
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

            if (onNavigatingGlobal(current_url))
            {
                e.Cancel = true;
                return;
            }

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
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
            if (addr.SequenceEqual(IxianHandler.getWalletStorage().getPrimaryAddress()))
            {
                // this is a sent payment

                foreach (var entry in ctransaction.toList)
                {
                    //Utils.sendUiCommand(webView, "addSender", Base58Check.Base58CheckEncoding.EncodePlain(entry.Key) + ": " + entry.Value.ToString(), time);
                    Utils.sendUiCommand(webView, "addSender", Encoding.ASCII.GetString(ctransaction.data) + ": " + entry.Value.ToString(), time);
                }
            }
           
            Utils.sendUiCommand(webView, "setData", amount.ToString(), ctransaction.fee.ToString(),
                time, confirmed_text, (ctransaction.fee / ctransaction.amount).ToString() + "%", Transaction.txIdV8ToLegacy(transaction.id));
            return;
        }

        public override void updateScreen()
        {
            checkTransaction();
        }

        private void onDismiss()
        {
            Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        protected override bool OnBackButtonPressed()
        {
            onDismiss();
            return true;
        }
    }
}