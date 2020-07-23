using IXICore;
using IXICore.Meta;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;

namespace SPIXI.Storage
{  
    // Used for storing and retrieving local data for SPIXI
    class LocalStorage
    {
        public string nickname = "";

        // storage paths
        public string documentsPath { get; private set; }
        public string avatarsPath { get; private set; }
        public string tmpPath { get; private set; }

        private string accountFileName = "account.ixi";
        private string txCacheFileName = "txcache.ixi";

        // locks, for thread concurrency
        private object accountLock = new object();
        private object txCacheLock = new object();
        private object avatarLock = new object();
        private object messagesLock = new object();

        private int messagesPerFile = 1000;

        private bool started = false;

        public LocalStorage(string path, int messages_per_file = 1000)
        {
            // Retrieve the app-specific and platform-specific documents path
            documentsPath = path;
            avatarsPath = Path.Combine(path, "html", "Avatars");
            tmpPath = Path.Combine(path, "tmp");

            messagesPerFile = messages_per_file;

            // Prepare html path
            if (!Directory.Exists(Path.Combine(path, "html")))
            {
                Directory.CreateDirectory(Path.Combine(path, "html"));
            }

            // Prepare tmp path
            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }

            // Prepare Chats path
            if (!Directory.Exists(Path.Combine(documentsPath, "Chats")))
            {
                Directory.CreateDirectory(Path.Combine(documentsPath, "Chats"));
            }

            // Prepare Downloads path
            if (!Directory.Exists(Path.Combine(documentsPath, "Downloads")))
            {
                Directory.CreateDirectory(Path.Combine(documentsPath, "Downloads"));
            }

            // Prepare Avatars path
            if (!Directory.Exists(avatarsPath))
            {
                Directory.CreateDirectory(avatarsPath);
            }
        }

        public void start()
        {
            if(started)
            {
                return;
            }
            started = true;

            // Read transactions
            readTransactionCacheFile();

            // Read the account file
            readAccountFile();
        }

        // Returns the user's avatar path
        public string getOwnAvatarPath(bool override_with_default = true)
        {
            var avatarPath = Path.Combine(avatarsPath, "avatar.jpg");

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
            string avatarPath = Path.Combine(avatarsPath, "avatar.jpg");
            if (File.Exists(avatarPath) == false)
            {
                return false;
            }
            File.Delete(avatarPath);
            return true;
        }

