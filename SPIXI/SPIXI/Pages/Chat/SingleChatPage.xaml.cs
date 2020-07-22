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
using SPIXI.Lang;
using IXICore.SpixiBot;
using System.ComponentModel;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleChatPage : SpixiContentPage
    {
        private Friend friend = null;

        private int lastMessageCount = 0;

        private int selectedChannel = 0;

        public SingleChatPage(Friend fr)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            Title = fr.nickname;
            friend = fr;

            friend.chat_page = this;

            loadPage(webView, "chat.html");
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

            if (onNavigatingGlobal(current_url))
            {
                return;
            }

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
            else if (current_url.StartsWith("ixian:acceptfile:"))
            {
                string id = current_url.Substring("ixian:acceptfile:".Length);

                FriendMessage fm = friend.getMessages(selectedChannel).Find(x => x.transferId == id);
                if (fm != null)
                {
                    onAcceptFile(selectedChannel, fm);
                }
                else
                {
                    Logging.error("Cannot find message with transfer id: {0}", id);
                }

            }
            else if (current_url.StartsWith("ixian:openfile:"))
            {
                string id = current_url.Substring("ixian:openfile:".Length);

                FriendMessage fm = friend.getMessages(selectedChannel).Find(x => x.transferId == id);

                // Open file in default app. May not work, check https://forums.xamarin.com/discussion/103042/how-to-open-pdf-or-txt-file-in-default-app-on-xamarin-forms
                //Device.OpenUri(new Uri(transfer.filePath));
                if (File.Exists(fm.filePath))
                {
                    DependencyService.Get<IFileOperations>().open(fm.filePath);
                }
            }
            else if (current_url.StartsWith("ixian:chat:"))
            {
                string msg = current_url.Substring("ixian:chat:".Length);
                onSend(msg);
            }
            else if (current_url.StartsWith("ixian:viewPayment:"))
            {
                string tx_id = current_url.Substring("ixian:viewPayment:".Length);
                onViewPayment(tx_id);
            }
            else if (current_url.StartsWith("ixian:app:"))
            {
                string app_id = current_url.Substring("ixian:app:".Length);
                onApp(app_id);
            }
            else if (current_url.StartsWith("ixian:loadContacts"))
            {
                loadContacts();
            }
            else if (current_url.StartsWith("ixian:populateChannelSelector"))
            {
                populateChannelSelector();
            }
            else if (current_url.StartsWith("ixian:selectChannel:"))
            {
                int sel_channel = Int32.Parse(current_url.Substring("ixian:selectChannel:".Length));
                BotChannel channel = friend.channels.getChannel(friend.channels.channelIndexToName(sel_channel));
                if (channel != null)
                {
                    Utils.sendUiCommand(webView, "setSelectedChannel", channel.index.ToString(), "fa-globe-africa", channel.channelName);
                    selectedChannel = sel_channel;
                    loadMessages();
                }
            }
            else if (current_url.StartsWith("ixian:contextAction:"))
            {
                string action = current_url.Substring("ixian:contextAction:".Length);
                action = action.Substring(0, action.IndexOf(':'));

                string msg_id = current_url.Substring("ixian:contextAction:".Length + action.Length + 1);
                onContextAction(action, msg_id);
            }
            else if (current_url.StartsWith("ixian:enableNotifications"))
            {
                friend.users.getUser(Node.walletStorage.getPrimaryAddress()).sendNotification = true;
                StreamProcessor.sendBotAction(friend, SpixiBotActionCode.enableNotifications, new byte[1] { 1 }, 0, true);
            }
            else if (current_url.StartsWith("ixian:disableNotifications"))
            {
                friend.users.getUser(Node.walletStorage.getPrimaryAddress()).sendNotification = false;
                StreamProcessor.sendBotAction(friend, SpixiBotActionCode.enableNotifications, new byte[1] { 0 }, 0, true);
            }
            else if (current_url.StartsWith("ixian:sendContactRequest:"))
            {
                byte[] address = Base58Check.Base58CheckEncoding.DecodePlain(current_url.Substring("ixian:sendContactRequest:".Length));
                Friend new_friend = FriendList.addFriend(address, null, Base58Check.Base58CheckEncoding.EncodePlain(address), null, null, 0);
                if (new_friend != null)
                {
                    FriendList.saveToStorage();

                    StreamProcessor.sendContactRequest(new_friend);
                }
            }
            else if (current_url.StartsWith("ixian:kick:"))
            {
                byte[] address = Base58Check.Base58CheckEncoding.DecodePlain(current_url.Substring("ixian:kick:".Length));
                StreamProcessor.sendBotAction(friend, SpixiBotActionCode.kickUser, address, 0, true);
                string modal_title = String.Format(SpixiLocalization._SL("chat-modal-kicked-title"), address);
                string modal_body = String.Format(SpixiLocalization._SL("chat-modal-kicked-body"), address);
                displaySpixiAlert(modal_title, modal_body, SpixiLocalization._SL("global-dialog-ok"));
            }
            else if (current_url.StartsWith("ixian:ban:"))
            {
                byte[] address = Base58Check.Base58CheckEncoding.DecodePlain(current_url.Substring("ixian:ban:".Length));
                StreamProcessor.sendBotAction(friend, SpixiBotActionCode.banUser, address, 0, true);
                string modal_title = String.Format(SpixiLocalization._SL("chat-modal-banned-title"), address);
                string modal_body = String.Format(SpixiLocalization._SL("chat-modal-banned-body"), address);
                displaySpixiAlert(modal_title, modal_body, SpixiLocalization._SL("global-dialog-ok"));
            }
            else if (current_url.StartsWith("ixian:typing"))
            {
                StreamProcessor.sendTyping(friend);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
        }

        private void populateChannelSelector()
        {
            var channels = friend.channels.channels;
            lock(channels)
            {
                foreach(var channel in channels.Values)
                {
                    string icon = "fa-globe-africa";
                    Utils.sendUiCommand(webView, "addChannelToSelector", channel.index.ToString(), channel.channelName, icon);
                }
            }
        }

        private void loadContacts()
        {
            var contacts = friend.users.contacts;
            lock (contacts)
            {
                foreach (var contact in contacts)
                {
                    string address = Base58Check.Base58CheckEncoding.EncodePlain(contact.Key);
                    string avatar = Node.localStorage.getAvatarPath(address);
                    if (avatar == null)
                    {
                        avatar = "img/spixiavatar.png";
                    }
                    int role = contact.Value.getPrimaryRole();
                    Utils.sendUiCommand(webView, "addContact",  address, contact.Value.getNick(), avatar, role.ToString());
                }
            }
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            if(friend.bot)
            {
                string cost_text = String.Format(SpixiLocalization._SL("chat-message-cost-bar"), friend.botInfo.cost.ToString() + " IXI");
                Utils.sendUiCommand(webView, "setBotMode", friend.bot.ToString(), friend.botInfo.cost.ToString(), cost_text, friend.botInfo.admin.ToString(), friend.botInfo.serverDescription, friend.users.getUser(Node.walletStorage.getPrimaryAddress()).sendNotification.ToString(), Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                if (selectedChannel == 0 && friend.channels.channels.Count > 0)
                {
                    selectedChannel = friend.botInfo.defaultChannel;
                }
                if (selectedChannel != 0)
                {
                    BotChannel channel = friend.channels.getChannel(friend.channels.channelIndexToName(selectedChannel));
                    if(channel != null)
                    {
                        Utils.sendUiCommand(webView, "setSelectedChannel", channel.index.ToString(), "fa-globe-africa", channel.channelName);
                    }
                }else
                {
                    selectedChannel = 0;
                }
            }else
            {
                Utils.sendUiCommand(webView, "setBotMode", "False", "0.00000000", "", "False");
            }
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                if (DependencyService.Get<ISpixiCodecInfo>().getSupportedAudioCodecs().Count > 0)
                {
                    Utils.sendUiCommand(webView, "showCallButton", "");
                }

                loadApps();
            }).Start();

            // Execute timer-related functionality immediately
            updateScreen();

            loadMessages();

            Utils.sendUiCommand(webView, "onChatScreenLoaded");

            if (FriendList.getUnreadMessageCount() == 0)
            {
                DependencyService.Get<IPushService>().clearNotifications();
            }

            Node.refreshAppRequests = true;
        }


        public async void onSend(string str)
        {
            if (str.Length < 1)
            {
                return;
            }

            if(friend.bot)
            {
                if (friend.botInfo.cost > 0)
                {
                    IxiNumber message_cost = friend.getMessagePrice(str.Length);
                    if (message_cost > 0)
                    {
                        Transaction tx = new Transaction((int)Transaction.Type.Normal, message_cost, ConsensusConfig.transactionPrice, friend.walletAddress, Node.walletStorage.getPrimaryAddress(), null, Node.walletStorage.getPrimaryPublicKey(), IxianHandler.getHighestKnownNetworkBlockHeight());
                        IxiNumber balance = IxianHandler.getWalletBalance(Node.walletStorage.getPrimaryAddress());
                        if (tx.amount + tx.fee > balance)
                        {
                            string alert_body = String.Format(SpixiLocalization._SL("wallet-error-balance-text"), tx.amount + tx.fee, balance);
                            await displaySpixiAlert(SpixiLocalization._SL("wallet-error-balance-title"), alert_body, SpixiLocalization._SL("global-dialog-ok"));
                            return;
                        }
                    }
                }
            }

            await Task.Run(() =>
            {
                // Send the message
                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.chat, Encoding.UTF8.GetBytes(str), selectedChannel);
                byte[] spixi_msg_bytes = spixi_message.getBytes();

                // store the message and display it
                FriendMessage friend_message = FriendList.addMessageWithType(null, FriendMessageType.standard, friend.walletAddress, selectedChannel, str, true, null, 0, true, spixi_msg_bytes.Length);

                // Finally, clear the input field
                Utils.sendUiCommand(webView, "clearInput");


                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.data;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_msg_bytes;
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
                transfer.channel = selectedChannel;
                Logging.info("File Transfer uid: " + transfer.uid);

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.fileHeader, transfer.getBytes(), selectedChannel);

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
                FriendMessage friend_message = FriendList.addMessageWithType(message.id, FriendMessageType.fileHeader, friend.walletAddress, selectedChannel, message_data, true);

                friend_message.transferId = transfer.uid;
                friend_message.filePath = transfer.filePath;

                Node.localStorage.writeMessages(friend.walletAddress, selectedChannel, friend.getMessages(selectedChannel));
            }
            catch (Exception ex)
            {
                Logging.error("Exception choosing file: " + ex.ToString());
            }
        }

        public void onAcceptFile(int selected_channel, FriendMessage message)
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
            ft.channel = selected_channel;

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
            FriendMessage msg = friend.getMessages(selectedChannel).Find(x => x.id.SequenceEqual(Crypto.stringToHash(msg_id)));

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

            FriendList.addMessageWithType(custom_app_page.sessionId, FriendMessageType.appSession, friend.walletAddress, 0, app_id, true, null, 0, false);
            StreamProcessor.sendAppRequest(friend, app_id, custom_app_page.sessionId, null);
        }

        private void onContextAction(string action, string msg_id_hex)
        {
            string data = "";
            if (msg_id_hex.Contains(':'))
            {
                int sep_offset = msg_id_hex.IndexOf(':');
                data = msg_id_hex.Substring(sep_offset + 1);
                msg_id_hex = msg_id_hex.Substring(0, sep_offset);
            }
            byte[] msg_id = Crypto.stringToHash(msg_id_hex);
            switch(action)
            {
                case "tip":
                    FriendMessage msg = friend.getMessages(selectedChannel).Find(x => x.id.SequenceEqual(msg_id));
                    byte[] sender_address = msg.senderAddress;
                    if(!friend.bot)
                    {
                        sender_address = friend.walletAddress;
                    }
                    IxiNumber amount = new IxiNumber(data);
                    Transaction tx = new Transaction((int)Transaction.Type.Normal, amount, ConsensusConfig.transactionPrice, sender_address, Node.walletStorage.getPrimaryAddress(), null, Node.walletStorage.getPrimaryPublicKey(), IxianHandler.getHighestKnownNetworkBlockHeight());
                    IxiNumber balance = IxianHandler.getWalletBalance(Node.walletStorage.getPrimaryAddress());
                    if (tx.amount + tx.fee > balance)
                    {
                        string alert_body = String.Format(SpixiLocalization._SL("wallet-error-balance-text"), tx.amount + tx.fee, balance);
                        displaySpixiAlert(SpixiLocalization._SL("wallet-error-balance-title"), alert_body, SpixiLocalization._SL("global-dialog-ok"));
                    }
                    else
                    {
                        string nick = friend.nickname;
                        if (friend.bot)
                        {
                            nick = friend.users.getUser(sender_address).getNick();
                        }
                        string modal_title = String.Format(SpixiLocalization._SL("chat-modal-tip-title"), nick);
                        if (friend.addReaction(Node.walletStorage.getPrimaryAddress(), new SpixiMessageReaction(msg_id, "tip:" + tx.id), selectedChannel))
                        {
                            StreamProcessor.sendReaction(friend, msg_id, "tip:" + tx.id, selectedChannel);
                            IxianHandler.addTransaction(tx);
                            TransactionCache.addUnconfirmedTransaction(tx);
                            string modal_body = String.Format(SpixiLocalization._SL("chat-modal-tip-confirmed-body"), nick, amount.ToString() + " IXI");
                            displaySpixiAlert(modal_title, modal_body, SpixiLocalization._SL("global-dialog-ok"));
                        }
                        else
                        {
                            displaySpixiAlert(modal_title, SpixiLocalization._SL("chat-modal-tip-error-body"), SpixiLocalization._SL("global-dialog-ok"));
                        }
                    }
                    break;
                case "sendContactRequest":
                    byte[] new_friend_address = friend.getMessages(selectedChannel).Find(x => x.id.SequenceEqual(msg_id)).senderAddress;
                    Friend new_friend = FriendList.addFriend(new_friend_address, null, Base58Check.Base58CheckEncoding.EncodePlain(new_friend_address), null, null, 0);
                    if (new_friend != null)
                    {
                        FriendList.saveToStorage();

                        StreamProcessor.sendContactRequest(new_friend);
                    }
                    break;
                case "kickUser":
                    StreamProcessor.sendBotAction(friend, SpixiBotActionCode.kickUser, friend.getMessages(selectedChannel).Find(x => x.id.SequenceEqual(msg_id)).senderAddress, 0, true);
                    break;
                case "banUser":
                    StreamProcessor.sendBotAction(friend, SpixiBotActionCode.banUser, friend.getMessages(selectedChannel).Find(x => x.id.SequenceEqual(msg_id)).senderAddress, 0, true);
                    break;
                case "deleteMessage":
                    StreamProcessor.sendMsgDelete(friend, msg_id, selectedChannel);
                    if (!friend.bot)
                    {
                        friend.deleteMessage(msg_id, selectedChannel);
                    }
                    break;
                case "like":
                    if (friend.addReaction(Node.walletStorage.getPrimaryAddress(), new SpixiMessageReaction(msg_id, "like:"), selectedChannel))
                    {
                        StreamProcessor.sendReaction(friend, msg_id, "like:", selectedChannel);
                    }
                    break;
            }
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
            Utils.sendUiCommand(webView, "clearMessages");
            var messages = friend.getMessages(selectedChannel);
            lock (messages)
            {
                int skip_messages = 0;
                if(messages.Count > 100)
                {
                    skip_messages = messages.Count() - 100;
                }
                foreach (FriendMessage message in messages)
                {
                    if(skip_messages > 0)
                    {
                        skip_messages--;
                        continue;
                    }
                    insertMessage(message, selectedChannel);
                    updateReactions(message);
                }
            }
        }

        public void insertMessage(FriendMessage message, int channel)
        {
            if(channel != selectedChannel)
            {
                return;
            }
            if (friend.approved == false)
            {
                if (message.type == FriendMessageType.requestAdd)
                {

                    // Call webview methods on the main UI thread only
                    Utils.sendUiCommand(webView, "showContactRequest", "1");
                    if (!message.read)
                    {
                        message.read = true;
                        Node.localStorage.writeMessages(friend.walletAddress, channel, friend.getMessages(channel));
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
            string address = friend.nickname;
            if(address == "")
            {
                address = Base58Check.Base58CheckEncoding.EncodePlain(message.senderAddress);
            }
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
                        if (message.senderAddress != null && friend.users.hasUser(message.senderAddress))
                        {
                            nick = friend.users.getUser(message.senderAddress).getNick();
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
                string status = SpixiLocalization._SL("chat-payment-status-waiting-confirmation");
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
                    status = SpixiLocalization._SL("chat-payment-status-declined");
                    status_icon = "fa-exclamation-circle";
                    amount = message.message.Substring(2);
                    txid = Crypto.hashToString(message.id);
                    enableView = false;
                }else if(message.message.StartsWith(":"))
                {
                    status = SpixiLocalization._SL("chat-payment-status-pending");
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
                            status = SpixiLocalization._SL("chat-payment-status-confirmed");
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
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, SpixiLocalization._SL("chat-payment-request-sent"), amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString(), message.confirmed.ToString(), message.read.ToString(), enableView.ToString());
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), txid, address, nick, avatar, SpixiLocalization._SL("chat-payment-request-received"), amount, status, status_icon, message.timestamp.ToString(), "", message.confirmed.ToString(), message.read.ToString(), enableView.ToString());
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

                string status = SpixiLocalization._SL("chat-payment-status-pending");
                string status_icon = "fa-clock";

                string amount = "?";

                if (transaction != null)
                {
                    if (confirmed)
                    {
                        status = SpixiLocalization._SL("chat-payment-status-confirmed");
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
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, SpixiLocalization._SL("chat-payment-sent"), amount, status, status_icon, message.timestamp.ToString(), message.localSender.ToString(), message.confirmed.ToString(), message.read.ToString(), "True");
                }
                else
                {
                    Utils.sendUiCommand(webView, "addPaymentRequest", Crypto.hashToString(message.id), message.message, address, nick, avatar, SpixiLocalization._SL("chat-payment-received"), amount, status, status_icon, message.timestamp.ToString(), "", message.confirmed.ToString(), message.read.ToString(), "True");
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

            if(message.type == FriendMessageType.voiceCall || message.type == FriendMessageType.voiceCallEnd)
            {
                string text;
                if(message.localSender)
                {
                    text = SpixiLocalization._SL("chat-call-outgoing");
                }else
                {
                    text = SpixiLocalization._SL("chat-call-incoming");
                }
                bool declined = false;
                if(message.message == "")
                {
                    if(message.type == FriendMessageType.voiceCallEnd || !VoIPManager.hasSession(message.id))
                    {
                        declined = true;
                        if (message.localSender)
                        {
                            text = SpixiLocalization._SL("chat-call-no-answer");
                        }
                        else
                        {
                            text = SpixiLocalization._SL("chat-call-missed");
                        }
                    }
                }else if(message.type == FriendMessageType.voiceCallEnd)
                {
                    long seconds = Int32.Parse(message.message);
                    long minutes = seconds / 60;
                    seconds = seconds % 60;
                    text = string.Format("{0} ({1}:{2})", text, minutes, seconds < 10 ? "0" + seconds : seconds.ToString());
                }
                Utils.sendUiCommand(webView, "addCall", Crypto.hashToString(message.id), text, declined.ToString(), message.timestamp.ToString());
            }

            updateMessageReadStatus(message, channel);
        }

        private void updateMessageReadStatus(FriendMessage message, int channel)
        {
            if (!message.read && !message.localSender && App.isInForeground)
            {
                Node.shouldRefreshContacts = true;

                message.read = true;
                Node.localStorage.writeMessages(friend.walletAddress, channel, friend.getMessages(channel));

                if (!friend.bot)
                {
                    // Send read confirmation
                    StreamMessage msg_received = new StreamMessage();
                    msg_received.type = StreamMessageCode.info;
                    msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
                    msg_received.recipient = friend.walletAddress;
                    msg_received.data = new SpixiMessage(SpixiMessageCode.msgRead, message.id, selectedChannel).getBytes();
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
            var messages = friend.getMessages(selectedChannel);
            lock (messages)
            {
                int max_msg_count = 0;
                if (messages.Count > 50)
                {
                    max_msg_count = messages.Count - 50;
                }

                for (int i = messages.Count - 1; i >= max_msg_count; i--)
                {
                    FriendMessage msg = messages[i];
                    updateMessageReadStatus(msg, selectedChannel);
                }
            }
        }

        public void deleteMessage(byte[] msg_id, int channel)
        {
            if (channel == selectedChannel)
            {
                Utils.sendUiCommand(webView, "deleteMessage", Crypto.hashToString(msg_id));
            }
        }

        public void showTyping()
        {
            Utils.sendUiCommand(webView, "showUserTyping");
        }

        public void updateReactions(byte[] msg_id, int channel)
        {
            if (channel == selectedChannel)
            {
                FriendMessage fm = friend.getMessages(channel).Find(x => x.id.SequenceEqual(msg_id));
                if (fm != null)
                {
                    updateReactions(fm);
                }
            }
        }

        private void updateReactions(FriendMessage fm)
        {
            var reactions_str = "";
            foreach (var reaction in fm.reactions)
            {
                reactions_str += reaction.Key + ":" + reaction.Value.Count() + ";";
            }
            Utils.sendUiCommand(webView, "addReactions", Crypto.hashToString(fm.id), reactions_str);
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
            string status = SpixiLocalization._SL("chat-payment-status-pending");
            string status_icon = "fa-clock";

            if (verified)
            {
                status = SpixiLocalization._SL("chat-payment-status-confirmed");
                status_icon = "fa-check-circle";
            }

            Utils.sendUiCommand(webView, "updateTransactionStatus", txid, status, status_icon);
        }

        public void updateRequestFundsStatus(byte[] msg_id, string txid, string status)
        {
            string status_icon = "fa-clock";
            bool enableView = true;
            if(status == SpixiLocalization._SL("chat-payment-status-declined"))
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

            if(friend.bot)
            {
                Utils.sendUiCommand(webView, "setOnlineStatus", String.Format(SpixiLocalization._SL("chat-member-count"), friend.users.contacts.Count()));
            }
            else
            {
                if (friend.online)
                {
                    Utils.sendUiCommand(webView, "setOnlineStatus", SpixiLocalization._SL("chat-online"));
                }
                else
                {
                    Utils.sendUiCommand(webView, "setOnlineStatus", SpixiLocalization._SL("chat-offline"));
                }
            }

            // Show connectivity warning bar
            if (NetworkClientManager.getConnectedClients(true).Count() > 0)
            {
                if (!Config.enablePushNotifications && (friend.relayIP == null || StreamClientManager.isConnectedTo(friend.relayIP, true) == null))
                {
                    Utils.sendUiCommand(webView, "showWarning", SpixiLocalization._SL("global-connecting-s2"));
                }
                else
                {
                    Utils.sendUiCommand(webView, "showWarning", "");
                }
            }
            else
            {
                Utils.sendUiCommand(webView, "showWarning", SpixiLocalization._SL("global-connecting-dlt"));
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