using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using Xamarin.Forms;

namespace SPIXI.Meta
{
    class Balance
    {
        public byte[] address = null;
        public IxiNumber balance = 0;
        public ulong blockHeight = 0;
        public byte[] blockChecksum = null;
        public bool verified = false;
        public long lastUpdate = 0;
    }

    class Node: IxianNode
    {
        // Use the SPIXI-specific wallet storage code
        public static WalletStorage walletStorage;

        // Used for all local data storage
        public static SPIXI.Storage.LocalStorage localStorage;

        // Used to force reloading of some homescreen elements
        public static bool changedSettings = false;

        public static Balance balance = new Balance();      // Stores the last known balance for this node

        public static int startCounter = 0;

        public static bool shouldRefreshContacts = true;

        public static TransactionInclusion tiv = null;
        
        // Private data

        // Node timer
        private static System.Timers.Timer mainLoopTimer;

        private static ulong networkBlockHeight = 0;
        private static byte[] networkBlockChecksum = null;
        private static int networkBlockVersion = 0;


        public Node()
        {
            CoreConfig.productVersion = Config.version;
            IxianHandler.setHandler(this);

            CoreConfig.isTestNet = Config.isTestNet;

            // Initialize the crypto manager
            CryptoManager.initLib();

            // Prepare the wallet
            walletStorage = new WalletStorage(Path.Combine(Config.spixiUserFolder, Config.walletFile));

            string peers_filename = "peers.ixi";
            if(CoreConfig.isTestNet)
            {
                peers_filename = "testnet-peers.ixi";
            }

            PeerStorage.init(Config.spixiUserFolder, peers_filename);
        }

        static public void start()
        {
            // Generate presence list
            PresenceList.init(IxianHandler.publicIP, 0, 'C');

            // Prepare the local storage
            localStorage = new SPIXI.Storage.LocalStorage(Config.spixiUserFolder);

            // Read the account file
            localStorage.readAccountFile();

            // Start the network queue
            NetworkQueue.start();

            // Prepare the stream processor
            StreamProcessor.initialize();

            // Start the keepalive thread
            PresenceList.startKeepAlive();

            // Start the transfer manager
            TransferManager.start();

            ulong block_height = 1;
            byte[] block_checksum = null;

            if(!walletStorage.walletExists())
            {
                block_height = Config.bakedBlockHeight;
                block_checksum = Config.bakedBlockChecksum;
            }

            string headers_path = "";
            if(Config.isTestNet)
            {
                headers_path = Path.Combine(Config.spixiUserFolder, "headers");
            }else
            {
                // Temporary hack for our beta testers, remove before release
                BlockHeaderStorage.path = Path.Combine(Config.spixiUserFolder, "headers"); ;
                BlockHeaderStorage.deleteCache();
                BlockHeaderStorage.stop();
                // End of hack

                headers_path = Path.Combine(Config.spixiUserFolder, "testnet-headers");
            }
            // Start TIV
            tiv = new TransactionInclusion(headers_path, block_height, block_checksum);

            startCounter++;

            // Setup a timer to handle routine updates
            mainLoopTimer = new System.Timers.Timer(2500);
            mainLoopTimer.Elapsed += new ElapsedEventHandler(onUpdate);
            mainLoopTimer.Start();
        }


        // Checks for existing wallet file. Can also be used to handle wallet/account upgrading in the future.
        // Returns true if found, otherwise false.
        static public bool checkForExistingWallet()
        {
            if (File.Exists(walletStorage.getFileName()) == false)
            {
                Logging.log(LogSeverity.error, "Cannot read wallet file.");
                return false;
            }

            return true;
        }

        static public bool loadWallet()
        {
            if (Application.Current.Properties.ContainsKey("walletpass") == false)
                return false;

            // TODO: decrypt the password
            string password = Application.Current.Properties["walletpass"].ToString();


            return walletStorage.readWallet(password);
        }

        static public bool generateWallet(string pass)
        {
            return walletStorage.generateWallet(pass);
        }
        

        static public void connectToNetwork()
        {
            // Start the network client manager
            NetworkClientManager.start();
            // TODOSPIXI
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

            if(Config.enablePushNotifications)
                OfflinePushMessages.fetchPushMessages();

            // Request initial wallet balance
            if (balance.blockHeight == 0 || balance.lastUpdate + 300 < Clock.getTimestamp())
            {
                using (MemoryStream mw = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(mw))
                    {
                        writer.Write(Node.walletStorage.getPrimaryAddress().Length);
                        writer.Write(Node.walletStorage.getPrimaryAddress());
                        NetworkClientManager.broadcastData(new char[] { 'M' }, ProtocolMessageCode.getBalance, mw.ToArray(), null);
                    }
                }
            }
        }

        static public void stop()
        {
            // Stop TIV
            tiv.stop();

            // Stop the transfer manager
            TransferManager.stop();

            // Stop the keepalive thread
            PresenceList.stopKeepAlive();

            // Stop the loop timer
            mainLoopTimer.Stop();

            // Stop the network queue
            NetworkQueue.stop();

            NetworkClientManager.stop();
            StreamClientManager.stop();

            // Stop the stream processor
            StreamProcessor.uninitialize();
        }

