using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.CustomApps;
using SPIXI.Interfaces;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xamarin.Forms;

namespace SPIXI.Meta
{
    class Balance
    {
        public Address address = null;
        public IxiNumber balance = 0;
        public ulong blockHeight = 0;
        public byte[] blockChecksum = null;
        public bool verified = false;
        public long lastUpdate = 0;
    }

    class Node: IxianNode
    {
        // Used for all local data storage
        public static LocalStorage localStorage;

        // Used to force reloading of some homescreen elements
        public static bool changedSettings = false;

        public static Balance balance = new Balance();      // Stores the last known balance for this node

        public static int startCounter = 0;

        public static bool shouldRefreshContacts = true;

        public static bool refreshAppRequests = true;

        public static TransactionInclusion tiv = null;

        public static CustomAppManager customAppManager = null;

        public static bool generatedNewWallet = false;

        // Private data

        private static Thread mainLoopThread;

        private static ulong networkBlockHeight = 0;
        private static byte[] networkBlockChecksum = null;
        private static int networkBlockVersion = 0;

        public static Node Instance = null;

        private static bool running = false;

        public Node()
        {
#if DEBUG
            Logging.warn("Testing language files");
            Lang.SpixiLocalization.testLanguageFiles("en-us");
#endif

               
            Logging.info("Initing node constructor");

            Instance = this;

            CoreConfig.simultaneousConnectedNeighbors = 6;

            IxianHandler.init(Config.version, this, Config.networkType);

            PeerStorage.init(Config.spixiUserFolder);

            // Init TIV
            tiv = new TransactionInclusion();

            Logging.info("Initing local storage");

            // Prepare the local storage
            localStorage = new SPIXI.Storage.LocalStorage(Config.spixiUserFolder);

            customAppManager = new CustomAppManager(Config.spixiUserFolder);

            FriendList.init(Config.spixiUserFolder);

            Logging.info("Node init done");

            string backup_file_name = Path.Combine(Config.spixiUserFolder, "spixi.account.backup.ixi");
            if(File.Exists(backup_file_name))
            {
                File.Delete(backup_file_name);
            }
        }

        static public void preStart()
        {
            // Start local storage
            localStorage.start();

            FriendList.loadContacts();
        }

        static public void start()
        {
            if (running)
            {
                return;
            }
            Logging.info("Starting node");

            running = true;

            UpdateVerify.init(Config.checkVersionUrl, Config.checkVersionSeconds);
            UpdateVerify.start();

            ulong block_height = 0;
            byte[] block_checksum = null;

            string headers_path;
            if (IxianHandler.isTestNet)
            {
                headers_path = Path.Combine(Config.spixiUserFolder, "testnet-headers");
            }
            else
            {
                headers_path = Path.Combine(Config.spixiUserFolder, "headers");
                if (generatedNewWallet || !checkForExistingWallet())
                {
                    generatedNewWallet = false;
                    block_height = CoreConfig.bakedBlockHeight;
                    block_checksum = CoreConfig.bakedBlockChecksum;
                }
                else
                {
                    block_height = Config.bakedRecoveryBlockHeight;
                    block_checksum = Config.bakedRecoveryBlockChecksum;
                }
            }

            // Start TIV
            tiv.start(headers_path, block_height, block_checksum);

            // Generate presence list
            PresenceList.init(IxianHandler.publicIP, 0, 'C');

            // Start the network queue
            NetworkQueue.start();

            // Prepare the stream processor
            StreamProcessor.initialize(Config.spixiUserFolder);

            // Start the keepalive thread
            PresenceList.startKeepAlive();

            // Start the transfer manager
            TransferManager.start();

            customAppManager.start();

            startCounter++;

            if (mainLoopThread != null)
            {
                mainLoopThread.Abort();
                mainLoopThread = null;
            }

            mainLoopThread = new Thread(mainLoop);
            mainLoopThread.Name = "Main_Loop_Thread";
            mainLoopThread.Start();

            // Init push service
            string tag = IxianHandler.getWalletStorage().getPrimaryAddress().ToString();
            var push_service = DependencyService.Get<IPushService>();            
            push_service.setTag(tag);
            push_service.initialize();
            push_service.clearNotifications();

            Logging.info("Node started");
        }


        // Checks for existing wallet file. Can also be used to handle wallet/account upgrading in the future.
        // Returns true if found, otherwise false.
        static public bool checkForExistingWallet()
        {
            if (!File.Exists(Path.Combine(Config.spixiUserFolder, Config.walletFile)))
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


            WalletStorage walletStorage = new WalletStorage(Path.Combine(Config.spixiUserFolder, Config.walletFile));
            if(walletStorage.readWallet(password))
            {
                IxianHandler.addWallet(walletStorage);
                return true;
            }
            return false;
        }

        static public bool generateWallet(string pass)
        {
            if(IxianHandler.getWalletList().Count == 0)
            {
                WalletStorage ws = new WalletStorage(Path.Combine(Config.spixiUserFolder, Config.walletFile));
                if(ws.generateWallet(pass))
                {
                    return IxianHandler.addWallet(ws);
                }
            }
            return false;
        }
        

        static public void connectToNetwork()
        {
            // Start the network client manager
            NetworkClientManager.start(2);

            // Start the s2 client manager
            StreamClientManager.start();
        }

