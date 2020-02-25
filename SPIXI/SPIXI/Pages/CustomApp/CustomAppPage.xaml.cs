using IXICore;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Text;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomAppPage : SpixiContentPage
    {
        public byte[] sessionId = null; // App session ID

        private byte[] hostUserAddress = null; // address of the user that initiated the app
        private byte[][] userAddresses = null; // addresses of all users connected to/using the app

        private string node_ip = "";


        public CustomAppPage(byte[] host_user_address, byte[][] user_addresses, string app_entry_point)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            sessionId = Guid.NewGuid().ToByteArray();

            hostUserAddress = host_user_address;
            userAddresses = user_addresses;

            node_ip = FriendList.getRelayHostname(host_user_address);

            // Load the app entry point
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/{1}", DependencyService.Get<IBaseUrl>().Get(), app_entry_point);
            webView.Source = source;
        }



        public override void recalculateLayout()
        {
            ForceLayout();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            FriendList.removeAppPage(sessionId);
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);
            e.Cancel = true;

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
                FriendList.addAppPage(this);
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
                FriendList.removeAppPage(sessionId);
            }
            else if (current_url.StartsWith("ixian:data", StringComparison.Ordinal))
            {
                onNetworkData(current_url.Substring(10));
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
            // Execute timer-related functionality immediately
            updateScreen();
        }

        // Executed every second
        public override void updateScreen()
        {

        }

        private void onNetworkData(string data)
        {
            foreach(byte[] address in userAddresses)
            {
                Friend f = FriendList.getFriend(address);
                if(f != null)
                {
                    SpixiMessage spixi_msg = new SpixiMessage();
                    spixi_msg.type = SpixiMessageCode.appData;
                    spixi_msg.data = (new SpixiAppData(sessionId, UTF8Encoding.UTF8.GetBytes(data))).getBytes();

                    StreamMessage msg = new StreamMessage();
                    msg.type = StreamMessageCode.data;
                    msg.recipient = f.walletAddress;
                    msg.sender = Node.walletStorage.getPrimaryAddress();
                    msg.transaction = new byte[1];
                    msg.sigdata = new byte[1];
                    msg.data = spixi_msg.getBytes();

                    StreamProcessor.sendMessage(f, msg, false, false);
                }else
                {
                    Logging.error("Friend {0} does not exist in the friend list.", Base58Check.Base58CheckEncoding.EncodePlain(address));
                }
            }
        }

        public void networkDataReceive(byte[] sender_address, byte[] data)
        {
            Utils.sendUiCommand(webView, "networkData", UTF8Encoding.UTF8.GetString(data));
        }
    }
}