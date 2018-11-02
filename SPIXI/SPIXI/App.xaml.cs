using DLT.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace SPIXI
{
	public partial class App : Application
	{
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
                Config.device_id = uid;
            }
            else
            {
                // Generate and save the device ID
                Application.Current.Properties["uid"] = Config.device_id;
            }

            // Start the IXIAN DLT
            Node.start();

            // Attempt to load a pre-existing wallet
            bool wallet_found = Node.loadWallet();

            if (!wallet_found)
            {
                // Wallet not found, go to initial launch page
                MainPage = new NavigationPage(new SPIXI.LaunchPage());
            }
            else
            {
                // Wallet found, go to main page //MainPage(); //
                MainPage = new NavigationPage(new SPIXI.HomePage());
                //MainPage = new NavigationPage(new SPIXI.LockPage());
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