        // Handle timer routines
        static public void mainLoop()
        {
            byte[] primaryAddress = IxianHandler.getWalletStorage().getPrimaryAddress().addressNoChecksum;
            if (primaryAddress == null)
                return;

            byte[] getBalanceBytes;
            using (MemoryStream mw = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mw))
                {
                    writer.WriteIxiVarInt(primaryAddress.Length);
                    writer.Write(primaryAddress);
                }
                getBalanceBytes = mw.ToArray();
            }

            while (running)
            {
                try
                {
                    // Update the friendlist
                    FriendList.Update();

                    // Request initial wallet balance
                    if (balance.blockHeight == 0 || balance.lastUpdate + 300 < Clock.getTimestamp())
                    {
                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.getBalance2, getBalanceBytes, null);
                    }

                    if (Config.enablePushNotifications)
                        OfflinePushMessages.fetchPushMessages();

                    // Cleanup the presence list
                    // TODO: optimize this by using a different thread perhaps
                    PresenceList.performCleanup();
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured in mainLoop: " + e);
                }
                Thread.Sleep(2500);
            }
        }

        static public void stop()
        {
            if (!running)
            {
                Logging.stop();
                IxianHandler.status = NodeStatus.stopped;
                return;
            }

            Logging.info("Stopping node...");
            running = false;

            // Stop the stream processor
            StreamProcessor.uninitialize();

            localStorage.stop();

            customAppManager.stop();

            // Stop TIV
            tiv.stop();

            // Stop the transfer manager
            TransferManager.stop();

            // Stop the keepalive thread
            PresenceList.stopKeepAlive();

            // Stop the network queue
            NetworkQueue.stop();

            NetworkClientManager.stop();
            StreamClientManager.stop();

            UpdateVerify.stop();

            IxianHandler.status = NodeStatus.stopped;

            Logging.info("Node stopped");

            Logging.stop();
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

        public override void receivedTransactionInclusionVerificationResponse(byte[] txid, bool verified)
        {
            // TODO implement error
            // TODO implement blocknum
            Transaction tx = TransactionCache.getUnconfirmedTransaction(txid);
            if(tx == null)
            {
                return;
            }

            if (!verified)
            {
                tx.applied = 0;
            }

            TransactionCache.addTransaction(tx);
            Page p = App.Current.MainPage.Navigation.NavigationStack.Last();
            if (p.GetType() == typeof(SingleChatPage))
            {
                ((SingleChatPage)p).updateTransactionStatus(Transaction.getTxIdString(txid), verified);
            }
        }

        public override void receivedBlockHeader(Block block_header, bool verified)
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
            if (tiv.getLastBlockHeader() == null || tiv.getLastBlockHeader().version < Block.maxVersion)
            {
                return Block.maxVersion - 1;
            }
            return tiv.getLastBlockHeader().version;
        }

        public override bool addTransaction(Transaction tx, bool force_broadcast)
        {
            // TODO Send to peer if directly connectable
            CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.transactionData, tx.getBytes(), null);
            PendingTransactions.addPendingLocalTransaction(tx);
            return true;
        }

        public override Block getLastBlock()
        {
            // TODO handle this more elegantly
            Block bh = tiv.getLastBlockHeader();
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

        public override Wallet getWallet(Address id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return new Wallet(balance.address, balance.balance);
            }
            return new Wallet(id, 0);
        }

        public override IxiNumber getWalletBalance(Address id)
        {
            // TODO Properly implement this for multiple addresses
            if (balance.address != null && id.SequenceEqual(balance.address))
            {
                return balance.balance;
            }
            return 0;
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
                List<PendingTransaction> tmp_pending_transactions = new List<PendingTransaction>(PendingTransactions.pendingTransactions);
                int idx = 0;
                foreach (var entry in tmp_pending_transactions)
                {
                    Transaction t = entry.transaction;
                    long tx_time = entry.addedTimestamp;

                    if (t.applied != 0)
                    {
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    // if transaction expired, remove it from pending transactions
                    if (last_block_height > ConsensusConfig.getRedactedWindowSize() && t.blockHeight < last_block_height - ConsensusConfig.getRedactedWindowSize())
                    {
                        Console.WriteLine("Error sending the transaction {0}", t.getTxIdString());
                        PendingTransactions.pendingTransactions.RemoveAll(x => x.transaction.id.SequenceEqual(t.id));
                        continue;
                    }

                    if (cur_time - tx_time > 40) // if the transaction is pending for over 40 seconds, resend
                    {
                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.transactionData, t.getBytes(), null);
                        entry.addedTimestamp = cur_time;
                        entry.confirmedNodeList.Clear();
                    }

                    if (entry.confirmedNodeList.Count() > 3) // already received 3+ feedback
                    {
                        continue;
                    }

                    if (cur_time - tx_time > 20) // if the transaction is pending for over 20 seconds, send inquiry
                    {
                        CoreProtocolMessage.broadcastGetTransaction(t.id, 0);
                    }

                    idx++;
                }
            }
        }

        public void onLowMemory()
        {
            FriendList.onLowMemory();
        }

        public override Block getBlockHeader(ulong blockNum)
        {
            return BlockHeaderStorage.getBlockHeader(blockNum);
        }

        public override IxiNumber getMinSignerPowDifficulty(ulong blockNum)
        {
            // TODO TODO implement this properly
            return ConsensusConfig.minBlockSignerPowDifficulty;
        }
    }
}