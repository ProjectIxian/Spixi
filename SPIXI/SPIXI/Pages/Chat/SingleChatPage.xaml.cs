using IXICore;
using IXICore.Meta;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);
            e.Cancel = true;

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
                onAcceptFriendRequest();
            }
            else if (current_url.Equals("ixian:call", StringComparison.Ordinal))
            {
                displaySpixiAlert("Voice Call", "Coming soon.\nCheck regularly for new version on www.spixi.io", "Ok");

            }
            else if (current_url.Equals("ixian:sendfile", StringComparison.Ordinal))
            {
                onSendFile();
            }
            else if (current_url.Contains("ixian:acceptfile:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:acceptfile:" }, StringSplitOptions.None);
                string id = split[1];

                onAcceptFile(id);
               
            }
            else if (current_url.Contains("ixian:openfile:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:openfile:" }, StringSplitOptions.None);
                string id = split[1];

                FriendMessage fm = friend.messages.Find(x => x.transferId == id);

                // Open file in default app. May not work, check https://forums.xamarin.com/discussion/103042/how-to-open-pdf-or-txt-file-in-default-app-on-xamarin-forms
                //Device.OpenUri(new Uri(transfer.filePath));
                DependencyService.Get<IFileOperations>().open(fm.filePath);

            }
            else if (current_url.Contains("ixian:chat:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:chat:" }, StringSplitOptions.None);
                string msg = split[1];
                if(msg == "/draw") // TODO TODO TODO experimental test
                {
                    byte[][] user_addresses = new byte[][] { friend.walletAddress };
                    CustomAppPage custom_app_page = new CustomAppPage(IxianHandler.getWalletStorage().getPrimaryAddress(), user_addresses, "custom_app.html");
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        Navigation.PushAsync(custom_app_page, Config.defaultXamarinAnimations);
                    });

                    SpixiMessage spixi_msg = new SpixiMessage();
                    spixi_msg.type = SpixiMessageCode.appRequest;
                    spixi_msg.data = (new SpixiAppData(custom_app_page.sessionId, null)).getBytes();

                    StreamMessage new_msg = new StreamMessage();
                    new_msg.type = StreamMessageCode.data;
                    new_msg.recipient = friend.walletAddress;
                    new_msg.sender = Node.walletStorage.getPrimaryAddress();
                    new_msg.transaction = new byte[1];
                    new_msg.sigdata = new byte[1];
                    new_msg.data = spixi_msg.getBytes();
                    new_msg.encryptionType = StreamMessageEncryptionCode.none;

                    StreamProcessor.sendMessage(friend, new_msg);

                    return;
                }
                onSend(msg);
            }else if(current_url.Contains("ixian:viewPayment:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:viewPayment:" }, StringSplitOptions.None);
                string id = split[1];
                onViewPayment(id);
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


        public async void onSend(string str)
        {
            if (str.Length < 1)
            {
                return;
            }

            await Task.Run(async () =>
            {
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
                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.chat, Encoding.UTF8.GetBytes(str));

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();
                message.id = friend_message.id;

                if (friend.bot)
                {
                    message.encryptionType = StreamMessageEncryptionCode.none;
                    message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
                }

                StreamProcessor.sendMessage(friend, message);
            });

        }

        public async System.Threading.Tasks.Task onSendFile()
        {
            try
            {
                FileData fileData = await CrossFilePicker.Current.PickFile();
                if (fileData == null)
                    return; // User canceled file picking

                string fileName = fileData.FileName;
                string filePath = fileData.FilePath;

                FileTransfer transfer = TransferManager.prepareFileTransfer(fileName, fileData.GetStream(), filePath);
                Logging.info("File Transfer uid: " + transfer.uid);

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.fileHeader, transfer.getBytes());

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                StreamProcessor.sendMessage(friend, message);


                string message_data = string.Format("{0}:{1}", transfer.uid, transfer.fileName);

                // store the message and display it
                FriendMessage friend_message = FriendList.addMessageWithType(message.id, FriendMessageType.fileHeader, friend.walletAddress, message_data, true);

                friend_message.transferId = transfer.uid;
                friend_message.filePath = transfer.filePath;

                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);
            }
            catch (Exception ex)
            {
                Logging.error("Exception choosing file: " + ex.ToString());
            }
        }

        public void onAcceptFile(string uid)
        {
            //displaySpixiAlert("File", uid, "Ok");
            TransferManager.acceptFile(friend, uid);
            updateFile(uid, "0", false);
        }

        public void onAcceptFriendRequest()
        {
            friend.approved = true;

            friend.handshakePushed = false;

            StreamProcessor.sendAcceptAdd(friend);
        }

        public void onViewPayment(string msg_id)
        {
            FriendMessage msg = friend.messages.Find(x => x.id.SequenceEqual(Crypto.stringToHash(msg_id)));

            if(msg.type == FriendMessageType.sentFunds || msg.message.StartsWith(":"))
            {
                string id = msg.message;
                if(id.StartsWith(":"))
                {
                    id = id.Substring(1);
                }

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
                        return;
                    }
                }

                Navigation.PushAsync(new WalletSentPage(transaction), Config.defaultXamarinAnimations);

                return;
            }

            if(msg.type == FriendMessageType.requestFunds && !msg.localSender)
            {
                onConfirmPaymentRequest(msg, msg.message);
            }
        }

        public void onConfirmPaymentRequest(FriendMessage msg, string amount)
        {
            // TODO: extract the date from the corresponding message
            DateTime dt = DateTime.Now;
            string date_text = String.Format("{0:t}", dt);
            Navigation.PushAsync(new WalletContactRequestPage(msg, friend, amount, date_text), Config.defaultXamarinAnimations);
        }

        private void onEntryCompleted(object sender, EventArgs e)
        {

        }

        public void loadMessages()
        {
            lock (friend.messages)
            {
                foreach (FriendMessage message in friend.messages)
                {
                    insertMessage(message);
                }
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

            string prefix = "addMe";
            string avatar = "";//Node.localStorage.getOwnAvatarPath();
            string address = "";
            string nick = "";
            if (friend.bot)
            {
                if (message.senderAddress != null)
                {
                    address = Base58Check.Base58CheckEncoding.EncodePlain(message.senderAddress);
                }

                nick = message.senderNick;
                if (nick == "")
                {
                    nick = address;
                }
            }
            if (!message.localSender)
            {
                prefix = "addThem";
                avatar = "img/spixiavatar.png";
            }

            if (message.type == FriendMessageType.requestFunds)
            {
                string status = "WAITING CONFIRMATION";
                string status_icon = "fa-clock";

                string amount = message.message;

                string txid = "";

                bool enableView = false;

                if(!message.localSender)
                {
                    enableView = true;
                }

                if(message.message.StartsWith("::"))
                {
                    status = "DECLINED";
                    status_icon = "fa-exclamation-circle";
                    amount = message.message.Substring(2);
                    txid = Crypto.hashToString(message.id);
                    enableView = false;
                }else if(message.message.StartsWith(":"))
                {
                    status = "PENDING";
                    txid = message.message.Substring(1);

                    Transaction transaction = TransactionCache.getTransaction(txid);
                    if (transaction == null)
                        transaction = TransactionCache.getUnconfirmedTransaction(txid);

                    amount = "?";

                    if (transaction != null)
                    {
                        amount = transaction.amount.ToString();

                        if (transaction.applied > 0)
                        {
                            status = "CONFIRMED";
                            status_icon = "fa-check-circle";
                        }
                    }
                    enableView = true;
                }


                if (message.localSender)
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, "Payment request SENT", amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString(), enableView.ToString());
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, "Payment request RECEIVED", amount, status, status_icon, message.timestamp.ToString(), "", enableView.ToString());
                }
            }

            if (message.type == FriendMessageType.sentFunds)
            {
                Transaction transaction = TransactionCache.getTransaction(message.message);
                if (transaction == null)
                    transaction = TransactionCache.getUnconfirmedTransaction(message.message);

                string status = "PENDING";
                string status_icon = "fa-clock";

                string amount = "?";

                if (transaction != null)
                {
                    if (transaction.applied > 0)
                    {
                        status = "CONFIRMED";
                        status_icon = "fa-check-circle";
                    }
                    amount = transaction.amount.ToString();
                }

                // Call webview methods on the main UI thread only
                if (message.localSender)
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, "Payment SENT", amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString());
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, "Payment RECEIVED", amount, status, status_icon, message.timestamp.ToString());
                }
            }


            if (message.type == FriendMessageType.fileHeader)
            {
                string[] split = message.message.Split(new string[] { ":" }, StringSplitOptions.None);
                if (split != null && split.Length > 1)
                {
                    string uid = split[0];
                    string name = split[1];

                    string progress = "0";
                    if(message.completed)
                    {
                        progress = "100";
                    }
                    Utils.sendUiCommand(webView, "addFile", Crypto.hashToString(message.id), address, nick, avatar, uid, name, message.timestamp.ToString(), message.localSender.ToString(), message.confirmed.ToString(), message.read.ToString(), progress, message.completed.ToString());
                }
            }
            
            if(message.type == FriendMessageType.standard)
            {
                // Normal chat message
                // Call webview methods on the main UI thread only
                Utils.sendUiCommand(webView, prefix, Crypto.hashToString(message.id), address, nick, avatar, message.message, message.timestamp.ToString(), message.confirmed.ToString(), message.read.ToString());
            }

            if (!message.read && !message.localSender)
            {
                Node.shouldRefreshContacts = true;

                message.read = true;
                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);

                // Send read confirmation
                StreamMessage msg_received = new StreamMessage();
                msg_received.type = StreamMessageCode.info;
                msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
                msg_received.recipient = friend.walletAddress;
                msg_received.data = new SpixiMessage(SpixiMessageCode.msgRead, message.id).getBytes();
                msg_received.transaction = new byte[1];
                msg_received.sigdata = new byte[1];

                StreamProcessor.sendMessage(friend, msg_received, true, false, false);
            }
        }

        public void updateMessage(FriendMessage message)
        {
            Utils.sendUiCommand(webView, "updateMessage", Crypto.hashToString(message.id), message.message, message.confirmed.ToString(), message.read.ToString());
        }

        public void updateFile(string uid, string progress, bool complete)
        {
            Utils.sendUiCommand(webView, "updateFile", uid, progress, complete.ToString());
        }

        public void updateGroupChatNicks(byte[] address, string nick)
        {
            Utils.sendUiCommand(webView, "updateGroupChatNicks", Base58Check.Base58CheckEncoding.EncodePlain(address), nick);
        }

        public void updateTransactionStatus(string txid, bool verified)
        {
            string status = "PENDING";
            string status_icon = "fa-clock";

            if (verified)
            {
                status = "CONFIRMED";
                status_icon = "fa-check-circle";
            }

            Utils.sendUiCommand(webView, "updateTransactionStatus", txid, status, status_icon);
        }

        public void updateRequestFundsStatus(byte[] msg_id, string txid, string status)
        {
            string status_icon = "fa-clock";
            bool enableView = true;
            if(status == "DECLINED")
            {
                status_icon = "fa-exclamation-circle";
                enableView = false;
            }
            Utils.sendUiCommand(webView, "updatePaymentRequestStatus", Crypto.hashToString(msg_id), txid, status, status_icon, enableView.ToString());
        }

        // Executed every second
        public override void updateScreen()
        {
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

        protected override bool OnBackButtonPressed()
        {
            friend.chat_page = null;

            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}