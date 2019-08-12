using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Net;
using System.Text;
using System.Timers;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleChatPage : SpixiContentPage
    {
        private Friend friend = null;

        private int lastMessageCount = 0;
        private bool connectedToNode = false;
        private string node_ip = "";


        public SingleChatPage(Friend fr)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            Title = fr.nickname;
            friend = fr;

            friend.chat_page = this;

            // Connect to the friend's S2 node
            node_ip = friend.searchForRelay();

            //TODOSPIXI
            //NetworkClientManager.connectToStreamNode(node_ip);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/chat.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        public override void recalculateLayout()
        {
            ForceLayout();
        }

        protected override void OnAppearing()
        {
            if(friend != null)
                friend.chat_page = this;
            base.OnAppearing();
        }


        protected override void OnDisappearing()
        {

            base.OnDisappearing();
        //    this.Content = null;
        //    GC.Collect();
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
                friend.chat_page = null;

                Navigation.PopAsync(Config.defaultXamarinAnimations);

            }
            else if (current_url.Equals("ixian:request", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletReceivePage(friend), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:details", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new ContactDetails(friend, true), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletSendPage(friend.walletAddress), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:accept", StringComparison.Ordinal))
            {
                onAccept();
            }
            else if (current_url.Equals("ixian:call", StringComparison.Ordinal))
            {
                
            }
            else if (current_url.Equals("ixian:sendfile", StringComparison.Ordinal))
            {

            }
            else if (current_url.Contains("ixian:chat:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:chat:" }, StringSplitOptions.None);
                string id = WebUtility.UrlDecode(split[1]);
                onSend(id);
            }
            else if (current_url.Contains("ixian:confirmrequest:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:confirmrequest:" }, StringSplitOptions.None);
                string amount = split[1];
                onConfirmPaymentRequest(amount);
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

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            loadMessages();
            Utils.sendUiCommand(webView, "setNickname", friend.nickname);

            // Execute timer-related functionality immediately
            updateScreen();

            // Set the last message as read
            friend.setLastRead();
        }


        public void onSend(string str)
        {
            if (str.Length < 1)
            {
                return;
            }

            // TODOSPIXI
            /*            // Send the message to the S2 nodes
                        byte[] recipient_address = friend.wallet_address;
                        byte[] encrypted_message = StreamProcessor.prepareSpixiMessage(SpixiMessageCode.chat, str, friend.pubkey);
                        // CryptoManager.lib.encryptData(Encoding.UTF8.GetBytes(string_to_send), friend.pubkey);

                        // Check the relay ip
                        string relayip = friend.getRelayIP();
                        if (relayip == null)
                        {
                            Logging.warn("No relay node to send message to!");
                            return;
                        }
                        if (relayip.Equals(node_ip, StringComparison.Ordinal) == false)
                        {

                            node_ip = relayip;
                            // Connect to the contact's S2 relay first
                            NetworkClientManager.connectToStreamNode(relayip);

                            // TODO: optimize this
                            while (NetworkClientManager.isNodeConnected(relayip) == false)
                            {

                            }
                        }

                        Message message = new Message();
                        message.recipientAddress = recipient_address;
                        message.data = encrypted_message;

                        StreamProcessor.sendMessage(message, node_ip);*/

            // store the message and display it
            FriendMessage friend_message = FriendList.addMessageWithType(null, FriendMessageType.standard, friend.walletAddress, str, true);

            // Finally, clear the input field
            Utils.sendUiCommand(webView, "clearInput");

            // Send the message
            SpixiMessage spixi_message = new SpixiMessage(friend_message.id, SpixiMessageCode.chat, Encoding.UTF8.GetBytes(str));

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.data;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            StreamProcessor.sendMessage(friend, message);

        }

        public void onAccept()
        {
            FriendList.resetHiddenMatchAddressesCache();

            friend.approved = true;

            StreamProcessor.sendAcceptAdd(friend);
        }

        public void onConfirmPaymentRequest(string amount)
        {
            // TODO: extract the date from the corresponding message
            DateTime dt = DateTime.Now;
            string date_text = String.Format("{0:t}", dt);
            Navigation.PushAsync(new WalletContactRequestPage(friend, amount, date_text), Config.defaultXamarinAnimations);
        }

        private void onEntryCompleted(object sender, EventArgs e)
        {

        }

        public void loadMessages()
        {
            foreach (FriendMessage message in friend.messages)
            {
                insertMessage(message);
            }
        }

        public void insertMessage(FriendMessage message)
        {
            if (friend.approved == false)
            {
                if (message.type == FriendMessageType.requestAdd)
                {

                    // Call webview methods on the main UI thread only
                    Utils.sendUiCommand(webView, "showContactRequest", "1");
                    message.read = true;
                    return;
                }
            }
            else
            {
                // Don't show if the friend is already approved
                if (message.type == FriendMessageType.requestAdd)
                    return;
            }


            if (message.type == FriendMessageType.requestFunds)
            {
                // Call webview methods on the main UI thread only
                if (message.localSender)
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", "Payment request for " + message.message + " IxiCash has been sent.", "0", Clock.getRelativeTime(message.timestamp));
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", friend.nickname + " has sent a payment request" + " for " + message.message + " IxiCash.", message.message, Clock.getRelativeTime(message.timestamp));
                }
                message.read = true;
                return;
            }

            if (message.type == FriendMessageType.sentFunds)
            {
                message.read = true;
                Transaction transaction = TransactionCache.getTransaction(message.message);
                if (transaction == null)
                    transaction = TransactionCache.getUnconfirmedTransaction(message.message);

                if (transaction == null)
                    return;
                // Call webview methods on the main UI thread only
                Utils.sendUiCommand(webView, "addPaymentSent", transaction.amount.ToString(), message.message);
                return;
            }

            string prefix = "addMe";
            string avatar = "";//Node.localStorage.getOwnAvatarPath();
            if (!message.localSender)
            {
                prefix = "addThem";
                avatar = "img/spixiavatar.png";
            }
            // Call webview methods on the main UI thread only
            Utils.sendUiCommand(webView, prefix, Crypto.hashToString(message.id), avatar, message.message, Clock.getRelativeTime(message.timestamp), message.confirmed.ToString(), message.read.ToString());

            if (!message.read && !message.localSender)
            {
                message.read = true;
                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);

                // Send read confirmation
                StreamMessage msg_received = new StreamMessage();
                msg_received.type = StreamMessageCode.info;
                msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
                msg_received.recipient = friend.walletAddress;
                msg_received.data = new SpixiMessage(message.id, SpixiMessageCode.msgRead, null).getBytes();
                msg_received.transaction = new byte[1];
                msg_received.sigdata = new byte[1];
                msg_received.encryptionType = StreamMessageEncryptionCode.none;

                StreamProcessor.sendMessage(friend, msg_received, true);
            }
        }

        public void updateMessage(FriendMessage message)
        {
            Logging.info("Sending update message for {0}, content {1} ", Crypto.hashToString(message.id), message.message);
            Utils.sendUiCommand(webView, "updateMessage", Crypto.hashToString(message.id), message.message, message.confirmed.ToString(), message.read.ToString());
        }

        // Executed every second
        public override void updateScreen()
        {
            Logging.info("Updating chat");

            Utils.sendUiCommand(webView, "setNickname", friend.nickname);

            if (friend.online)
            {
                Utils.sendUiCommand(webView, "showIndicator", "true");
            }
            else
            {
                Utils.sendUiCommand(webView, "showIndicator", "false");
            }

            // Show connectivity warning bar
            if (StreamClientManager.isConnectedTo(node_ip) == null)
            {
                if (connectedToNode == true)
                {
                    connectedToNode = false;
                    Utils.sendUiCommand(webView, "showWarning", "Not connected to S2 node");
                }
            }
            else
            {
                if(connectedToNode == false)
                {
                    connectedToNode = true;
                    Utils.sendUiCommand(webView, "showWarning", "");
                }
            }
            

            // Show the messages indicator
            int msgCount = FriendList.getUnreadMessageCount();
            if(msgCount != lastMessageCount)
            {
                lastMessageCount = msgCount;
                //webView.Eval(string.Format("showUnread({0})", lastMessageCount));
            }
        }
    }
}