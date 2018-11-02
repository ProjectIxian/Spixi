using DLT.Network;
using SPIXI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Timers;

namespace DLT.Meta
{
    class Node
    {
        public static WalletState walletState;

        // Use the SPIXI-specific wallet storage code
        public static SPIXI.Wallet.WalletStorage walletStorage;

        // Store the in-memory friendlist here
        public static SPIXI.FriendList friendList;

        // Used for all local data storage
        public static SPIXI.Storage.LocalStorage localStorage;

        // Used to force reloading of some homescreen elements
        public static bool changedSettings = false;

        // Node timer
        private static Timer mainLoopTimer;


        public static IxiNumber balance = 0;

        static public void start()
        {
            // Initialize the crypto manager
            CryptoManager.initLib();

            // Prepare the wallet
            walletStorage = new SPIXI.Wallet.WalletStorage(Config.walletFile);

            // Initialize the wallet state
            walletState = new WalletState();

            // Prepare the local storage
            localStorage = new SPIXI.Storage.LocalStorage();

            // Read the account file
            localStorage.readAccountFile();

            // Start the network queue
            NetworkQueue.start();

            // Prepare the stream processor
            StreamProcessor.initialize();


            // Setup a timer to handle routine updates
            mainLoopTimer = new Timer(2500);
            mainLoopTimer.Elapsed += new ElapsedEventHandler(onUpdate);
            mainLoopTimer.Start();
        }

        static public bool loadWallet()
        {
            return walletStorage.readWallet();
        }

        static public bool generateWallet()
        {
            return walletStorage.generateWallet();
        }
        

        static public void connectToNetwork()
        {
            // Start the network client manager
            NetworkClientManager.start();
        }

        // Handle timer routines
        static public void onUpdate(object source, ElapsedEventArgs e)
        {
            // Update the friendlist
            FriendList.Update();

            // Cleanup the presence list
            // TODO: optimize this by using a different thread perhaps
            PresenceList.performCleanup();

        }

        static public void stop()
        {
            // Stop the loop timer
            mainLoopTimer.Stop();

            // Stop the network queue
            NetworkQueue.stop();

            NetworkClientManager.stop();

            // Stop the stream processor
            StreamProcessor.uninitialize();
        }

    }
}