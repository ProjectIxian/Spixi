using IXICore;
using IXICore.Meta;
using IXICore.Network;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using SPIXI.CustomApps;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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


        public SingleChatPage(Friend fr)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            _webView = webView;

            Title = fr.nickname;
            friend = fr;

            friend.chat_page = this;

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
            if (friend != null)
            {
                friend.chat_page = this;
            }
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
                if (VoIPManager.isInitiated())
                {
                    VoIPManager.hangupCall(null);
                }
                else
                {
                    VoIPManager.initiateCall(friend);
                }

            }
            else if (current_url.Equals("ixian:sendfile", StringComparison.Ordinal))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                onSendFile();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else if (current_url.Contains("ixian:acceptfile:"))
            {
                string id = current_url.Substring("ixian:acceptfile:".Length);

                FriendMessage fm = friend.messages.Find(x => x.transferId == id);
                if (fm != null)
                {
                    onAcceptFile(fm);
                }else
                {
                    Logging.error("Cannot find message with transfer id: {0}", id);
                }
               
            }
            else if (current_url.Contains("ixian:openfile:"))
            {
                string id = current_url.Substring("ixian:openfile:".Length);

                FriendMessage fm = friend.messages.Find(x => x.transferId == id);

                // Open file in default app. May not work, check https://forums.xamarin.com/discussion/103042/how-to-open-pdf-or-txt-file-in-default-app-on-xamarin-forms
                //Device.OpenUri(new Uri(transfer.filePath));
                if (File.Exists(fm.filePath))
                {
                    DependencyService.Get<IFileOperations>().open(fm.filePath);
                }
            }
            else if (current_url.Contains("ixian:chat:"))
            {
                string msg = current_url.Substring("ixian:chat:".Length);
                onSend(msg);
            }
            else if (current_url.Contains("ixian:viewPayment:"))
            {
                string tx_id = current_url.Substring("ixian:viewPayment:".Length);
                onViewPayment(tx_id);
            }
            else if (current_url.Contains("ixian:app:"))
            {
                string app_id = current_url.Substring("ixian:app:".Length);
                onApp(app_id);
            }
            else if (current_url.StartsWith("ixian:appAccept:"))
            {
                string session_id = current_url.Substring("ixian:appAccept:".Length);
                onAppAccept(session_id);
            }
            else if (current_url.StartsWith("ixian:appReject:"))
            {
                string session_id = current_url.Substring("ixian:appReject:".Length);
                onAppReject(session_id);
            }
            else if (current_url.StartsWith("ixian:hangUp:"))
            {
                if (!App.proximityNear)
                {
                    string session_id = current_url.Substring("ixian:hangUp:".Length);
                    VoIPManager.hangupCall(Crypto.stringToHash(session_id));
                }
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
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DependencyService.Get<ISpixiCodecInfo>().getSupportedAudioCodecs().Count > 0 && !friend.bot)
                {
                    Utils.sendUiCommand(webView, "showCallButton", "");
                }

                loadApps();
            }).Start();

            // Execute timer-related functionality immediately
            updateScreen();

            loadMessages();

            if (FriendList.getUnreadMessageCount() == 0)
            {
                DependencyService.Get<IPushService>().clearNotifications();
            }

            // Set the last message as read
            friend.setLastRead();

            Node.refreshAppRequests = true;
        }


        public async void onSend(string str)
        {
            if (str.Length < 1)
            {
                return;
            }

            await Task.Run(() =>
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

        public async Task onSendFile()
        {
            // Show file picker and send the file
            try
            {
                Stream stream = null;
                string fileName = null;
                string filePath = null;

                // Special case for iOS platform
                if (Device.RuntimePlatform == Device.iOS)
                {
                    var picker_service = DependencyService.Get<IPicturePicker>();

                    SpixiImageData spixi_img_data = await picker_service.PickImageAsync();
                    stream = spixi_img_data.stream;

                    if (stream == null)
                    {
                        return;
                    }

                    fileName = spixi_img_data.name;
                    filePath = spixi_img_data.path;
                }
                else
                {
                    FileData fileData = await CrossFilePicker.Current.PickFile();
                    if (fileData == null)
                        return; // User canceled file picking

                    stream = fileData.GetStream();

                    fileName = fileData.FileName;
                    filePath = fileData.FilePath;
                }

                FileTransfer transfer = TransferManager.prepareFileTransfer(fileName, stream, filePath);
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

                Node.localStorage.writeMessages(friend.walletAddress, friend.messages);
            }
            catch (Exception ex)
            {
                Logging.error("Exception choosing file: " + ex.ToString());
            }
        }

        public void onAcceptFile(FriendMessage message)
        {
            if (TransferManager.getIncomingTransfer(message.transferId) != null)
            {
                Logging.warn("Incoming file transfer {0} already prepared.", message.transferId);
                return;
            }

            //displaySpixiAlert("File", uid, "Ok");
            string file_name = System.IO.Path.GetFileName(message.filePath);

            var ft = new FileTransfer();
            ft.fileName = file_name;
            ft.fileSize = message.fileSize;
            ft.uid = message.transferId;

            ft = TransferManager.prepareIncomingFileTransfer(ft.getBytes(), friend.walletAddress);

            if (ft != null)
            {
                TransferManager.acceptFile(friend, ft.uid);
                updateFile(ft.uid, "0", false);
            }
        }

        public void onAcceptFriendRequest()
        {
            friend.approved = true;

            friend.handshakePushed = false;

            Node.shouldRefreshContacts = true;

            StreamProcessor.sendAcceptAdd(friend, true);
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

        public void onApp(string app_id)
        {
            byte[][] user_addresses = new byte[][] { friend.walletAddress };
            CustomAppPage custom_app_page = new CustomAppPage(app_id, IxianHandler.getWalletStorage().getPrimaryAddress(), user_addresses, Node.customAppManager.getAppEntryPoint(app_id));
            custom_app_page.accepted = true;
            Node.customAppManager.addAppPage(custom_app_page);

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                Navigation.PushAsync(custom_app_page, Config.defaultXamarinAnimations);
            });

            StreamProcessor.sendAppRequest(friend, app_id, custom_app_page.sessionId, null);
        }

        private void onEntryCompleted(object sender, EventArgs e)
        {

        }

        public void loadApps()
        {
            var apps = Node.customAppManager.getInstalledApps();
            lock (apps)
            {
                foreach (CustomApp app in apps.Values)
                {
                    string icon = Node.customAppManager.getAppIconPath(app.id);
                    if(icon == null)
                    {
                        icon = "";
                    }
                    Utils.sendUiCommand(webView, "addApp", app.id, app.name, icon);
                }
            }
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
                    if (!message.read)
                    {
                        message.read = true;
                        Node.localStorage.writeMessages(friend.walletAddress, friend.messages);
                    }
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
            string avatar = "";
            string address = "";
            string nick = "";
            if (!message.localSender)
            {
                if (friend.bot)
                {
                    if (message.senderAddress != null)
                    {
                        address = Base58Check.Base58CheckEncoding.EncodePlain(message.senderAddress);
                    }

                    nick = message.senderNick;
                    if (nick == "")
                    {
                        if (message.senderAddress != null && friend.contacts.ContainsKey(message.senderAddress))
                        {
                            nick = friend.contacts[message.senderAddress].nick;
                        }
                    }

                    if (nick == "")
                    {
                        nick = address;
                    }
                }

                prefix = "addThem";
                if(message.senderAddress != null)
                {
                    avatar = Node.localStorage.getAvatarPath(Base58Check.Base58CheckEncoding.EncodePlain(message.senderAddress));
                }else
                {
                    avatar = Node.localStorage.getAvatarPath(Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                }
                if (avatar == null)
                {
                    avatar = "img/spixiavatar.png";
                }
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

                if (message.message.StartsWith("::"))
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

                    bool confirmed = true;
                    Transaction transaction = TransactionCache.getTransaction(txid);
                    if (transaction == null)
                    {
                        transaction = TransactionCache.getUnconfirmedTransaction(txid);
                        confirmed = false;
                    }

                    amount = "?";

                    if (transaction != null)
                    {
                        amount = transaction.amount.ToString();

                        if (confirmed)
                        {
                            status = "CONFIRMED";
                            status_icon = "fa-check-circle";
                        }
                    }
                    else
                    {
                        // TODO think about how to make this more private
                        CoreProtocolMessage.broadcastGetTransaction(txid, 0, null);
                    }
                    enableView = true;
                }


                if (message.localSender)
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, "Payment request SENT", amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString(), message.confirmed.ToString(), message.read.ToString(), enableView.ToString());
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, "Payment request RECEIVED", amount, status, status_icon, message.timestamp.ToString(), "", message.confirmed.ToString(), message.read.ToString(), enableView.ToString());
                }
            }

            if (message.type == FriendMessageType.sentFunds)
            {
                bool confirmed = true;
                Transaction transaction = TransactionCache.getTransaction(message.message);
                if (transaction == null)
                {
                    transaction = TransactionCache.getUnconfirmedTransaction(message.message);
                    confirmed = false;
                }

                string status = "PENDING";
                string status_icon = "fa-clock";

                string amount = "?";

                if (transaction != null)
                {
                    if (confirmed)
                    {
                        status = "CONFIRMED";
                        status_icon = "fa-check-circle";
                    }
                    if(message.localSender)
                    {
                        amount = transaction.amount.ToString();
                    }else
                    {
                        amount = HomePage.calculateReceivedAmount(transaction).ToString();
                    }
                }
                else
                {
                    // TODO think about how to make this more private
                    CoreProtocolMessage.broadcastGetTransaction(message.message, 0, null);
                }

                // Call webview methods on the main UI thread only
                if (message.localSender)
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, "Payment SENT", amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString(), message.confirmed.ToString(), message.read.ToString(), "True");
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, "Payment RECEIVED", amount, status, status_icon, message.timestamp.ToString(), "", message.confirmed.ToString(), message.read.ToString(), "True");
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
            updateMessageReadStatus(message);
        }

        private void updateMessageReadStatus(FriendMessage message)
        {
            if (!message.read && !message.localSender && App.isInForeground)
            {
                Node.shouldRefreshContacts = true;

                message.read = true;
                Node.localStorage.writeMessages(friend.walletAddress, friend.messages);

                if (!friend.bot)
                {
                    // Send read confirmation
                    StreamMessage msg_received = new StreamMessage();
                    msg_received.type = StreamMessageCode.info;
                    msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
                    msg_received.recipient = friend.walletAddress;
                    msg_received.data = new SpixiMessage(SpixiMessageCode.msgRead, message.id).getBytes();
                    msg_received.transaction = new byte[1];
                    msg_received.sigdata = new byte[1];

                    StreamProcessor.sendMessage(friend, msg_received, true, true, false, true);
                }
            }
        }

        public void updateMessagesReadStatus()
        {
            if(friend == null)
            {
                return;
            }
            lock (friend.messages)
            {
                int max_msg_count = 0;
                if (friend.messages.Count > 50)
                {
                    max_msg_count = friend.messages.Count - 50;
                }

                for (int i = friend.messages.Count - 1; i >= max_msg_count; i--)
                {
                    FriendMessage msg = friend.messages[i];
                    updateMessageReadStatus(msg);
                }
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
            base.updateScreen();

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
            if (NetworkClientManager.getConnectedClients(true).Count() > 0)
            {
                if (!Config.enablePushNotifications && (friend.relayIP == null || StreamClientManager.isConnectedTo(friend.relayIP, true) == null))
                {
                    Utils.sendUiCommand(webView, "showWarning", "Connecting to Ixian S2...");
                }
                else
                {
                    Utils.sendUiCommand(webView, "showWarning", "");
                }
            }
            else
            {
                Utils.sendUiCommand(webView, "showWarning", "Connecting to Ixian Network...");
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

        public override void onResume()
        {
            base.onResume();

            if(FriendList.getUnreadMessageCount() == 0)
            {
                DependencyService.Get<IPushService>().clearNotifications();
            }

            updateMessagesReadStatus();
        }
    }
}