using System;
using SPIXI.Interfaces;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;
using DLT;
using DLT.Network;
using DLT.Meta;

namespace SPIXI
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ContactNewPage : SpixiContentPage
	{
        private string wallet_to_add = "";

		public ContactNewPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/contact_new.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;

            // TODO optimize this         
            NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.syncPresenceList, new byte[1], null);
        }

        public ContactNewPage(string wal_id)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            wallet_to_add = wal_id;

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/contact_new.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            webView.Eval(string.Format("setAddress(\"{0}\")", wallet_to_add));
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI Account", "Please type a wallet address.", "OK");
            }
            else if (current_url.Contains("ixian:request:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:request:" }, StringSplitOptions.None);
                byte[] wal = Base58Check.Base58CheckEncoding.DecodePlain(split[1]);
                onRequest(wal);
            }
            else if (current_url.Equals("ixian:quickscan", StringComparison.Ordinal))
            {
                quickScan();
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
                ScannerPage.IsAnalyzing = false;
                Device.BeginInvokeOnMainThread(() => {

                    Navigation.PopAsync(Config.defaultXamarinAnimations);
                    
                    if (result.Text.Contains(":ixi"))
                    {
                        string[] split = result.Text.Split(new string[] { ":ixi" }, StringSplitOptions.None);
                        if (split.Count() < 1)
                            return;
                        string wal = split[0];
                        webView.Eval(string.Format("setAddress(\"{0}\")", wal));

                    }
                    else
                    {
                        string wal = result.Text;
                        // TODO: enter exact Ixian address length
                        if(wal.Length > 20 && wal.Length < 128)
                            webView.Eval(string.Format("setAddress(\"{0}\")", wal));
                    }

                });
            };


            await Navigation.PushAsync(ScannerPage, Config.defaultXamarinAnimations);

        }

        public void onRequest(byte[] wal)
        {
            if(Address.validateChecksum(wal) == false)
            {
                DisplayAlert("Invalid checksum", "Please make sure you typed the address correctly.", "OK");
                return;
            }


            byte[] pubkey = FriendList.findContactPubkey(wal);
            if(pubkey == null)
            {
                DisplayAlert("Contact does not exist", "Try again later.", "OK");
                NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.syncPresenceList, new byte[1], null);
                return;
            }

            string relayip = FriendList.getRelayHostname(wal);

            // TODOSPIXI
            //FriendList.addFriend(wal, pubkey, "Unknown");

            // Connect to the contact's S2 relay first
            //  StreamClientManager.connectToStreamNode(relayip);

            // Send the message to the S2 nodes
            byte[] recipient_address = wal;

            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestAdd, new byte[1]);


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = recipient_address;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];

            StreamProcessor.sendMessage(message, relayip);

            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }
    }
}