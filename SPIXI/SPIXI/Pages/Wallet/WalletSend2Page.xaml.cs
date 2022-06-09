﻿using IXICore;
using IXICore.Meta;
using IXICore.Utils;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static IXICore.Transaction;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WalletSend2Page : SpixiContentPage
    {
        SortedDictionary<Address, ToEntry> to_list = new SortedDictionary<Address, ToEntry>(new AddressComparer());
        IxiNumber totalAmount = 0;
        Transaction transaction = null;

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
                    displaySpixiAlert(SpixiLocalization._SL("global-invalid-address-title"), SpixiLocalization._SL("global-invalid-address-text"), SpixiLocalization._SL("global-dialog-ok"));
                    return;
                }
                string[] amount_split = amount.Split(new string[] { "." }, StringSplitOptions.None);
                if (amount_split.Length > 2)
                {
                    displaySpixiAlert(SpixiLocalization._SL("wallet-error-amount-title"), SpixiLocalization._SL("wallet-error-amountdecimal-text"), SpixiLocalization._SL("global-dialog-ok"));
                    return;
                }

                IxiNumber _amount = amount;

                if (_amount < (long)0)
                {
                    displaySpixiAlert(SpixiLocalization._SL("wallet-error-amount-title"), SpixiLocalization._SL("wallet-error-amount-text"), SpixiLocalization._SL("global-dialog-ok"));
                    return;
                }

                to_list.AddOrReplace(new Address(_address), new ToEntry(Transaction.getExpectedVersion(IxianHandler.getLastBlockVersion()), _amount));
                totalAmount = totalAmount + _amount;
            }

            loadPage(webView, "wallet_send_2.html");
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            IxiNumber fee = ConsensusConfig.forceTransactionPrice;
            Address from = IxianHandler.getWalletStorage().getPrimaryAddress();
            Address pubKey = new Address(IxianHandler.getWalletStorage().getPrimaryPublicKey());

            transaction = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());

            IxiNumber total_amount = transaction.amount + transaction.fee;

            if (Node.balance.balance < total_amount)
            {
                displaySpixiAlert(SpixiLocalization._SL("wallet-error-balance-title"), string.Format(SpixiLocalization._SL("wallet-error-balance-text"), total_amount.ToString(), Node.balance.balance.ToString()), SpixiLocalization._SL("global-dialog-ok"));
                Navigation.PopAsync(Config.defaultXamarinAnimations);
                return;
            }

            Utils.sendUiCommand(webView, "setFees", transaction.fee.ToString());
            Utils.sendUiCommand(webView, "setBalance", Node.balance.balance.ToString());
            Utils.sendUiCommand(webView, "setTotalAmount", transaction.amount.ToString());
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

            Logging.info("Broadcasting tx");

            IxianHandler.addTransaction(transaction, true);
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
                    FriendMessage friend_message = FriendList.addMessageWithType(null, FriendMessageType.sentFunds, entry.Key, 0, transaction.getTxIdString(), true);

                    SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.sentFunds, transaction.id);

                    StreamMessage message = new StreamMessage();
                    message.type = StreamMessageCode.info;
                    message.recipient = friend.walletAddress;
                    message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
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