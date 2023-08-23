using IXICore;
using IXICore.Meta;
using IXICore.SpixiBot;
using IXICore.Utils;
using SPIXI.Meta;


namespace SPIXI
{
    public class FriendMetaData
    {
        public BotInfo botInfo = null;
        public Dictionary<int, byte[]> lastReceivedMessageIds = new Dictionary<int, byte[]>(); // Used primarily for bot purposes
        public FriendMessage lastMessage { get; private set; }
        public int lastMessageChannel { get; private set; }

        public int unreadMessageCount = 0;

        public FriendMetaData()
        {
            lastMessage = null;
            lastMessageChannel = 0;
            unreadMessageCount = 0;
        }


        public FriendMetaData(byte[] bytes)
        {

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int version = reader.ReadInt32();

                    int bot_info_len = reader.ReadInt32();
                    if (bot_info_len > 0)
                    {
                        botInfo = new BotInfo(reader.ReadBytes(bot_info_len));
                    }

                    int msg_count = reader.ReadInt32();
                    for (int i = 0; i < msg_count; i++)
                    {
                        int channel = reader.ReadInt32();
                        int msg_len = reader.ReadInt32();
                        lastReceivedMessageIds.Add(channel, reader.ReadBytes(msg_len));
                    }

                    int last_message_len = reader.ReadInt32();
                    if (last_message_len > 0)
                    {
                        lastMessage = new FriendMessage(reader.ReadBytes(last_message_len));
                    }

                    lastMessageChannel = reader.ReadInt32();

                    unreadMessageCount = reader.ReadInt32();
                }
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(0);

                    if (botInfo != null)
                    {
                        byte[] bi_bytes = botInfo.getBytes();
                        writer.Write(bi_bytes.Length);
                        writer.Write(bi_bytes);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    lock (lastReceivedMessageIds)
                    {
                        writer.Write(lastReceivedMessageIds.Count);
                        foreach (var msg in lastReceivedMessageIds)
                        {
                            writer.Write(msg.Key);
                            writer.Write(msg.Value.Length);
                            writer.Write(msg.Value);
                        }
                    }

                    if (lastMessage != null)
                    {
                        byte[] msg_bytes = lastMessage.getBytes();
                        writer.Write(msg_bytes.Length);
                        writer.Write(msg_bytes);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write(lastMessageChannel);

                    writer.Write(unreadMessageCount);
                }
                return m.ToArray();
            }
        }

        public void setLastMessage(FriendMessage msg, int channel)
        {
            lastMessage = new FriendMessage(msg.getBytes());
            lastMessageChannel = channel;
        }

        public bool setLastReceivedMessageIds(byte[] msg_id, int channel)
        {
            if (botInfo != null)
            {
                lock (lastReceivedMessageIds)
                {
                    lastReceivedMessageIds.AddOrReplace(channel, msg_id);
                    return true;
                }
            }
            return false;
        }