        public byte[] getOwnAvatarBytes()
        {
            string path = getOwnAvatarPath(false);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            return null;
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
            catch (Exception e)
            {
                Logging.error("Cannot open account file: {0}", e.Message);
                return false;
            }

            int version = 0;

            try
            {
                // TODO: decrypt data and compare the address/pubkey
                version = reader.ReadInt32();
                int address_length = reader.ReadInt32();
                byte[] address = reader.ReadBytes(address_length);
                string nick = reader.ReadString();

                nickname = nick;

                FriendList.clear();
                int num_contacts = reader.ReadInt32();
                for(int i = 0; i < num_contacts; i++)
                {
                    int friend_len = reader.ReadInt32();
                    byte[] friend_bytes = reader.ReadBytes(friend_len);

                    Friend friend = null;
                    try
                    {
                        friend = new Friend(friend_bytes, version);
                    }catch(Exception e)
                    {
                        Logging.error("Error reading contact from accounts file: " + e);
                        continue;
                    }

                    string friend_path = Path.Combine(documentsPath, "Chats", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                    if(!Directory.Exists(friend_path))
                    {
                        Directory.CreateDirectory(friend_path);
                    }

                    if (friend.bot)
                    {
                        var files = Directory.EnumerateFiles(friend_path, "*.ixi");
                        if (files.Count() > 0)
                        {
                            foreach(var file in files)
                            {
                                File.Delete(file);
                            }
                        }
                    }else
                    {
                        var files = Directory.EnumerateFiles(friend_path, "*.ixi");
                        if (files.Count() > 0)
                        {
                            string channel_path = Path.Combine(friend_path, "0");
                            if (!Directory.Exists(channel_path))
                            {
                                Directory.CreateDirectory(channel_path);
                            }
                            foreach (var file in files)
                            {
                                if (!friend.bot)
                                {
                                    File.Move(file, Path.Combine(channel_path, Path.GetFileName(file)));
                                }else
                                {
                                    File.Delete(file);
                                }
                            }
                        }
                    }

                    FriendList.addFriend(friend);
                }
            }
            catch (Exception e)
            {
                Logging.error("Cannot read from account file: {0}", e);
            }

            reader.Close();

            if (version < 2)
            {
                writeAccountFile();
            }

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
                catch (Exception e)
                {
                    Logging.error("Cannot create account file: {0}", e.Message);
                    return false;
                }

                try
                {
                    // TODO: encrypt written data
                    int version = 2; // Set the account file version
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
                catch (Exception e)
                {
                    Logging.error("Cannot write to account file: {0}", e.Message);
                }
                writer.Close();
            }
            return true;
        }

        // Deletes the account file if it exists
        public bool deleteAccountFile()
        {
            lock (accountLock)
            {
                string account_filename = Path.Combine(documentsPath, accountFileName);

                if (File.Exists(account_filename) == false)
                {
                    return false;
                }

                File.Delete(account_filename);
                return true;
            }
        }

        private List<FriendMessage> readMessagesFile(string path)
        {
            lock (messagesLock)
            {
                List<FriendMessage> messages = new List<FriendMessage>();
                string messages_path = path;

                if (File.Exists(messages_path) == false)
                {
                    // Return an empty list of messages
                    return messages;
                }

                BinaryReader reader;
                try
                {
                    reader = new BinaryReader(new FileStream(messages_path, FileMode.Open));
                }
                catch (Exception e)
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
                        int msg_len = reader.ReadInt32();
                        FriendMessage msg = new FriendMessage(reader.ReadBytes(msg_len));
                        messages.Add(msg);

                        string t_file_name = Path.GetFileName(msg.filePath);
                        try
                        {
                            if (msg.type == FriendMessageType.fileHeader && msg.completed == false)
                            {
                                if (msg.localSender)
                                {
                                    // TODO may not work on Android/iOS due to unauthorized access
                                    FileStream fs = new FileStream(msg.filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    TransferManager.prepareFileTransfer(t_file_name, fs, msg.filePath, msg.transferId);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.error("Error occured while trying to prepare file transfer for file '{0}' - friend '{1}', message contents '{2}' full path '{3}': {4}", t_file_name, path, msg.message, msg.filePath, e);
                        }
                    }

                }
                catch (Exception e)
                {
                    Logging.error("Cannot read from chat file. {0}", e.Message);
                    // TODO TODO notify the user or something like that
                }

                reader.Close();

                return messages;
            }
        }

        private long getTimestampFromFileName(string filename)
        {
            return Int64.Parse(Path.GetFileNameWithoutExtension(filename));
        }

        // Reads the message archive for a given wallet
        public List<FriendMessage> readLastMessages(byte[] wallet_bytes, int channel, long from_time_stamp = 0, int msg_count = 100, bool reverse = true)
        {
            lock (messagesLock)
            {
                string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);
                string messages_path = Path.Combine(documentsPath, "Chats", wallet, channel.ToString());

                List<FriendMessage> messages = new List<FriendMessage>();

                if (!Directory.Exists(messages_path))
                {
                    return messages;
                }
                string[] files = Directory.GetFiles(messages_path, "*.ixi");
                if (reverse)
                {
                    files = files.OrderByDescending(x => x).ToArray();
                }else
                {
                    files = files.OrderBy(x => x).ToArray();
                }

                for (int i = 0; i < files.Count(); i++)
                {
                    // handle from_time_stamp skip
                    if(reverse)
                    {
                        // skip to the first file to read from in reverse
                        if (from_time_stamp != 0 && getTimestampFromFileName(files[i]) >= from_time_stamp)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // skip to the first file to read from
                        if (i + 1 < files.Count() && getTimestampFromFileName(files[i + 1]) < from_time_stamp)
                        {
                            continue;
                        }
                    }
                    string path = files[i];
                    var tmp_msgs = readMessagesFile(path);
                    int msgs_to_take = msg_count - messages.Count();
                    if (reverse)
                    {
                        int msgs_to_skip = tmp_msgs.Count() - msgs_to_take;
                        if (msgs_to_skip < 0)
                        {
                            msgs_to_skip = 0;
                        }
                        if(from_time_stamp != 0 && messages.Count() == 0)
                        {
                            // Remove all messages that are newer than the from_time_stamp
                            tmp_msgs.RemoveAll(x => x.receivedTimestamp >= from_time_stamp);
                        }
                        messages.InsertRange(0, tmp_msgs.Skip(msgs_to_skip).Take(msgs_to_take));
                    }else
                    {
                        if (messages.Count() == 0)
                        {
                            // Remove all messages that are older than the from_time_stamp
                            tmp_msgs.RemoveAll(x => x.receivedTimestamp <= from_time_stamp);
                        }
                        messages.AddRange(tmp_msgs.Take(msgs_to_take));
                    }
                    if (messages.Count() >= msg_count)
                    {
                        break;
                    }
                }

                return messages;
            }
        }

        private string getMessagesFullPath(string wallet_address, int channel, long min_timestamp)
        {
            string messages_path = Path.Combine(documentsPath, "Chats", wallet_address, channel.ToString());
            var files = Directory.GetFiles(messages_path, "*.ixi").OrderBy(x => x).ToArray();
            for(int i = 0; i < files.Count(); i++)
            {
                if (i + 1 < files.Count() && getTimestampFromFileName(files[i + 1]) > min_timestamp)
                {
                    return files[i];
                }
            }
            if(files.Count() > 0)
            {
                return files.Last();
            }

            return null;
        }

        // Writes the message archive for a given wallet
        public bool writeMessages(byte[] wallet_bytes, int channel, List<FriendMessage> messages)
        {
            List<FriendMessage> local_messages = null;
            lock (messages)
            {
                local_messages = messages.OrderBy(x => x.receivedTimestamp).ToList();
            }
            lock (messagesLock)
            {
                string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);
                string messages_path = Path.Combine(documentsPath, "Chats", wallet, channel.ToString());

                if (!Directory.Exists(messages_path))
                {
                    Directory.CreateDirectory(messages_path);
                }

                string messages_full_path = getMessagesFullPath(wallet, channel, local_messages.First().receivedTimestamp);
                if (messages_full_path == null)
                {
                    messages_full_path = Path.Combine(messages_path, local_messages.First().receivedTimestamp + ".ixi");
                }
                else
                {
                    var tmp_messages = readMessagesFile(messages_full_path);
                    tmp_messages.RemoveAll(x => x.receivedTimestamp >= local_messages.First().receivedTimestamp);

                    local_messages.InsertRange(0, tmp_messages);
                }

                bool first = true;
                for (int i = 0; i < local_messages.Count;)
                {
                    BinaryWriter writer;
                    if (!first)
                    {
                        messages_full_path = Path.Combine(messages_path, local_messages[i].receivedTimestamp + ".ixi");
                    }
                    first = false;
                    try
                    {
                        // Prepare the file for writing
                        writer = new BinaryWriter(new FileStream(messages_full_path, FileMode.Create));
                    }
                    catch (Exception e)
                    {
                        Logging.log(LogSeverity.error, String.Format("Cannot create chat file. {0}", e.Message));
                        return false;
                    }

                    try
                    {
                        // TODO: encrypt written data
                        System.Int32 version = 3; // Set the messages file version
                        writer.Write(version);
                        // Write the address used for verification
                        writer.Write(wallet);

                        var messages_to_write = local_messages.Skip(i).Take(messagesPerFile);

                        int message_num = messages_to_write.Count();
                        writer.Write(message_num);

                        foreach (FriendMessage message in messages_to_write)
                        {
                            byte[] msg_bytes = message.getBytes();
                            if (msg_bytes != null)
                            {
                                writer.Write(msg_bytes.Length);
                                writer.Write(msg_bytes);
                            }
                            else
                            {
                                writer.Write((int)0);
                            }
                            i++;
                        }

                    }
                    catch (Exception e)
                    {
                        Logging.error("Cannot write to chat file. {0}", e.Message);
                    }
                    writer.Close();
                }

                return true;
            }
        }

