using DLT.Network;
using IXICore;
using SPIXI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
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
        private static System.Timers.Timer mainLoopTimer;


        public static int keepAliveVersion = 0;

        // Private data
        private static Thread keepAliveThread;
        private static bool autoKeepalive = false;


        public static ulong blockHeight = 0;      // Stores the last known block height 
        public static IxiNumber balance = 0;      // Stores the last known balance for this node

        public static string primaryS2Address = "";

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

            // Start the keepalive thread
            autoKeepalive = true;
            keepAliveThread = new Thread(keepAlive);
            keepAliveThread.Start();

            // Setup a timer to handle routine updates
            mainLoopTimer = new System.Timers.Timer(2500);
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

            // Start the s2 client manager
            StreamClientManager.start();
        }

        // Handle timer routines
        static public void onUpdate(object source, ElapsedEventArgs e)
        {
            // Update the friendlist
            FriendList.Update();

            // Cleanup the presence list
            // TODO: optimize this by using a different thread perhaps
            PresenceList.performCleanup();


            if (Node.walletStorage.getPrimaryAddress() == null)
                return;

            // Request wallet balance
            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mw))
                {
                    writer.Write(Node.walletStorage.getPrimaryAddress().Length);
                    writer.Write(Node.walletStorage.getPrimaryAddress());
                    NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.getBalance, mw.ToArray());
                }
            }

        }

        static public void stop()
        {
            // Stop the keepalive thread
            autoKeepalive = false;
            if (keepAliveThread != null)
            {
                keepAliveThread.Abort();
                keepAliveThread = null;
            }

            // Stop the loop timer
            mainLoopTimer.Stop();

            // Stop the network queue
            NetworkQueue.stop();

            NetworkClientManager.stop();
            StreamClientManager.stop();

            // Stop the stream processor
            StreamProcessor.uninitialize();
        }


        // Sends periodic keepalive network messages
        private static void keepAlive()
        {
            while (autoKeepalive)
            {
                // Wait x seconds before rechecking
                for (int i = 0; i < CoreConfig.keepAliveInterval; i++)
                {
                    if (autoKeepalive == false)
                    {
                        Thread.Yield();
                        return;
                    }
                    // Sleep for one second
                    Thread.Sleep(1000);
                }


                sendKeepAlive();

            }

            Thread.Yield();
        }

        // Sends a single keepalive message
        public static void sendKeepAlive()
        {
            if(walletStorage.getPrimaryPrivateKey() == null)
            {
                return;
            }
            try
            {
                // Prepare the keepalive message
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        writer.Write(keepAliveVersion);

                        byte[] wallet = walletStorage.getPrimaryAddress();
                        writer.Write(wallet.Length);
                        writer.Write(wallet);

                        writer.Write(Config.device_id);

                        // Add the unix timestamp
                        long timestamp = Core.getCurrentTimestamp();
                        writer.Write(timestamp);

                        string hostname = Node.getFullAddress();
                        writer.Write(hostname);

                        // Add a verifiable signature
                        byte[] private_key = walletStorage.getPrimaryPrivateKey();
                        byte[] signature = CryptoManager.lib.getSignature(Encoding.UTF8.GetBytes(CoreConfig.ixianChecksumLockString + "-" + Config.device_id + "-" + timestamp + "-" + hostname), private_key);
                        writer.Write(signature.Length);
                        writer.Write(signature);

                        PresenceList.curNodePresenceAddress.lastSeenTime = timestamp;
                        PresenceList.curNodePresenceAddress.signature = signature;
                    }


                    byte[] address = null;
                    // Update self presence
                    PresenceList.receiveKeepAlive(m.ToArray(), out address);

                    // Send this keepalive message to the primary S2 node only
                    // TODO
                    //ProtocolMessage.broadcastProtocolMessage(ProtocolMessageCode.keepAlivePresence, m.ToArray());
                    StreamClientManager.broadcastData(ProtocolMessageCode.keepAlivePresence, m.ToArray());
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("KA Exception: {0}", e.Message));               
            }
        }

        public static string getFullAddress()
        {
            return Config.publicServerIP + ":" + Config.serverPort;
        }

        public static ulong getLastBlockHeight()
        {
            return blockHeight;
        }

        public static int getLastBlockVersion()
        {
            return 3; // TODO
        }
    }
}