        public override bool isAcceptingConnections()
        {
            // TODO TODO TODO TODO implement this properly
            return false;
        }
        

        public override void shutdown()
        {
            stop();
        }


        static public void setNetworkBlock(ulong block_height, byte[] block_checksum, int block_version)
        {
            networkBlockHeight = block_height;
            networkBlockChecksum = block_checksum;
            networkBlockVersion = block_version;
        }

        public override void receivedTransactionInclusionVerificationResponse(string txid, bool verified)
        {
            // TODO implement error
            // TODO implement blocknum

            if (verified)
            {
                Transaction tx = TransactionCache.getUnconfirmedTransaction(txid);
                if (tx != null)
                {
                    TransactionCache.addTransaction(tx);
                }
            }
        }

        public override void receivedBlockHeader(BlockHeader block_header, bool verified)
        {
            if (balance.blockChecksum != null && balance.blockChecksum.SequenceEqual(block_header.blockChecksum))
            {
                balance.verified = true;
            }
            if (block_header.blockNum >= networkBlockHeight)
            {
                IxianHandler.status = NodeStatus.ready;
                setNetworkBlock(block_header.blockNum, block_header.blockChecksum, block_header.version);
            }
            processPendingTransactions();
        }

        public override ulong getLastBlockHeight()
        {
            if (tiv.getLastBlockHeader() == null)
            {
                return 0;
            }
            return tiv.getLastBlockHeader().blockNum;
        }

        public override ulong getHighestKnownNetworkBlockHeight()
        {
            return networkBlockHeight;
        }

        public override int getLastBlockVersion()
        {
            if (tiv.getLastBlockHeader() == null)
            {
                return BlockVer.v6;
            }
            if (tiv.getLastBlockHeader().version < BlockVer.v6)
            {
                return BlockVer.v6;
            }
            return tiv.getLastBlockHeader().version;
        }

        public override bool addTransaction(Transaction tx)
        {
            PendingTransactions.addPendingLocalTransaction(tx);
            return true;
        }

        public override Block getLastBlock()
        {
            // TODO handle this more elegantly
            BlockHeader bh = tiv.getLastBlockHeader();
            return new Block()
            {
                blockNum = bh.blockNum,
                blockChecksum = bh.blockChecksum,
                lastBlockChecksum = bh.lastBlockChecksum,
                lastSuperBlockChecksum = bh.lastSuperBlockChecksum,
                lastSuperBlockNum = bh.lastSuperBlockNum,
                difficulty = bh.difficulty,
                superBlockSegments = bh.superBlockSegments,
                timestamp = bh.timestamp,
                transactions = bh.transactions,
                version = bh.version,
                walletStateChecksum = bh.walletStateChecksum,
                signatureFreezeChecksum = bh.signatureFreezeChecksum,
                compactedSigs = true
            };
        }

        public override Wallet getWallet(byte[] id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return new Wallet(balance.address, balance.balance);
            }
            return new Wallet(id, 0);
        }

        public override IxiNumber getWalletBalance(byte[] id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return balance.balance;
            }
            return 0;
        }

        public override WalletStorage getWalletStorage()
        {
            return walletStorage;
        }

        public override void parseProtocolMessage(ProtocolMessageCode code, byte[] data, RemoteEndpoint endpoint)
        {
            ProtocolMessage.parseProtocolMessage(code, data, endpoint);
        }

        public static void processPendingTransactions()
        {
            // TODO TODO improve to include failed transactions
            ulong last_block_height = IxianHandler.getLastBlockHeight();
            lock (PendingTransactions.pendingTransactions)
            {
                long cur_time = Clock.getTimestamp();
                List<object[]> tmp_pending_transactions = new List<object[]>(PendingTransactions.pendingTransactions);
                int idx = 0;
                foreach (var entry in tmp_pending_transactions)
                {
                    Transaction t = (Transaction)entry[0];
                    long tx_time = (long)entry[1];
                    if ((int)entry[2] > 3) // already received 3+ feedback
                    {
                        continue;
                    }

                    if (t.applied != 0)
                    {
                        PendingTransactions.pendingTransactions.RemoveAll(x => ((Transaction)x[0]).id.SequenceEqual(t.id));
                        continue;
                    }

                    // if transaction expired, remove it from pending transactions
                    if (last_block_height > ConsensusConfig.getRedactedWindowSize() && t.blockHeight < last_block_height - ConsensusConfig.getRedactedWindowSize())
                    {
                        Console.WriteLine("Error processing the transaction {0}", Encoding.UTF8.GetBytes(t.id));
                        PendingTransactions.pendingTransactions.RemoveAll(x => ((Transaction)x[0]).id.SequenceEqual(t.id));
                        continue;
                    }

                    if (cur_time - tx_time > 40) // if the transaction is pending for over 40 seconds, resend
                    {
                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.newTransaction, t.getBytes(), null);
                        PendingTransactions.pendingTransactions[idx][1] = cur_time;
                    }
                    idx++;
                }
            }
        }
    }
}