        // Deletes the message archive if it exists for a given wallet
        public bool deleteMessages(byte[] wallet_bytes)
        {
            lock (messagesLock)
            {
                string wallet = Base58Check.Base58CheckEncoding.EncodePlain(wallet_bytes);
                string chats_path = Path.Combine(documentsPath, "Chats");
                string messages_directory = Path.Combine(chats_path, wallet);

                if (!Directory.Exists(messages_directory))
                {
                    return false;
                }

                Directory.Delete(messages_directory, true);

                return true;
            }
        }

        public bool deleteTransactionCacheFile()
        {
            lock (txCacheLock)
            {
                string tx_filename = Path.Combine(documentsPath, txCacheFileName);

                if (File.Exists(tx_filename) == false)
                {
                    return false;
                }

                File.Delete(tx_filename);
                return true;
            }
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
            catch (Exception e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open file. {0}", e.Message));
                return false;
            }

            TransactionCache.clearAllTransactions();

            System.Int32 version = 0;
            try
            {
                version = reader.ReadInt32();

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
                int num_utx = reader.ReadInt32();
                for (int i = 0; i < num_utx; i++)
                {
                    int data_length = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(data_length);

                    Transaction transaction = new Transaction(data, true);
                    TransactionCache.addUnconfirmedTransaction(transaction, false);
                    Node.tiv.receivedNewTransaction(transaction);
                }

            }
            catch (Exception e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot read from file. {0}", e.Message));
                reader.Close();
                return false;
            }

            reader.Close();

            if(version < 2)
            {
                writeTransactionCacheFile();
            }

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
                catch (Exception e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot create file. {0}", e.Message));
                    return false;
                }

                try
                {
                    // TODO: encrypt written data
                    System.Int32 version = 2; // Set the tx cache file version
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
                catch (Exception e)
                {
                    Logging.log(LogSeverity.error, String.Format("Cannot write to file. {0}", e.Message));
                    writer.Close();
                    return false;
                }
                writer.Close();
            }

            return true;
        }

