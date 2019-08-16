using IXICore;
using IXICore.Meta;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPIXI.Storage
{
    // Used for storing and retrieving local data for SPIXI
    class LocalStorage
    {
        public string nickname = "";

        // storage paths
        private string documentsPath = "";

        private string accountFileName = "account.ixi";
        private string txCacheFileName = "txcache.ixi";
        private string offlineFileName = "offline.ixi";

        // locks, for thread concurrency
        private object accountLock = new object();
        private object txCacheLock = new object();
        private object offlineLock = new object();


        public LocalStorage(string path)
        {
            // Retrieve the app-specific and platform-specific documents path
            documentsPath = path;

            // prepare tmp path
            if (!Directory.Exists(Path.Combine(documentsPath, "tmp")))
            {
                Directory.CreateDirectory(Path.Combine(documentsPath, "tmp"));
            }

            // prepare chats path
            if (!Directory.Exists(Path.Combine(documentsPath, "Chats")))
            {
                Directory.CreateDirectory(Path.Combine(documentsPath, "Chats"));
            }

            // Read transactions
            readTransactionCacheFile();
        }

        // Returns the user's avatar path
        public string getOwnAvatarPath(bool override_with_default = true)
        {
            var avatarPath = Path.Combine(documentsPath, "avatar.jpg");

            // Check if the file exists
            if (File.Exists(avatarPath) == false && override_with_default)
            {
                // Use the default avatar instead
                avatarPath = "img/spixiavatar.png";
            }

            return avatarPath;
        }

        // Delete the user's avatar
        public bool deleteOwnAvatar()
        {
            string avatarPath = Path.Combine(documentsPath, "avatar.jpg");
            if (File.Exists(avatarPath) == false)
            {
                return false;
            }
            File.Delete(avatarPath);
            return true;
        }

        // Read the account file from local storage
        public bool readAccountFile()
        {
            string account_filename = Path.Combine(documentsPath, accountFileName);

            if (File.Exists(account_filename) == false)
            {
                Logging.log(LogSeverity.error, "Cannot read account file.");

                // Generate a new wallet
                return false;
            }

            BinaryReader reader;
            try
            {
                reader = new BinaryReader(new FileStream(account_filename, FileMode.Open));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open account file. {0}", e.Message));
                return false;
            }

            try
            {
                // TODO: decrypt data and compare the address/pubkey
                System.Int32 version = reader.ReadInt32();
                int address_length = reader.ReadInt32();
                byte[] address = reader.ReadBytes(address_length);
                string nick = reader.ReadString();

                nickname = nick;

                FriendList.clear();
                int num_contacts = reader.ReadInt32();
                for(int i = 0; i < num_contacts; i++)
                {
                    int friend_len = reader.ReadInt32();

                    Friend friend = new Friend(reader.ReadBytes(friend_len));

                    // Read messages from chat history
                    friend.messages = readMessagesFile(friend.walletAddress);

                    FriendList.addFriend(friend);
                }

            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot read from account file. {0}", e.Message));
            }

            reader.Close();

            return true;
        }

        // Write the account file to local storage
        public bool writeAccountFile()
        {
            lock (accountLock)
            {
                string account_filename = Path.Combine(documentsPath, accountFileName);

                BinaryWriter writer;
                try
                {
                    writer = new BinaryWriter(new FileStream(account_filename, FileMode.Create));
                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot create account file. {0}", e.Message));
                    return false;
                }

                try
                {
                    // TODO: encrypt written data
                    System.Int32 version = 1; // Set the account file version
                    writer.Write(version);

                    // Write the address used for verification
                    writer.Write(IxianHandler.getWalletStorage().getPrimaryAddress().Length);
                    writer.Write(IxianHandler.getWalletStorage().getPrimaryAddress());

                    // Write account information
                    writer.Write(nickname);

                    int num_contacts = FriendList.friends.Count;
                    writer.Write(num_contacts);

                    foreach (Friend friend in FriendList.friends)
                    {
                        byte[] friend_bytes = friend.getBytes();

                        writer.Write(friend_bytes.Length);
                        writer.Write(friend_bytes);
                    }

                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot write to account file. {0}", e.Message));
                }
                writer.Close();
            }
            return true;
        }

        // Deletes the account file if it exists
        public bool deleteAccountFile()
        {
            string account_filename = Path.Combine(documentsPath, accountFileName);

            if (File.Exists(account_filename) == false)
            {
                return false;
            }

            File.Delete(account_filename);
            return true;
        }

        // Reads the message archive for a given wallet
        public List<FriendMessage> readMessagesFile(byte[] wallet_bytes)
        {
            string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);

            List<FriendMessage> messages = new List<FriendMessage>();
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.ixi", wallet));

            if (File.Exists(messages_filename) == false)
            {
                // Return an empty list of messages
                return messages;
            }

            BinaryReader reader;
            try
            {
                reader = new BinaryReader(new FileStream(messages_filename, FileMode.Open));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open chat file. {0}", e.Message));
                return messages;
            }

            try
            {
                // TODO: decrypt data and compare the address/pubkey
                System.Int32 version = reader.ReadInt32();
                string address = reader.ReadString();

                int num_messages = reader.ReadInt32();
                for (int i = 0; i < num_messages; i++)
                {
                    int id_len = reader.ReadInt32();
                    byte[] id = reader.ReadBytes(id_len);
                    int s_type = reader.ReadInt32();
                    string s_message = reader.ReadString();
                    long s_timestamp = reader.ReadInt64();
                    bool s_local_sender = reader.ReadBoolean();
                    bool s_read = reader.ReadBoolean();
                    bool s_confirmed = reader.ReadBoolean();

                    FriendMessage message = new FriendMessage(id, s_message, s_timestamp, s_local_sender, (FriendMessageType)s_type);
                    message.read = s_read;
                    message.confirmed = s_confirmed;
                    messages.Add(message);
                }

            }
            catch (IOException e)
            {
                Logging.error("Cannot read from chat file. {0}", e.Message);
                // TODO TODO notify the user or something like that
            }

            reader.Close();

            return messages;
        }

        // Writes the message archive for a given wallet
        public bool writeMessagesFile(byte[] wallet_bytes, List<FriendMessage> messages)
        {
            string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.ixi", wallet));

            BinaryWriter writer;
            try
            {
                // Prepare the file for writing
                writer = new BinaryWriter(new FileStream(messages_filename, FileMode.Create));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot create chat file. {0}", e.Message));
                return false;
            }

            try
            {
                // TODO: encrypt written data
                System.Int32 version = 1; // Set the messages file version
                writer.Write(version);
                // Write the address used for verification
                writer.Write(wallet);

                int message_num = messages.Count;
                writer.Write(message_num);

                foreach(FriendMessage message in messages)
                {
                    writer.Write(message.id.Length);
                    writer.Write(message.id);
                    writer.Write((int)message.type);
                    writer.Write(message.message);
                    writer.Write(message.timestamp);
                    writer.Write(message.localSender);
                    writer.Write(message.read);
                    writer.Write(message.confirmed);
                }

            }
            catch (IOException e)
            {
                Logging.error("Cannot write to chat file. {0}", e.Message);
            }
            writer.Close();

            return true;
        }

        // Deletes the message archive if it exists for a given wallet
        public bool deleteMessagesFile(byte[] wallet_bytes)
        {
            string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.ixi", wallet));

            if (File.Exists(messages_filename) == false)
            {
                return false;
            }

            File.Delete(messages_filename);
            return true;
        }

        // Reads the offline message archive
        public List<StreamMessage> readOfflineMessagesFile()
        {
            List<StreamMessage> messages = new List<StreamMessage>();
            string messages_filename = Path.Combine(documentsPath, offlineFileName);

            if (File.Exists(messages_filename) == false)
            {
                // Return an empty list of messages
                return messages;
            }

            BinaryReader reader;
            try
            {
                reader = new BinaryReader(new FileStream(messages_filename, FileMode.Open));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open file. {0}", e.Message));
                return messages;
            }

            try
            {
                System.Int32 version = reader.ReadInt32();

                int num_messages = reader.ReadInt32();
                for (int i = 0; i < num_messages; i++)
                {
                    int data_length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(data_length);

                    StreamMessage message = new StreamMessage(data);
                    messages.Add(message);
                }

            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot read from file. {0}", e.Message));
                return messages;
            }

            reader.Close();

            return messages;
        }

        // Writes the cached offline messages to a file
        public bool writeOfflineMessagesFile(List<StreamMessage> messages)
        {
            lock (offlineLock)
            {
                string messages_filename = Path.Combine(documentsPath, offlineFileName);

                BinaryWriter writer;
                try
                {
                    // Prepare the file for writing
                    writer = new BinaryWriter(new FileStream(messages_filename, FileMode.Create));
                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot create file. {0}", e.Message));
                    return false;
                }

                try
                {
                    // TODO: encrypt written data
                    System.Int32 version = 1; // Set the messages file version
                    writer.Write(version);

                    int message_num = messages.Count;
                    writer.Write(message_num);

                    foreach (StreamMessage message in messages)
                    {
                        byte[] data = message.getBytes();
                        int data_length = data.Length;
                        writer.Write(data_length);
                        writer.Write(data);
                    }

                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot write to file. {0}", e.Message));
                    return false;
                }
                writer.Close();
            }

            return true;
        }


        // Reads the message archive for a given wallet
        public bool readTransactionCacheFile()
        {
            string tx_filename = Path.Combine(documentsPath, txCacheFileName);

            if (File.Exists(tx_filename) == false)
            {
                // Return
                return false;
            }

            BinaryReader reader;
            try
            {
                reader = new BinaryReader(new FileStream(tx_filename, FileMode.Open));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open file. {0}", e.Message));
                return false;
            }

            TransactionCache.clearAllTransactions();

            try
            {
                System.Int32 version = reader.ReadInt32();

                // Read confirmed transactions first
                int num_tx = reader.ReadInt32();
                for (int i = 0; i < num_tx; i++)
                {
                    int data_length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(data_length);

                    Transaction transaction = new Transaction(data, true);
                    TransactionCache.addTransaction(transaction, false);
                }

                // Read unconfirmed transactions
                /*int num_utx = reader.ReadInt32();
                for (int i = 0; i < num_utx; i++)
                {
                    int data_length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(data_length);

                    Transaction transaction = new Transaction(data);
                    TransactionCache.addUnconfirmedTransaction(transaction, false);
                }*/

            }
            catch (Exception e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot read from file. {0}", e.Message));
                return false;
            }

            reader.Close();

            return true;
        }

        // Writes the cached offline messages to a file
        public bool writeTransactionCacheFile()
        {
            lock (txCacheLock)
            {
                string tx_filename = Path.Combine(documentsPath, txCacheFileName);

                BinaryWriter writer;
                try
                {
                    // Prepare the file for writing
                    writer = new BinaryWriter(new FileStream(tx_filename, FileMode.Create));
                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot create file. {0}", e.Message));
                    return false;
                }

                try
                {
                    // TODO: encrypt written data
                    System.Int32 version = 1; // Set the tx cache file version
                    writer.Write(version);

                    // Write confirmed transaction
                    lock (TransactionCache.transactions)
                    {
                        int tx_num = TransactionCache.transactions.Count;
                        writer.Write(tx_num);

                        foreach (Transaction transaction in TransactionCache.transactions)
                        {
                            byte[] data = transaction.getBytes(true);
                            int data_length = data.Length;
                            writer.Write(data_length);
                            writer.Write(data);
                        }
                    }

                    // Write unconfirmed transactions
                    lock (TransactionCache.unconfirmedTransactions)
                    {
                        int tx_num = TransactionCache.unconfirmedTransactions.Count;
                        writer.Write(tx_num);

                        foreach (Transaction transaction in TransactionCache.unconfirmedTransactions)
                        {
                            byte[] data = transaction.getBytes(true);
                            int data_length = data.Length;
                            writer.Write(data_length);
                            writer.Write(data);
                        }
                    }

                }
                catch (IOException e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot write to file. {0}", e.Message));
                    return false;
                }
                writer.Close();
            }

            return true;
        }

        public string getTmpPath()
        {
            return Path.Combine(documentsPath, "tmp");
        }

    }
}