        public void setUnreadMessageCount(int count)
        {
            unreadMessageCount = count;
        }
    }

    public enum FriendState
    {
        RequestSent,
        RequestReceived,
        Approved,
        Ignored,
        Unknown
    }

    public class Friend
    {
        public Address walletAddress { get; private set; }
        public byte[] publicKey { get; private set; }

        private string _nick = "";
        private string userDefinedNick = "";

        public byte[] chachaKey = null; // TODO TODO don't keep keys in plaintext in memory
        public byte[] aesKey = null; // TODO TODO don't keep keys in plaintext in memory
        public long keyGeneratedTime = 0;

        public string relayIP = null;
        public byte[] relayWallet = null;

        public bool online = false;

        public bool forcePush = false; // on error - for bypassing trying to resend to the same S2 and sending directly to push server

        public long addedTimestamp = 0;

        private Dictionary<int, List<FriendMessage>> messages = new Dictionary<int, List<FriendMessage>>();

        public BotUsers users = null;
        public BotGroups groups = null;
        public BotChannels channels = null;
        public FriendMetaData metaData = new FriendMetaData();

        public SingleChatPage chat_page = null;

        public bool approved = true;

        public bool bot { get;  private set; }

        private int _handshakeStatus = 0;

        public bool handshakePushed = false;

        public long lastReceivedHandshakeMessageTimestamp = 0;

        public bool pendingDeletion = false;

        private object saveLock = new object();

        public FriendState state = FriendState.Unknown;

        public Friend(FriendState friend_state, Address wallet, byte[] public_key, string nick, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approve = true)
        {
            state = friend_state;
            walletAddress = wallet;
            publicKey = public_key;
            nickname = nick;
            approved = approve;

            chachaKey = chacha_key;
            aesKey = aes_key;
            keyGeneratedTime = key_generated_time;
            bot = false;
            addedTimestamp = Clock.getNetworkTimestamp();
        }

        public void setBotMode()
        {
            bot = true;
            string base_path = Path.Combine(FriendList.accountsPath, walletAddress.ToString());
            users = new BotUsers(Path.Combine(base_path, "contacts.dat"), null, true);
            users.loadContactsFromFile();
            groups = new BotGroups(Path.Combine(base_path, "groups.dat"));
            groups.loadGroupsFromFile();
            channels = new BotChannels(Path.Combine(base_path, "channels.dat"));
            channels.loadChannelsFromFile();
        }

        public Friend(byte[] bytes, int version = 5)
        {

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    if(version >= 4)
                    {
                        version = reader.ReadInt32();
                    }

                    int wal_length = reader.ReadInt32();
                    walletAddress = new Address(reader.ReadBytes(wal_length));

                    int pkey_length = reader.ReadInt32();
                    if (pkey_length > 0)
                    {
                        publicKey = reader.ReadBytes(pkey_length);
                    }

                    _nick = reader.ReadString(); // use internal variable, to avoid writing to file

                    int aes_len = reader.ReadInt32();
                    if (aes_len > 0)
                    {
                        aesKey = reader.ReadBytes(aes_len);
                    }

                    int cc_len = reader.ReadInt32();
                    if (cc_len > 0)
                    {
                        chachaKey = reader.ReadBytes(cc_len);
                    }

                    keyGeneratedTime = reader.ReadInt64();

                    approved = reader.ReadBoolean();

                    _handshakeStatus = reader.ReadInt32(); // use internal variable, to avoid writing to file

                    bot = reader.ReadBoolean();
                    handshakePushed = reader.ReadBoolean();

                    if(version >= 4)
                    {
                        lastReceivedHandshakeMessageTimestamp = reader.ReadInt64();
                        try
                        {
                            pendingDeletion = reader.ReadBoolean();
                            userDefinedNick = reader.ReadString();
                        }catch(Exception)
                        {

                        }
                    }

                    if(version >= 5)
                    {
                        addedTimestamp = reader.ReadInt64();
                    }

                    if (bot)
                    {
                        setBotMode();
                    }

                    if (version >= 6)
                    {
                        state = (FriendState)reader.ReadInt32();
                    }
                    else
                    {
                        // "Upgrade" previous version friend state to approved if aes key is present
                        if(aesKey != null)
                            state = FriendState.Approved;
                    }

                }
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(6);

                    writer.Write(walletAddress.addressNoChecksum.Length);
                    writer.Write(walletAddress.addressNoChecksum);
                    if (publicKey != null)
                    {
                        writer.Write(publicKey.Length);
                        writer.Write(publicKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }
                    writer.Write(nickname);

                    // encryption keys
                    if (aesKey != null)
                    {
                        writer.Write(aesKey.Length);
                        writer.Write(aesKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    if (chachaKey != null)
                    {
                        writer.Write(chachaKey.Length);
                        writer.Write(chachaKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write(keyGeneratedTime);

                    writer.Write(approved);

                    writer.Write(handshakeStatus);

                    writer.Write(bot);

                    writer.Write(handshakePushed);

                    writer.Write(lastReceivedHandshakeMessageTimestamp);

                    writer.Write(pendingDeletion);

                    writer.Write(userDefinedNick);

                    writer.Write(addedTimestamp);

                    writer.Write((int)state); // current FriendState

                }
                return m.ToArray();
            }
        }

        // Get the number of unread messages
        public int getUnreadMessageCount()
        {
            return metaData.unreadMessageCount;
        }

        // Flushes the temporary message history
        public bool flushHistory()
        {
            lock (messages)
            {
                messages.Clear();
            }
            return true;
        }

        // Deletes the history file and flushes the temporary history
        public bool deleteHistory()
        {

            if (Node.localStorage.deleteMessages(walletAddress) == false)
                return false;

            if (flushHistory() == false)
                return false;

            return true;
        }


        // Generates a random chacha key and a random aes key
        // Returns the two keys encrypted using the supplied public key
        // Returns false if not enough time has passed to generate the keys
        public bool generateKeys()
        {
            // TODO TODO TODO keys should be re-generated periodically
            try
            {
                if (aesKey == null)
                {
                    aesKey = CryptoManager.lib.getSecureRandomBytes(32);
                    return true;
                }

                if (chachaKey == null)
                {
                    chachaKey = CryptoManager.lib.getSecureRandomBytes(32);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during generate keys: {0}", e.Message));
            }

            return false;
        }

        public bool sendKeys(int selected_key)
        {
            try
            {
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        if (aesKey != null && selected_key != 2)
                        {
                            writer.Write(aesKey.Length);
                            writer.Write(aesKey);
                            Logging.info("Sending aes key");
                        }else
                        {
                            writer.Write(0);
                        }

                        if (chachaKey != null && selected_key != 1)
                        {
                            writer.Write(chachaKey.Length);
                            writer.Write(chachaKey);
                            Logging.info("Sending chacha key");
                        }
                        else
                        {
                            writer.Write(0);
                        }

                        Logging.info("Preparing key message");

                        SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.keys, m.ToArray());

                        // Send the key to the recipient
                        StreamMessage sm = new StreamMessage();
                        sm.type = StreamMessageCode.info;
                        sm.recipient = walletAddress;
                        sm.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
                        sm.data = spixi_message.getBytes();
                        sm.encryptionType = StreamMessageEncryptionCode.rsa;
                        sm.id = new byte[] { 2 };

                        StreamProcessor.sendMessage(this, sm);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during send keys: {0}", e.Message));
            }

            return false;
        }

        // Handles receiving and decryption of keys
        public bool receiveKeys(byte[] data)
        {
            try
            {
                Logging.info("Received keys");
                byte[] decrypted = data;

                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        // Read and assign the aes password
                        int aes_length = reader.ReadInt32();
                        byte[] aes = null;
                        if (aes_length > 0)
                        {
                            aes = reader.ReadBytes(aes_length);
                        }

                        // Read the chacha key
                        int cc_length = reader.ReadInt32();
                        byte[] chacha = null;
                        if (cc_length > 0)
                        {
                            chacha = reader.ReadBytes(cc_length);
                        }

                        if (aesKey == null)
                        {
                            aesKey = aes;
                        }

                        if (chachaKey == null)
                        {
                            chachaKey = chacha;
                        }

                        // Everything succeeded
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during receive keys: {0}", e.Message));
            }

            return false;
        }

        // Retrieve the friend's connected S2 node address. Returns null if not found
        public string searchForRelay()
        {
            relayIP = null;
            relayWallet = null;

            string hostname = FriendList.getRelayHostname(walletAddress);

            if (hostname != null && hostname != "")
            {
                // Store the last relay ip and wallet for this friend
                relayIP = hostname;
            }
            // Finally, return the ip address of the node
            return relayIP;
        }

        public bool setMessageRead(int channel, byte[] id)
        {
            var tmp_messages = getMessages(channel);
            if(tmp_messages == null)
            {
                return false;
            }

            FriendMessage msg = tmp_messages.Find(x => x.id.SequenceEqual(id));
            if(msg == null)
            {
                Logging.error("Error trying to set read indicator, message does not exist");
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.read)
                {
                    msg.read = true;
                    Node.localStorage.requestWriteMessages(walletAddress, channel);
                }

                if(chat_page != null)
                {
                    chat_page.updateMessage(msg);
                }
            }

            return true;
        }

        public bool setMessageReceived(int channel, byte[] id)
        {
            if(channel == -1)
            {
                return false;
            }
            var tmp_messages = getMessages(channel);
            if (tmp_messages == null)
            {
                return false;
            }
            FriendMessage msg = tmp_messages.Find(x => x.id.SequenceEqual(id));
            if (msg == null)
            {
                Logging.error("Error trying to set received indicator, message from {0} for channel {1} does not exist", walletAddress.ToString(), channel.ToString());
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.confirmed)
                {
                    msg.confirmed = true;
                    Node.localStorage.requestWriteMessages(walletAddress, channel);
                }

                if (chat_page != null)
                {
                    chat_page.updateMessage(msg);
                }
            }

            return true;
        }


        public bool setMessageSent(int channel, byte[] id)
        {
            var tmp_messages = getMessages(channel);
            if (tmp_messages == null)
            {
                return false;
            }
            FriendMessage msg = tmp_messages.Find(x => x.id.SequenceEqual(id));
            if (msg == null)
            {
                Logging.error("Error trying to set sent indicator, message from {0} for channel {1} does not exist", walletAddress.ToString(), channel.ToString());
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.sent)
                {
                    msg.sent = true;
                    Node.localStorage.requestWriteMessages(walletAddress, channel);
                }

                if (chat_page != null)
                {
                    chat_page.updateMessage(msg);
                }
            }

            return true;
        }

        public int handshakeStatus
        {
            get
            {
                return _handshakeStatus;
            }
            set
            {
                if (_handshakeStatus != value)
                {
                    _handshakeStatus = value;
                    handshakePushed = false;
                    save();
                }
            }
        }

        public string nickname
        {
            get
            {
                if(userDefinedNick != "")
                {
                    return userDefinedNick;
                }
                return _nick;
            }
            set
            {
                if (_nick != value)
                {
                    _nick = value;
                    save();
                }
            }
        }

        public void setUserDefinedNick(string nick)
        {
            userDefinedNick = nick;
            save();
        }

        public void endCall(byte[] session_id, bool call_accepted, long call_duration, bool local_sender)
        {
            if(session_id == null)
            {
                return;
            }
            lock (messages)
            {
                var tmp_messages = getMessages(0);
                if (tmp_messages == null)
                {
                    return;
                }
                var fm = tmp_messages.Find(x => x.id.SequenceEqual(session_id));
                if(fm == null)
                {
                    Logging.warn("Cannot end call, no message with session ID exists.");
                    return;
                }
                if (call_accepted == true && tmp_messages.Last() != fm)
                {
                    fm.message = call_duration.ToString();
                    FriendList.addMessageWithType(null, FriendMessageType.voiceCallEnd, walletAddress, 0, fm.message, local_sender, null, 0, false);
                }
                else
                {
                    fm.type = FriendMessageType.voiceCallEnd;
                    if (call_accepted)
                    {
                        fm.message = call_duration.ToString();
                    }
                    Node.localStorage.requestWriteMessages(walletAddress, 0);
                    if (chat_page != null)
                    {
                        chat_page.insertMessage(fm, 0);
                    }
                }
            }
        }

        public bool hasMessage(int channel, byte[] message_id)
        {
            var tmp_messages = getMessages(channel);
            if(tmp_messages == null)
            {
                return false;
            }

            var fm = tmp_messages.Find(x => x.id.SequenceEqual(message_id));
            if(fm == null)
            {
                return false;
            }

            return true;
        }

        public List<FriendMessage> getMessages(int channel)
        {
            try
            {
                if (channels != null && !channels.hasChannel(channel))
                {
                    Logging.error("Error getting messages for {0}, channel {1} does not exist", walletAddress.ToString(), channel.ToString());
                    return null;
                }
                lock (messages)
                {
                    if (!messages.ContainsKey(channel))
                    {
                        // Read messages from chat history
                        messages[channel] = Node.localStorage.readLastMessages(walletAddress, channel);
                    }
                    return messages[channel];
                }
            }
            catch (Exception e)
            {
                Logging.error("Error reading contact's {0} messages: {1}", walletAddress.ToString(), e);
            }
            return null;
        }

        public IxiNumber getMessagePrice(int msg_len)
        {
            return metaData.botInfo.cost * msg_len / 1000;
        }

        public bool deleteMessage(byte[] msg_id, int channel)
        {
            lock (messages)
            {
                var tmp_messages = getMessages(channel);
                if (tmp_messages == null)
                {
                    return false;
                }
                FriendMessage fm = tmp_messages.Find(x => x.id.SequenceEqual(msg_id));
                if (fm != null)
                {
                    tmp_messages.Remove(fm);
                    Node.localStorage.requestWriteMessages(walletAddress, channel);
                    if(chat_page != null)
                    {
                        chat_page.deleteMessage(msg_id, channel);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool addReaction(Address sender_address, SpixiMessageReaction reaction_data, int channel)
        {
            if(!reaction_data.reaction.StartsWith("tip:") && !reaction_data.reaction.StartsWith("like:"))
            {
                Logging.warn("Invalid reaction data: " + reaction_data.reaction);
                return false;
            }
            if(reaction_data.reaction.Length > 128)
            {
                Logging.warn("Invalid reaction data length: " + reaction_data.reaction);
                return false;
            }
            lock (messages)
            {
                var tmp_messages = getMessages(channel);
                if (tmp_messages == null)
                {
                    return false;
                }
                FriendMessage fm = tmp_messages.Find(x => x.id.SequenceEqual(reaction_data.msgId));
                if (fm != null)
                {
                    if(fm.reactions.Count >= 10)
                    {
                        Logging.warn("Too many reactions on message " + Crypto.hashToString(reaction_data.msgId));
                        return false;
                    }
                    if (fm.addReaction(sender_address, reaction_data.reaction))
                    {
                        Node.localStorage.requestWriteMessages(walletAddress, channel);
                        if (chat_page != null)
                        {
                            chat_page.updateReactions(fm.id, channel);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public void freeMemory()
        {
            if (chat_page != null)
            {
                return;
            }

            lock(messages)
            {
                Node.localStorage.flush();
                messages.Clear();
            }
        }

        public void save()
        {
            lock (saveLock)
            {
                string base_path = Path.Combine(FriendList.accountsPath, walletAddress.ToString());
                if (!Directory.Exists(base_path))
                {
                    Directory.CreateDirectory(base_path);
                }

                File.WriteAllBytes(Path.Combine(base_path, "account.ixi"), getBytes());
            }
        }

        public void saveMetaData()
        {
            lock (saveLock)
            {
                string base_path = Path.Combine(FriendList.accountsPath, walletAddress.ToString());
                if (!Directory.Exists(base_path))
                {
                    Directory.CreateDirectory(base_path);
                }

                File.WriteAllBytes(Path.Combine(base_path, "meta.ixi"), metaData.getBytes());
            }
        }

        public void loadMetaData()
        {
            string path = Path.Combine(FriendList.accountsPath, walletAddress.ToString(), "meta.ixi");
            if (File.Exists(path))
            {
                metaData = new FriendMetaData(File.ReadAllBytes(path));
            }
        }

        public void setPublicKey(byte[] public_key)
        {
            if (publicKey == null)
            {
                publicKey = public_key;
                save();
            }
        }

        public void delete()
        {
            lock (saveLock)
            {
                string base_path = Path.Combine(FriendList.accountsPath, walletAddress.ToString());
                if (Directory.Exists(base_path))
                {
                    Directory.Delete(base_path, true);
                }
            }
        }
    }
}
