using IXICore;
using IXICore.Meta;
using IXICore.Network;
using Spixi;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Web;
//using ZXing.Net.Mobile.Forms;

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

        private bool fromSettings = false;


        // Interal cache object to store contact status items
        private struct contactStatusCacheItem
        {
            public Address address;
            public bool online;
            public int unread;
            public string excerpt;
            public long timestamp;
        }
        private static List<contactStatusCacheItem> contactStatusCache = new List<contactStatusCacheItem>();


        private HomePage ()
		{
            Node.preStart();

            InitializeComponent();
            NavigationPage.SetHasBackButton(this, false);
            NavigationPage.SetHasNavigationBar(this, false);
            this.Title = "SPIXI";
            string onboarding_complete = "false";
            if (Preferences.Default.ContainsKey("onboardingComplete"))
            {
                onboarding_complete = "true";
            }
            SpixiLocalization.addCustomString("OnboardingComplete", onboarding_complete);

            loadPage(webView, "index.html");

            if (!running)
            {
                running = true;

                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    try
                    {
                        Node.start();
                        Node.connectToNetwork();
                    }catch(Exception e)
                    {
                        Logging.error("Fatal error has occured: " + e);
                        displaySpixiAlert("Fatal exception", "Fatal exception has occured, please send the log files to the developers." + e.Message, "OK");
                    }
                }).Start();

                // Setup a timer to handle UI updates
                IDispatcherTimer timer = Dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromMilliseconds(2000);
                timer.Tick += (s, e) =>
                {
                    onUpdateUI();
                    if (!running)
                        timer.Stop();
                };
                timer.Start();
            }
        }

        public void stop()
        {
            running = false;
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if(onNavigatingGlobal(current_url))
            {
                e.Cancel = true;
                return;
            }

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
                quickScan(); 
                e.Cancel = true;
                return;
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
                Browser.Default.OpenAsync(new Uri(Config.aboutUrl));
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else if (current_url.Equals("ixian:guide", StringComparison.Ordinal))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Browser.Default.OpenAsync(new Uri(Config.guideUrl));
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

                Friend friend = FriendList.getFriend(new Address(id));

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
                byte[] b_txid = Transaction.txIdLegacyToV8(id);

                Transaction transaction = null;
                foreach (Transaction tx in TransactionCache.transactions)
                {
                    if (tx.id.SequenceEqual(b_txid))
                    {
                        transaction = tx;
                        break;
                    }
                }

                if (transaction == null)
                {
                    lock (TransactionCache.unconfirmedTransactions)
                    {
                        foreach (Transaction tx in TransactionCache.unconfirmedTransactions)
                        {
                            if (tx.id.SequenceEqual(b_txid))
                            {
                                transaction = tx;
                                break;
                            }
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
            else if (current_url.Equals("ixian:apps", StringComparison.Ordinal))
            {
                //   prepBackground();
                Navigation.PushAsync(new AppsPage(), Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:downloads", StringComparison.Ordinal))
            {
                //   prepBackground();
                Navigation.PushAsync(new DownloadsPage(), Config.defaultXamarinAnimations);
            }
            else if(current_url.StartsWith("ixian:viewLog"))
            {
                // TODO perhaps move this whole functionality to Logging class and delete spixi.log.zip on start if exists

                if (File.Exists(Path.Combine(Config.spixiUserFolder, "spixi.log.zip")))
                {
                    File.Delete(Path.Combine(Config.spixiUserFolder, "spixi.log.zip"));
                }

                if (File.Exists(Path.Combine(Config.spixiUserFolder, "ixian.log.tmp")))
                {
                    File.Delete(Path.Combine(Config.spixiUserFolder, "ixian.log.tmp"));
                }

                File.Copy(Path.Combine(Config.spixiUserFolder, "ixian.log"), Path.Combine(Config.spixiUserFolder, "ixian.log.tmp"));

                using (ZipArchive archive = ZipFile.Open(Path.Combine(Config.spixiUserFolder, "spixi.log.zip"), ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(Path.Combine(Config.spixiUserFolder, "ixian.log.tmp"), "ixian.log");
                    if(File.Exists(Path.Combine(Config.spixiUserFolder, "ixian.0.log")))
                    {
                        archive.CreateEntryFromFile(Path.Combine(Config.spixiUserFolder, "ixian.0.log"), "ixian.0.log");
                    }
                }

                if (File.Exists(Path.Combine(Config.spixiUserFolder, "ixian.log.tmp")))
                {
                    File.Delete(Path.Combine(Config.spixiUserFolder, "ixian.log.tmp"));
                }

                SFileOperations.share(Path.Combine(Config.spixiUserFolder, "spixi.log.zip"), "Share Spixi Log File");
            }else if(current_url.StartsWith("ixian:onboardingComplete"))
            {
                Preferences.Default.Set("onboardingComplete", true);

                SpixiLocalization.addCustomString("OnboardingComplete", "true");
                generatePage("index.html");
            }else if(current_url.StartsWith("ixian:joinBot"))
            {
                Friend friend = FriendList.addFriend(new Address("419jmKRKVFcsjmwpDF1XSZ7j1fez6KWaekpiawHvrpyZ8TPVmH1v6bhT2wFc1uddV"), null, "Spixi Group Chat", null, null, 0);
                if (friend != null)
                {
                    friend.save();

                    StreamProcessor.sendContactRequest(friend);
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


        public async void quickScan()
        {
            var scanPage = new ScanPage();
            scanPage.scanSucceeded += HandleScanSucceeded;
            await Navigation.PushModalAsync(scanPage);
        }
        private void HandleScanSucceeded(object sender, SPIXI.EventArgs<string> e)
        {
            processQRResult(e.Value);       
        }

        public void processQRResult(string result)
        {
            Navigation.PopModalAsync();

            // Check for add contact
            string[] split = result.Split(new string[] { ":send" }, StringSplitOptions.None);
            if (split.Count() > 1)
            {

                try
                {
                    Address wallet_to_send = new Address(split[0]);
                    Navigation.PushAsync(new WalletSendPage(wallet_to_send), Config.defaultXamarinAnimations);
                }catch(Exception)
                {

                }
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


            Address id_bytes = new Address(id);

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
            Node.refreshAppRequests = true;
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
                onChat(App.startingScreen, null);
                App.startingScreen = "";
            }
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            //webView.Eval(string.Format("setBalance(\"{0}\")", "0.0000"));
            //webView.Eval(string.Format("setAddress(\"{0}\")", IxianHandler.getWalletStorage().address));
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
            fromSettings = true;
            Navigation.PushAsync(new SettingsPage(), Config.defaultXamarinAnimations);
        }

        public void onChat(string friend_address, WebNavigatingEventArgs e)
        {
            Address id_bytes = new Address(friend_address);

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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopToRootAsync(false);
                }

                // TODO
                /*if (IXICore.Platform.onWindows())
                {
                    Window secondWindow = new Window(new SingleChatPage(friend));
                    secondWindow.Title = friend.nickname;
                    Application.Current.OpenWindow(secondWindow);
                }
                else*/
                {
                    await Navigation.PushAsync(new SingleChatPage(friend), animated);
                }
            });
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

                string avatar = Node.localStorage.getAvatarPath(friend.walletAddress.ToString());
                if (avatar == null)
                {
                    avatar = "img/spixiavatar.png";
                }

                Utils.sendUiCommand(webView, "addContact", friend.walletAddress.ToString(), friend.nickname, avatar, str_online, friend.getUnreadMessageCount().ToString());
            }
        }

        public void loadChats()
        {
            List<Friend> friends;
            lock(FriendList.friends)
            {
                friends = new List<Friend>(FriendList.friends);
            }
            // Check if there are any changes from last time first
            int unread = 0;
            foreach (Friend friend in friends)
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

            foreach (Friend friend in friends)
            {
                if(friend.pendingDeletion)
                {
                    continue;
                }

                FriendMessage lastmsg = friend.metaData.lastMessage;
                if(lastmsg == null)
                {
                    if(friend.getMessages(0).Count > 0)
                    {
                        lastmsg = friend.getMessages(0).Last();
                    }
                }
                if (lastmsg != null)
                {
                    string str_online = "false";
                    if (friend.online)
                        str_online = "true";

                    // Generate the excerpt depending on message type
                    string excerpt = lastmsg.message;
                    if (lastmsg.type == FriendMessageType.requestFunds)
                    {
                        if (lastmsg.localSender)
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-payment-request-sent");
                        }
                        else
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-payment-request-received");
                        }
                    }
                    else if (lastmsg.type == FriendMessageType.sentFunds)
                    {
                        if (lastmsg.localSender)
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-payment-sent");
                        }
                        else
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-payment-received");
                        }
                    }
                    else if (lastmsg.type == FriendMessageType.requestAdd)
                    {
                        if (friend.approved)
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-contact-accepted");
                        }
                        else
                        {
                            excerpt = SpixiLocalization._SL("index-excerpt-contact-request");
                        }
                    }
                    else if (lastmsg.type == FriendMessageType.fileHeader)
                    {
                        excerpt = SpixiLocalization._SL("index-excerpt-file");
                    }
                    else if(lastmsg.type == FriendMessageType.voiceCall || lastmsg.type == FriendMessageType.voiceCallEnd)
                    {
                        excerpt = SpixiLocalization._SL("index-excerpt-voice-call");
                    }
                    
                    if (lastmsg.localSender)
                    {
                        excerpt = SpixiLocalization._SL("index-excerpt-self") + " " + excerpt;
                    }

                    string avatar = Node.localStorage.getAvatarPath(friend.walletAddress.ToString());
                    if(avatar == null)
                    {
                        avatar = "img/spixiavatar.png";
                    }

                    FriendMessageHelper helper_msg = new FriendMessageHelper(friend.walletAddress.ToString(), friend.nickname, lastmsg.timestamp, avatar, str_online, excerpt, friend.getUnreadMessageCount());
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
                    Utils.sendUiCommand(webView, "addUnreadActivity", helper_msg.walletAddress, helper_msg.nickname, helper_msg.timestamp.ToString(), helper_msg.avatar, helper_msg.onlineString, helper_msg.excerpt);

                }

                Utils.sendUiCommand(webView, "addChat", helper_msg.walletAddress, helper_msg.nickname, helper_msg.timestamp.ToString(), helper_msg.avatar, helper_msg.onlineString, helper_msg.excerpt, helper_msg.unreadCount.ToString());
            }

            // Clear the lists so they will be collected by the GC
            helper_msgs = null;
            sorted_msgs = null;
        }

        public static IxiNumber calculateReceivedAmount(Transaction tx)
        {
            IxiNumber amount = 0;
            foreach (var entry in tx.toList)
            {
                if (IxianHandler.getWalletStorage().isMyAddress(entry.Key))
                {
                    amount += entry.Value.amount;
                }
            }
            return amount;
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
            lock (TransactionCache.unconfirmedTransactions)
            {
                for (int i = TransactionCache.unconfirmedTransactions.Count - 1; i >= 0; i--)
                {
                    Transaction utransaction = TransactionCache.unconfirmedTransactions[i];
                    string tx_type = SpixiLocalization._SL("index-excerpt-payment-received");
                    IxiNumber amount = utransaction.amount;
                    if (IxianHandler.getWalletStorage().isMyAddress(utransaction.pubKey))
                    {
                        tx_type = SpixiLocalization._SL("index-excerpt-payment-sent");
                    }
                    else
                    {
                        amount = calculateReceivedAmount(utransaction);
                    }
                    string time = Utils.UnixTimeStampToString(Convert.ToDouble(utransaction.timeStamp));
                    Utils.sendUiCommand(webView, "addPaymentActivity", utransaction.getTxIdString(), tx_type, time, amount.ToString(), "false");
                }
            }

            int max_tx_count = 0;
            if(TransactionCache.transactions.Count > 50)
            {
                max_tx_count = TransactionCache.transactions.Count - 50;
            }

            for (int i = TransactionCache.transactions.Count - 1; i >= max_tx_count; i--)
            {
                Transaction transaction = TransactionCache.transactions[i];
                string tx_type = SpixiLocalization._SL("index-excerpt-payment-received");
                IxiNumber amount = transaction.amount;
                if (IxianHandler.getWalletStorage().isMyAddress(transaction.pubKey))
                {
                    tx_type = SpixiLocalization._SL("index-excerpt-payment-sent");
                }
                else
                {
                    amount = calculateReceivedAmount(transaction);
                }
                string time = Utils.UnixTimeStampToString(Convert.ToDouble(transaction.timeStamp));

                string confirmed = "true";
                if(transaction.applied == 0)
                {
                    confirmed = "error";
                }

                Utils.sendUiCommand(webView, "addPaymentActivity", transaction.getTxIdString(), tx_type, time, amount.ToString(), confirmed);
            }
        }



        // Executed every second
        public override void updateScreen()
        {
            base.updateScreen();

            loadChats();
            loadContacts();
            Node.shouldRefreshContacts = false;

            updateContactStatus();
            loadTransactions();

            try
            {
                string new_version = checkForUpdate();
                if (!new_version.StartsWith("(") && Version.Parse(new_version.Substring(new_version.IndexOf('-') + 1)).CompareTo(Version.Parse(Config.version.Substring(Config.version.IndexOf('-') + 1))) > 0)
                {
                    Utils.sendUiCommand(webView, "showWarning", String.Format(SpixiLocalization._SL("global-update-available"), new_version));
                }
                else
                {
                    // Check the ixian dlt
                    if (NetworkClientManager.getConnectedClients(true).Count() > 0)
                    {
                        Utils.sendUiCommand(webView, "showWarning", "");
                    }
                    else
                    {
                        Utils.sendUiCommand(webView, "showWarning", SpixiLocalization._SL("global-connecting-dlt"));
                    }

                }
            }catch (Exception e)
            {
                Logging.error("Exception occurred in HomePage.UpdateScreen: " + e);
            }

            Utils.sendUiCommand(webView, "setBalance", Node.balance.balance.ToString(), Node.localStorage.nickname);

            // Check if we should reload certain elements
            if(Node.changedSettings == true)
            {
                Utils.sendUiCommand(webView, "loadAvatar", Node.localStorage.getOwnAvatarPath());
                Node.changedSettings = false;
            }

            SPushService.clearNotifications();
        }

        private void onUpdateUI(/*object source, ElapsedEventArgs e*/)
        {
            try
            {
                if (!webView.IsEnabled || !App.isInForeground)
                {
                   // return;
                }
                Page page = Navigation.NavigationStack.Last();
                if (page != null && page is SpixiContentPage)
                {
                    ((SpixiContentPage)page).updateScreen();
                }
            }
            catch(Exception ex)
            {
                Logging.error("Exception occured in onUpdateUI: {0}", ex);
            }
        }

        protected override void OnAppearing()
        {
            if(fromSettings)
            {
                fromSettings = false;
                loadPage(webView, "index.html");
            }
            base.OnAppearing();
            lock (FriendList.friends)
            {
                var tmp_list = FriendList.friends.FindAll(x => x.chat_page != null);
                foreach (var friend in tmp_list)
                {
                    if(friend.chat_page != null)
                    {
                        friend.chat_page = null;
                    }
                }
            }
        }

        private string checkForUpdate()
        {
            if (!UpdateVerify.ready && !UpdateVerify.error) return "(checking)";
            if (UpdateVerify.ready)
            {
                if (UpdateVerify.error) return "(error)";
                return UpdateVerify.serverVersion;
            }
            return "(not checked)";
        }

        // Adds and filters a new contact status to the cache
        // Can be called from any thread
        public void setContactStatus(Address address, bool online, int unread, string excerpt, long timestamp)
        {
            // Cache and filter contact status changes to reduce cpu usage with many notifications
            lock (contactStatusCache)
            {
                bool _alreadyInCache = false;
                int i = 0;
                for(i = 0; i < contactStatusCache.Count; i++)
                {
                    contactStatusCacheItem cacheItem = contactStatusCache[i];
                    if (cacheItem.address.SequenceEqual(address))
                    {
                        // Update the cached status to the latest message
                        if(timestamp > cacheItem.timestamp)
                        {
                            cacheItem.timestamp = timestamp;
                            cacheItem.unread = unread;
                            cacheItem.online = online;
                            cacheItem.excerpt = excerpt;
                        }
                        // Already in cache, break to minimize processing
                        _alreadyInCache = true;
                        break;
                    }
                }

                // If not found in cache, add this message
                if(!_alreadyInCache)
                {
                    contactStatusCacheItem cacheItem = new contactStatusCacheItem();
                    cacheItem.address = address;
                    cacheItem.online = online;
                    cacheItem.unread = unread;
                    cacheItem.excerpt = excerpt;
                    cacheItem.timestamp = timestamp;
                    contactStatusCache.Add(cacheItem);
                }
            }

        }
        // Updates the status for all entries in the contact status cache
        // Called from a UI thread
        public void updateContactStatus()
        {
            lock (contactStatusCache)
            {
                // Go through each cache item and perform the status update
                foreach (contactStatusCacheItem cacheItem in contactStatusCache)
                {
                    Utils.sendUiCommand(webView, "setContactStatus", cacheItem.address.ToString(), 
                        cacheItem.online.ToString(), cacheItem.unread.ToString(), cacheItem.excerpt, cacheItem.timestamp.ToString());
                }
                // Clear the contact status cache
                contactStatusCache.Clear();
            }
        }
    }
}