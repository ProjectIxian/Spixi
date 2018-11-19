using DLT;
using DLT.Meta;
using DLT.Network;
using SPIXI.Interfaces;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SingleChatPage : SpixiContentPage
    {
        private Friend friend = null;
        private static Timer chatLoopTimer;

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
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                friend.chat_page = null;

                Navigation.PopAsync();

            }
            else if (current_url.Equals("ixian:request", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletReceivePage(friend));
            }
            else if (current_url.Equals("ixian:details", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new ContactDetails(friend, true));
            }
            else if (current_url.Equals("ixian:send", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletSend2Page(friend.walletAddress, true));
            }
            else if (current_url.Equals("ixian:accept", StringComparison.Ordinal))
            {
                onAccept();
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

                Navigation.PushAsync(new WalletSentPage(transaction));
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
            loadMessages();
            webView.Eval(string.Format("setNickname(\"{0}\")", friend.nickname));

            //webView.Eval("setSubtitle(\"online\")");

            // Execute timer-related functionality immediately
            onTimer();

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                onTimer();
                return true; // True = Repeat again, False = Stop the timer
            });

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

            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.chat, Encoding.UTF8.GetBytes(str));


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.data;
            message.recipient = friend.walletAddress;
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            string relayip = friend.searchForRelay();
            StreamProcessor.sendMessage(message, relayip);

            // Finally, add the text bubble visually
            DateTime dt = DateTime.Now;
            FriendMessage msg = new FriendMessage(str, String.Format("{0:t}", dt), false);
            friend.messages.Add(msg);
            insertMessage(msg);

            // Finally, clear the input field
            webView.Eval("clearInput()");
        }

        public void onAccept()
        {
            friend.approved = true;

            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.acceptAdd, new byte[1]);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            string relayip = friend.searchForRelay();
            StreamProcessor.sendMessage(message, relayip);
        }

        public void onConfirmPaymentRequest(string amount)
        {
            // TODO: extract the date from the corresponding message
            DateTime dt = DateTime.Now;
            string date_text = String.Format("{0:t}", dt);
            Navigation.PushAsync(new WalletContactRequestPage(friend, amount, date_text));
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
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                    {
                        webView.Eval(string.Format("showContactRequest(true)"));
                    });
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
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    webView.Eval(string.Format("addPaymentRequest('{0}')", message.message));
                });
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
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    webView.Eval(string.Format("addPaymentSent('{0}','{1}')", transaction.amount.ToString(), message.message));
                });
                return;
            }

            string prefix = "addMe";
            string avatar = Node.localStorage.getOwnAvatarPath();
            if (message.from == true)
            {
                prefix = "addThem";
                avatar = "avatar.png";
            }
            // Call webview methods on the main UI thread only
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                webView.Eval(string.Format("{0}(\"{1}\",\"{2}\",\"{3}\")", prefix, avatar, message.message, message.timestamp));
            });
            message.read = true;

            // Write to chat history
            if(message.from == false)
                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);
        }

        // Executed every second
        private void onTimer()
        {
            if(friend.online)
            {
                webView.Eval("showIndicator(true)");
            }
            else
            {
                webView.Eval("showIndicator(false)");
            }
       
            // Show connectivity warning bar
            if (StreamClientManager.isConnectedTo(node_ip) == null)
            {
                if (connectedToNode == true)
                {
                    connectedToNode = false;
                    webView.Eval("showWarning('Not connected to S2 node')");
                }
            }
            else
            {
                if(connectedToNode == false)
                {
                    connectedToNode = true;
                    webView.Eval("showWarning('')");
                }
            }
            

            // Show the messages indicator
            int msgCount = FriendList.getUnreadMessageCount();
            if(msgCount != lastMessageCount)
            {
                lastMessageCount = msgCount;
                webView.Eval(string.Format("showUnread({0})", lastMessageCount));
            }
        }
    }
}