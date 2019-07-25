using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Notifications;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HomePage : SpixiContentPage
	{

        // Temporary optimizations for native->js data transfer
        private BigInteger lastContactsChange = 0;
        private BigInteger lastChatsChange = 0;
        private ulong lastTransactionChange = 9999;

        private string currentTab = "tab1";

        public HomePage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasBackButton(this, false);
            NavigationPage.SetHasNavigationBar(this, false);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Node.connectToNetwork();
            }).Start();

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/index.html",DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;


            //CrossLocalNotifications.Current.Show("title", "body",100, DateTime.Now.AddSeconds(10));
            handleBackground();

            //  Navigation.PushAsync(new LockPage(), Config.defaultXamarinAnimations);


            // Setup a timer to handle UI updates
            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                onUpdateUI();
                return true; // True = Repeat again, False = Stop the timer
            });
        }

        private void prepBackground()
        {
            var message = new StartMessage();
            MessagingCenter.Send(message, "StartMessage");
        }

        private void handleBackground()
        {
            MessagingCenter.Subscribe<TickedMessage>(this, "TickedMessage", message => {
                Device.BeginInvokeOnMainThread(() => {
                    // ticker.Text = message.Message;
                    Console.WriteLine("TICKTOCK: {0}", message.Message);
                });
            });

            MessagingCenter.Subscribe<CancelledMessage>(this, "CancelledMessage", message => {
                Device.BeginInvokeOnMainThread(() => {

                });
            });
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
                    Utils.sendUiCommand(webView, "quickScanJS");
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
                Device.OpenUri(new Uri(Config.aboutUrl));
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
                byte[] id_bytes = Base58Check.Base58CheckEncoding.DecodePlain(id);

                Friend friend = FriendList.getFriend(id_bytes);

                if (friend == null)
                {
                    e.Cancel = true;
                    return;
                }

                Navigation.PushAsync(new SingleChatPage(friend), Config.defaultXamarinAnimations);
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

        private void onLoaded()
        {
            lastContactsChange = 0;
            lastChatsChange = 0;
            lastTransactionChange = 0;

            Utils.sendUiCommand(webView, "selectTab", currentTab);

            Utils.sendUiCommand(webView, "loadAvatar", Node.localStorage.getOwnAvatarPath());

            updateScreen();
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
            // Check if there are any changes from last time first
            BigInteger chk = 0;
            foreach (Friend friend in FriendList.friends)
            {
                int umc = friend.getUnreadMessageCount();
                chk += new BigInteger(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(friend.nickname)));
                chk += umc;
                if (friend.online)
                {
                    chk += 100000;
                }
            }


            if (lastContactsChange == chk)
            {
                //  No changes detected, stop here
                return;
            }

            // Update the checksum
            lastContactsChange = chk;

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
            BigInteger chk = 0;
            int unread = 0;
            foreach (Friend friend in FriendList.friends)
            {
                int umc = friend.getUnreadMessageCount();
                if(umc > 0)
                {
                    unread += umc;
                }
                chk += new BigInteger(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(friend.nickname)));
                chk += umc;
                if (friend.online)
                {
                    chk += 100000;
                }
            }

            if(unread > 0)
            {
                Utils.sendUiCommand(webView, "setUnreadIndicator", unread.ToString());
            }else
            {
                Utils.sendUiCommand(webView, "setUnreadIndicator", "0");
            }

            if (lastChatsChange == chk)
            {
                //  No changes detected, stop here
                return;
            }

            // Update the checksum
            lastChatsChange = chk;


            Utils.sendUiCommand(webView, "clearChats");
            Utils.sendUiCommand(webView, "clearUnreadActivity");

            foreach (Friend friend in FriendList.friends)
            {
                if (!friend.approved || friend.getUnreadMessageCount() > 0)
                {
                    string str_online = "false";
                    if (friend.online)
                        str_online = "true";

                    // Generate the excerpt depending on message type
                    FriendMessage lastmsg = friend.messages.Last();
                    string excerpt = lastmsg.message;
                    if (lastmsg.type == FriendMessageType.requestFunds)
                        excerpt = "Payment Request";
                    else if (lastmsg.type == FriendMessageType.sentFunds)
                        excerpt = "Payment Sent";
                    else if (lastmsg.type == FriendMessageType.requestAdd)
                        excerpt = "Contact Request";

                    Utils.sendUiCommand(webView, "addChat",
                        Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, lastmsg.timestamp, "img/spixiavatar.png", str_online, excerpt, friend.getUnreadMessageCount().ToString());

                    Utils.sendUiCommand(webView, "addUnreadActivity",
                        Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, lastmsg.timestamp, "img/spixiavatar.png", str_online, excerpt);
                }

            }


            foreach (Friend friend in FriendList.friends)
            {
                if (friend.getMessageCount() > 0 && friend.getUnreadMessageCount() < 1)
                {
                    string str_online = "false";
                    if (friend.online)
                        str_online = "true";

                    // Generate the excerpt depending on message type
                    FriendMessage lastmsg = friend.messages.Last();
                    string excerpt = lastmsg.message;
                    if (lastmsg.type == FriendMessageType.requestFunds)
                        excerpt = "Payment Request";
                    else if (lastmsg.type == FriendMessageType.sentFunds)
                        excerpt = "Payment Sent";
                    else if (lastmsg.type == FriendMessageType.requestAdd)
                        excerpt = "Contact Request";

                    Utils.sendUiCommand(webView, "addChat",
                        Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.nickname, lastmsg.timestamp, "img/spixiavatar.png", str_online, excerpt, "0");
                }
            }
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
                Utils.sendUiCommand(webView, "addPaymentActivity", utransaction.id, tx_type, time, utransaction.amount.ToString());
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


                Utils.sendUiCommand(webView, "addPaymentActivity", transaction.id, tx_type, time, transaction.amount.ToString());
            }
        }



        // Executed every second
        public override void updateScreen()
        {
            Logging.info("Updating Home");

            loadChats();
            loadContacts();
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

            Utils.sendUiCommand(webView, "setBalance", Node.balance.ToString(), Node.localStorage.nickname);

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