        // Write the account file to local storage
        public bool writeAvatar(string friend_address, byte[] avatar_bytes)
        {
            lock (avatarLock)
            {
                string avatar_filename = Path.Combine(avatarsPath, friend_address + ".jpg");

                File.WriteAllBytes(avatar_filename, avatar_bytes);
            }
            return true;
        }

        // Deletes the avatar file if it exists
        public bool deleteAvatar(string friend_address)
        {
            lock (avatarLock)
            {
                string avatar_filename = Path.Combine(avatarsPath, friend_address + ".jpg");

                if (File.Exists(avatar_filename) == false)
                {
                    return false;
                }

                File.Delete(avatar_filename);

                avatar_filename = Path.Combine(avatarsPath, friend_address + "_128.jpg");

                if (File.Exists(avatar_filename))
                {
                    File.Delete(avatar_filename);
                }
            }
            return true;
        }

        public void deleteAllAvatars()
        {
            Directory.Delete(avatarsPath, true);
            Directory.CreateDirectory(avatarsPath);
        }

        public string getAvatarPath(string friend_address, bool thumb = true)
        {
            string size_str = "";
            if(thumb)
            {
                size_str = "_128";
            }

            string avatar_filename = Path.Combine(avatarsPath, friend_address + size_str + ".jpg");

            if (File.Exists(avatar_filename))
            {
                string ts = "?t=" + File.GetLastWriteTimeUtc(avatar_filename).Second; // Unique parameter for proper HTML based refresh
                return avatar_filename + ts;
            }
            return null;
        }

        public void deleteAllDownloads()
        {
            string downloadsPath = Path.Combine(documentsPath, "Downloads");
            Directory.Delete(downloadsPath, true);
            Directory.CreateDirectory(downloadsPath);
        }
    }
}
