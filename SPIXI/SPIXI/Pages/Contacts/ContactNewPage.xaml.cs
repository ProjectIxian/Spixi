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
using SPIXI.Network;

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


            NetworkClientManager.requestPresenceList();
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
            webView.Eval(string.Format("setAddress(\"{0}\")", wallet_to_add));
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = e.Url;

            if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync();
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                DisplayAlert("SPIXI Account", "Please type a wallet address.", "OK");
            }
            else if (current_url.Contains("ixian:request:"))
            {
                string[] split = current_url.Split(new string[] { "ixian:request:" }, StringSplitOptions.None);
                string wal = split[1];
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

                    Navigation.PopAsync();

                    //DisplayAlert("New contact", result.Text, "OK");
                    
                    if (result.Text.Contains(":ixi"))
                    {
                        string[] split = result.Text.Split(new string[] { ":ixi" }, StringSplitOptions.None);
                        if (split.Count() < 1)
                            return;
                        string wal = split[0];
                        webView.Eval(string.Format("setAddress(\"{0}\")", wal));

                    }

                });
            };


            await Navigation.PushAsync(ScannerPage);

        }

        public void onRequest(string wal)
        {
            if(Address.validateChecksum(wal) == false)
            {
                DisplayAlert("Invalid checksum", "Please make sure you typed the address correctly.", "OK");
                return;
            }


            string pubkey = FriendList.findContactPubkey(wal);
            if(pubkey.Length < 1)
            {
                DisplayAlert("Contact does not exist", "No such SPIXI user.", "OK");
                return;
            }

            string relayip = FriendList.getRelayHostname(wal);

            //FriendList.addFriend(wal, pubkey, "Unknown");

            // Send the message to the S2 nodes
            string recipient_address = wal;
            byte[] encrypted_message = StreamProcessor.prepareSpixiMessage(SpixiMessageCode.requestAdd, "", pubkey);


            // Connect to the contact's S2 relay first
            NetworkClientManager.connectToStreamNode(relayip);

            Message message = new Message();
            message.recipientAddress = recipient_address;
            message.data = encrypted_message;

            // TODO: optimize this
            while(NetworkClientManager.isNodeConnected(relayip) == false)
            {
                
            }

            StreamProcessor.sendMessage(message, relayip);

            Navigation.PopAsync();
        }
    }
}