using IXICore;
using IXICore.Meta;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;

namespace SPIXI.Storage
{
    // Used for storing and retrieving local data for SPIXI
    class LocalStorage
    {
        public string documentsPath = "";

        public string nickname = "";


        public LocalStorage()
        {
            // Retrieve the app-specific and platform-specific documents path
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            // Read transactions
            readTransactionCacheFile();
        }

        // Returns the user's avatar path
        public string getOwnAvatarPath()
        {
            var avatarPath = Path.Combine(documentsPath, "avatar.jpg");

            // Check if the file exists
            if (File.Exists(avatarPath) == false)
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
            string account_filename = Path.Combine(documentsPath, "account.ixi");

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
                Logging.log(LogSeverity.error, String.Format("Cannot open wallet file. {0}", e.Message));
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
                    int wal_length = reader.ReadInt32();
                    byte[] cwallet = reader.ReadBytes(wal_length);
                    int pkey_length = reader.ReadInt32();
                    byte[] cpubkey = reader.ReadBytes(pkey_length);

                    string cnick = reader.ReadString();
                    // Read chat history, todo
                    int num_messages = reader.ReadInt32();

                    int aes_len = reader.ReadInt32();
                    byte[] aes = reader.ReadBytes(aes_len);

                    int cc_len = reader.ReadInt32();
                    byte[] chacha = reader.ReadBytes(cc_len);

                    long key_generated_time = reader.ReadInt64();

                    FriendList.addFriend(cwallet, cpubkey, cnick, aes, chacha, key_generated_time);
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
            string account_filename = Path.Combine(documentsPath, "account.ixi");

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

                foreach(Friend friend in FriendList.friends)
                {
                    writer.Write(friend.walletAddress.Length);
                    writer.Write(friend.walletAddress);
                    if (friend.publicKey != null)
                    {
                        writer.Write(friend.publicKey.Length);
                        writer.Write(friend.publicKey);
                    }else
                    {
                        writer.Write(0);
                    }
                    writer.Write(friend.nickname);

                    // Chat history, todo.
                    int num_messages = 0;
                    writer.Write(num_messages);

                    // encryption keys
                    if (friend.aesKey != null)
                    {
                        writer.Write(friend.aesKey.Length);
                        writer.Write(friend.aesKey);
                    }else
                    {
                        writer.Write(0);
                    }

                    if (friend.chachaKey != null)
                    {
                        writer.Write(friend.chachaKey.Length);
                        writer.Write(friend.chachaKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write(friend.keyGeneratedTime);
                }

            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot write to account file. {0}", e.Message));
            }
            writer.Close();
            return true;
        }

        // Deletes the account file if it exists
        public bool deleteAccountFile()
        {
            string account_filename = Path.Combine(documentsPath, "account.ixi");

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
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.spx", wallet));

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
                    int s_type = reader.ReadInt32();
                    string s_message = reader.ReadString();
                    string s_timestamp = reader.ReadString();
                    bool s_from = reader.ReadBoolean();
                    bool s_read = reader.ReadBoolean();

                    FriendMessage message = new FriendMessage(s_message, s_timestamp, s_from, (FriendMessageType)s_type);
                    message.read = s_read;
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
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.spx", wallet));

            BinaryWriter writer;
            try
            {
                // Create the chats directory if it doesn't exist
                Directory.CreateDirectory(Path.Combine(documentsPath, "Chats"));
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
                    /*if (message.type != FriendMessageType.requestAdd)
                    {*/
                        int s_type = (int)message.type;
                        writer.Write(s_type);
                        writer.Write(message.message);
                        writer.Write(message.timestamp);
                        writer.Write(message.from);
                        writer.Write(message.read);
                    //}
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
            string messages_filename = Path.Combine(documentsPath, String.Format("Chats/{0}.spx", wallet));

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
            string messages_filename = Path.Combine(documentsPath, String.Format("offline.spx"));

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
            string messages_filename = Path.Combine(documentsPath, String.Format("offline.spx"));

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

            return true;
        }


        // Reads the message archive for a given wallet
        public bool readTransactionCacheFile()
        {
            string tx_filename = Path.Combine(documentsPath, String.Format("txcache.spx"));

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
            string tx_filename = Path.Combine(documentsPath, String.Format("txcache.spx"));

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

            return true;
        }

    }
}
