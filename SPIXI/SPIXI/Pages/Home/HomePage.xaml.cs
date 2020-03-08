using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HomePage : SpixiContentPage
	{
        private static HomePage _singletonInstance;

        public static HomePage Instance(bool force_new = false)
        {
            if(force_new)
            {
                if (_singletonInstance != null)
                {
                    _singletonInstance.stop();
                    _singletonInstance = null;
                }
            }
            if (_singletonInstance == null)
            {
                _singletonInstance = new HomePage();
            }
            return _singletonInstance;
        }

        // Temporary optimizations for native->js data transfer
        private ulong lastTransactionChange = 9999;

        private string currentTab = "tab1";

        private bool running = false;

        private HomePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasBackButton(this, false);
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/index.html",DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;

            if (!running)
            {
                running = true;

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Node.connectToNetwork();
                }).Start();

                // Setup a timer to handle UI updates
                Device.StartTimer(TimeSpan.FromSeconds(2), () =>
                {
                    Logging.info("HomePage.onUpdate");
                    onUpdateUI();
                    return running; // True = Repeat again, False = Stop the timer
                });
            }
        }

        public void stop()
        {
            running = false;
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoaded();
            }
            else if (current_url.Equals("ixian:wallet", StringComparison.Ordinal))
            {
                // Deprecated
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
            else if (current_url.Equals("ixian:newchat", StringComparison.Ordinal))
            {
                newChat();
            }
            else if (current_url.Equals("ixian:test", StringComparison.Ordinal))
            {
                displaySpixiAlert("Test", current_url, "Ok");
            }
            else if (current_url.Equals("ixian:newcontact", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new ContactNewPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:sendixi", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletSendPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:receiveixi", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new WalletReceivePage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:avatar", StringComparison.Ordinal))
            {
                //onChangeAvatarAsync(sender, e);
            }
            else if (current_url.Equals("ixian:settings", StringComparison.Ordinal))
            {
                onSettings(sender, e);
            }
            else if (current_url.Equals("ixian:address", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new MyAddressPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:lock", StringComparison.Ordinal))
            {
                //   prepBackground();
                Navigation.PushAsync(new SetLockPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:activity", StringComparison.Ordinal))
            {
                // TODO show wallet activity screen
            }
            else if (current_url.Equals("ixian:about", StringComparison.Ordinal))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Device.OpenUri(new Uri(Config.aboutUrl));
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else if (current_url.Equals("ixian:backup", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new BackupPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:encpass", StringComparison.Ordinal))
            {
                Navigation.PushAsync(new EncryptionPassword(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Contains("ixian:chat:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:chat:" }, StringSplitOptions.None);
                string id = split[1];
                onChat(id, e);
            }
            else if (current_url.Contains("ixian:details:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:details:" }, StringSplitOptions.None);
                string id = split[1];
                // TODO: handle exceptions
                byte[] id_bytes = Base58Check.Base58CheckEncoding.DecodePlain(id);

                Friend friend = FriendList.getFriend(id_bytes);

                if (friend == null)
                {
                    e.Cancel = true;
                    return;
                }

                Navigation.PushAsync(new ContactDetails(friend), Config.defaultXamarinAnimations);
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
            else if (current_url.Contains("ixian:tab:"))
            {
                currentTab = current_url.Split(new string[] { "ixian:tab:" }, StringSplitOptions.None)[1];
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

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
            // Check for add contact
            string[] split = result.Split(new string[] { ":send" }, StringSplitOptions.None);
            if (split.Count() > 1)
            {
                byte[] wallet_to_send = Base58Check.Base58CheckEncoding.DecodePlain(split[0]);
                Navigation.PushAsync(new WalletSendPage(wallet_to_send), Config.defaultXamarinAnimations);
                return;
            }

            string id_to_add = split[0];
            Navigation.PushAsync(new ContactNewPage(id_to_add), Config.defaultXamarinAnimations);
            return;
        }

        // Show the recipientpage
        public void newChat()
        {
            var recipientPage = new WalletRecipientPage();
            recipientPage.pickSucceeded += HandlePickSucceeded;
            Navigation.PushModalAsync(recipientPage);
        }

        private void HandlePickSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            //MainPage = new MainPage();
            string id = e.Value;


            byte[] id_bytes = Base58Check.Base58CheckEncoding.DecodePlain(id);

            Friend friend = FriendList.getFriend(id_bytes);

            if (friend == null)
            {
                return;
            }

            Navigation.PushAsync(new SingleChatPage(friend), Config.defaultXamarinAnimations);
            Navigation.PopModalAsync();


        }

        // Workaround for Android - sometimes the order of the screens isn't correct
        private void setAsRoot()
        {
            try
            {
                foreach (var page in Navigation.NavigationStack.ToList())
                {
                    if (page == this)
                    {
                        continue;
                    }
                    Navigation.RemovePage(page);
                }
            }
            catch(Exception e)
            {
                Logging.error("Exception occured while setting HomePage as root: {0}", e);
            }
        }

        private void onLoaded()
        {
            setAsRoot();

            Node.shouldRefreshContacts = true;
            lastTransactionChange = 0;

            Utils.sendUiCommand(webView, "selectTab", currentTab);

            Utils.sendUiCommand(webView, "loadAvatar", Node.localStorage.getOwnAvatarPath());

            Utils.sendUiCommand(webView, "setVersion", Config.version + " BETA (" + Node.startCounter + ")");

            try
            {
                updateScreen();
            }catch(Exception ex)
            {
                Logging.error("Exception occured in updateScreen call from onLoaded: {0}", ex);
            }

            if (App.startingScreen != "")
            {
                HomePage.Instance().onChat(App.startingScreen, null);
                App.startingScreen = "";
            }
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            //webView.Eval(string.Format("setBalance(\"{0}\")", "0.0000"));
            //webView.Eval(string.Format("setAddress(\"{0}\")", Node.walletStorage.address));
        }


        public void onSend(object sender, EventArgs e)
        {
            Navigation.PushAsync(new WalletSendPage(), Config.defaultXamarinAnimations);
        }

        public void onReceive(object sender, EventArgs e)
        {
            Navigation.PushAsync(new WalletReceivePage(), Config.defaultXamarinAnimations);
        }

        public void onSettings(object sender, EventArgs e)
        {
            Navigation.PushAsync(new SettingsPage(), Config.defaultXamarinAnimations);
        }

        public void onChat(string friend_address, WebNavigatingEventArgs e)
        {
            byte[] id_bytes = Base58Check.Base58CheckEncoding.DecodePlain(friend_address);

            Friend friend = FriendList.getFriend(id_bytes);

            if (friend == null)
            {
                if (e != null)
                {
                    e.Cancel = true;
                }
                return;
            }
            bool animated = Config.defaultXamarinAnimations;
            if(e == null)
            {
                animated = false;
            }
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopToRootAsync(false);
                }
                await Navigation.PushAsync(new SingleChatPage(friend), animated);
            });
        }

        public async Task onChangeAvatarAsync(object sender, EventArgs e)
        {
            Stream stream = await DependencyService.Get<IPicturePicker>().GetImageStreamAsync();

            if (stream != null)
            {
                Image image = new Image
                {
                    Source = ImageSource.FromStream(() => stream),
                    BackgroundColor = Color.Gray
                };

                var filePath = Node.localStorage.getOwnAvatarPath();
                FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                stream.CopyTo(fs);
                Utils.sendUiCommand(webView, "loadAvatar", filePath);
                stream.Close();
                fs.Close();
            }

        }

        // Load the contact list
        // TODO: optimize this
        public void loadContacts()
        {
            if (!Node.shouldRefreshContacts)
            {
                //  No changes detected, stop here
                return;
            }

            // Clear everything
            Utils.sendUiCommand(webView, "clearContacts");

            // Add contacts one-by-one
            foreach(Friend friend in FriendList.friends)
            {
                string str_online = "false";
                if (friend.online)
                    str_online = "true";

                Utils.sendUiCommand(webView, "addContact", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, "img/spixiavatar.png", str_online, friend.getUnreadMessageCount().ToString());
            }
        }

        public void loadChats()
        {
            // Check if there are any changes from last time first
            int unread = 0;
            foreach (Friend friend in FriendList.friends)
            {
                int umc = friend.getUnreadMessageCount();
                if(umc > 0)
                {
                    unread += umc;
                }
            }

            if(unread > 0)
            {
                Utils.sendUiCommand(webView, "setUnreadIndicator", unread.ToString());
            }else
            {
                Utils.sendUiCommand(webView, "setUnreadIndicator", "0");
            }

            if (!Node.shouldRefreshContacts)
            {
                //  No changes detected, stop here
                return;
            }

            Utils.sendUiCommand(webView, "clearChats");
            Utils.sendUiCommand(webView, "clearUnreadActivity");

            // Prepare a list of message helpers, to facilitate sorting and communication with the UI
            List<FriendMessageHelper> helper_msgs = new List<FriendMessageHelper>();

            foreach (Friend friend in FriendList.friends)
            {
                if (friend.getMessageCount() > 0)
                {
                    string str_online = "false";
                    if (friend.online)
                        str_online = "true";

                    // Generate the excerpt depending on message type
                    FriendMessage lastmsg = friend.messages.Last();
                    string excerpt = lastmsg.message;
                    if (lastmsg.type == FriendMessageType.requestFunds)
                    {
                        if (lastmsg.localSender)
                        {
                            excerpt = "Payment Request Sent";
                        }
                        else
                        {
                            excerpt = "Payment Request Received";
                        }
                    }
                    else if (lastmsg.type == FriendMessageType.sentFunds)
                    {
                        if (lastmsg.localSender)
                        {
                            excerpt = "Payment Sent";
                        }
                        else
                        {
                            excerpt = "Payment Received";
                        }
                    }
                    else if (lastmsg.type == FriendMessageType.requestAdd)
                        excerpt = "Contact Request";
                    else if (lastmsg.type == FriendMessageType.fileHeader)
                        excerpt = "File";

                    FriendMessageHelper helper_msg = new FriendMessageHelper(Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, lastmsg.timestamp, "img/spixiavatar.png", str_online, excerpt, friend.getUnreadMessageCount());
                    helper_msgs.Add(helper_msg);
                }
            }

            // Sort the helper messages
            List<FriendMessageHelper> sorted_msgs = helper_msgs.OrderByDescending(x => x.timestamp).ToList();

            // Add the messages visually
            foreach(FriendMessageHelper helper_msg in sorted_msgs)
            {
                if(helper_msg.unreadCount > 0)
                {
                    Utils.sendUiCommand(webView, "addUnreadActivity", helper_msg.walletAddress, helper_msg.nickname, Clock.getRelativeTime(helper_msg.timestamp), helper_msg.avatar, helper_msg.onlineString, helper_msg.excerpt);

                }

                Utils.sendUiCommand(webView, "addChat", helper_msg.walletAddress, helper_msg.nickname, Clock.getRelativeTime(helper_msg.timestamp), helper_msg.avatar, helper_msg.onlineString, helper_msg.excerpt, helper_msg.unreadCount.ToString());
            }

            // Clear the lists so they will be collected by the GC
            helper_msgs = null;
            sorted_msgs = null;
        }

        public void loadTransactions()
        {
            // Check if there are any changes
            if(lastTransactionChange == TransactionCache.lastChange)
            {
                return;
            }
            lastTransactionChange = TransactionCache.lastChange;

            Utils.sendUiCommand(webView, "clearPaymentActivity");

            foreach (Transaction utransaction in TransactionCache.unconfirmedTransactions)
            {
                string tx_type = "Payment Received";
                if (Node.walletStorage.isMyAddress((new Address(utransaction.pubKey).address)))
                {
                    tx_type = "Payment Sent";
                }
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(utransaction.timeStamp));
                Utils.sendUiCommand(webView, "addPaymentActivity", utransaction.id, tx_type, time, utransaction.amount.ToString(), "false");
            }

            for (int i = TransactionCache.transactions.Count - 1; i >= 0; i--)
            {
                Transaction transaction = TransactionCache.transactions[i];
                string tx_type = "Payment Received";
                if (Node.walletStorage.isMyAddress((new Address(transaction.pubKey).address)))
                {
                    tx_type = "Payment Sent";
                }
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(transaction.timeStamp));


                Utils.sendUiCommand(webView, "addPaymentActivity", transaction.id, tx_type, time, transaction.amount.ToString(), "true");
            }
        }



        // Executed every second
        public override void updateScreen()
        {
            Logging.info("Updating Home");

            loadChats();
            loadContacts();

            Node.shouldRefreshContacts = false;

            loadTransactions();

            // Check the ixian dlt
            if (NetworkClientManager.getConnectedClients(true).Count() > 0)
            {
                Utils.sendUiCommand(webView, "showWarning", "0");
            }
            else
            {
                Utils.sendUiCommand(webView, "showWarning", "1");
            }

            Utils.sendUiCommand(webView, "setBalance", Node.balance.balance.ToString(), Node.localStorage.nickname);

            // Check if we should reload certain elements
            if(Node.changedSettings == true)
            {
                Utils.sendUiCommand(webView, "loadAvatar", Node.localStorage.getOwnAvatarPath());
            }

        }

        private void onUpdateUI(/*object source, ElapsedEventArgs e*/)
        {
            try
            {
                if (!webView.IsEnabled)
                {
                    return;
                }
                Page page = Navigation.NavigationStack[Navigation.NavigationStack.Count - 1];
                if (page != null && page is SpixiContentPage)
                {
                    ((SpixiContentPage)page).updateScreen();
                }
            }catch(Exception ex)
            {
                Logging.error("Exception occured in onUpdateUI: {0}", ex);
            }
        }
    }
}