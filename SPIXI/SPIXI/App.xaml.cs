using DLT.Meta;
using IXICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace SPIXI
{
	public partial class App : Application
	{
        Node node = null;
		public App ()
		{
			InitializeComponent();

            // Start logging
            Logging.start();

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

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

	}
}
