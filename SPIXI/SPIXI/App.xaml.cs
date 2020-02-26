using IXICore;
using IXICore.Meta;
using SPIXI.Meta;
using System.IO;
using Xamarin.Forms;

namespace SPIXI
{
    public partial class App : Application
	{
        public static bool isInForeground { get; set; } = false;

        Node node = null;
		public App ()
		{
			InitializeComponent();

            // Prepare the personal folder
            if (!Directory.Exists(Config.spixiUserFolder))
            {
                Directory.CreateDirectory(Config.spixiUserFolder);
            }

            movePersonalFiles();

            // Start logging
            Logging.start(Config.spixiUserFolder);

            // Load or generate a device ID.
            if (Application.Current.Properties.ContainsKey("uid"))
            {
                string uid = Application.Current.Properties["uid"] as string;
                // TODO: sanitize the uid if necessary
                CoreConfig.device_id = uid;
            }
            else
            {
                // Generate and save the device ID
                Application.Current.Properties["uid"] = CoreConfig.device_id;
            }

            // Start the IXIAN DLT
            node = new Node();

            // Attempt to load a pre-existing wallet
            bool wallet_found = Node.checkForExistingWallet();

            if (!wallet_found)
            {
                // Wallet not found, go to initial launch page
                MainPage = new NavigationPage(new SPIXI.LaunchPage());
            }
            else
            {
                // Wallet found, see if it can be decrypted
                bool wallet_decrypted = Node.loadWallet();

                if (wallet_decrypted == false)
                {
                    MainPage = new NavigationPage(new SPIXI.LaunchRetryPage());
                }
                else
                {
                    // Wallet found, go to main page
                    MainPage = new NavigationPage(new SPIXI.HomePage());
                    //MainPage = new NavigationPage(new SPIXI.LockPage());
                    Node.start();
                }
            }

            
            NavigationPage.SetHasNavigationBar(MainPage, false);
        }

        private void movePersonalFiles()
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            if (File.Exists(Path.Combine(path, "spixi.wal")) && !File.Exists(Path.Combine(Config.spixiUserFolder, Config.walletFile)))
            {
                File.Move(Path.Combine(path, "spixi.wal"), Path.Combine(Config.spixiUserFolder, Config.walletFile));
            }
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
            isInForeground = true;
        }

		protected override void OnSleep ()
		{
            // Handle when your app sleeps
            isInForeground = false;
        }

		protected override void OnResume ()
		{
            // Handle when your app resumes
            isInForeground = true;
        }

	}
}
