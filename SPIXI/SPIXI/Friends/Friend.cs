using IXICore;
using IXICore.Meta;
using IXICore.SpixiBot;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SPIXI
{
    public class Friend
    {
        public byte[] walletAddress;
        public byte[] publicKey;

        private string _nick = "";

        public byte[] chachaKey = null; // TODO TODO don't keep keys in plaintext in memory
        public byte[] aesKey = null; // TODO TODO don't keep keys in plaintext in memory
        public long keyGeneratedTime = 0;

        public string relayIP = null;
        public byte[] relayWallet = null;

        public bool online = false;

        public bool forcePush = false; // on error - for bypassing trying to resend to the same S2 and sending directly to push server

        private Dictionary<int, List<FriendMessage>> messages = new Dictionary<int, List<FriendMessage>>();

        public BotInfo botInfo = null;
        public BotUsers users = null;
        public BotGroups groups = null;
        public BotChannels channels = null;

        public SingleChatPage chat_page = null;

        public bool approved = true;

        public bool bot { get;  private set; }

        private int _handshakeStatus = 0;

        public bool handshakePushed = false;

        public Dictionary<int, byte[]> lastReceivedMessageIds = new Dictionary<int, byte[]>(); // Used primarily for bot purposes

        public long lastReceivedHandshakeMessageTimestamp = 0;

        public FriendMessage lastMessage = null;

        public Friend(byte[] wallet, byte[] public_key, string nick, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approve = true)
        {
            walletAddress = wallet;
            publicKey = public_key;
            nickname = nick;
            approved = approve;

            chachaKey = chacha_key;
            aesKey = aes_key;
            keyGeneratedTime = key_generated_time;
            bot = false;
        }

        public void setBotMode()
        {
            bot = true;
            string base_path = Path.Combine(Config.spixiUserFolder, "Chats", Base58Check.Base58CheckEncoding.EncodePlain(walletAddress));
            users = new BotUsers(Path.Combine(base_path, "contacts.dat"), null, true);
            users.loadContactsFromFile();
            groups = new BotGroups(Path.Combine(base_path, "groups.dat"));
            groups.loadGroupsFromFile();
            channels = new BotChannels(Path.Combine(base_path, "channels.dat"));
            channels.loadChannelsFromFile();
        }

        public Friend(byte[] bytes, int version)
        {

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int wal_length = reader.ReadInt32();
                    walletAddress = reader.ReadBytes(wal_length);

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

                    if (bot)
                    {
                        setBotMode();
                    }

                    if (version < 2)
                    {
                        int num_contacts = reader.ReadInt32();
                        for (int i = 0; i < num_contacts; i++)
                        {
                            int contact_len = reader.ReadInt32();

                            BotContact contact = new BotContact(reader.ReadBytes(contact_len), true);
                            users.contacts.Add(new Address(contact.publicKey).address, contact);
                        }

                        int rcv_msg_id_len = reader.ReadInt32();
                        if (rcv_msg_id_len > 0)
                        {
                            reader.ReadBytes(rcv_msg_id_len);
                        }
                    }
                    else
                    {
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
                    }

                    lastReceivedHandshakeMessageTimestamp = reader.ReadInt64();

                    // TODO try/catch wrapper can be removed after upgrade
                    try
                    {
                        int last_message_len = reader.ReadInt32();
                        if(last_message_len > 0)
                        {
                            lastMessage = new FriendMessage(reader.ReadBytes(last_message_len));
                        }
                    }
                    catch(Exception)
                    {

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

                    writer.Write(walletAddress.Length);
                    writer.Write(walletAddress);
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

                    if (botInfo != null)
                    {
                        byte[] bi_bytes = botInfo.getBytes();
                        writer.Write(bi_bytes.Length);
                        writer.Write(bi_bytes);
                    }else
                    {
                        writer.Write(0);
                    }

                    lock(lastReceivedMessageIds)
                    {
                        writer.Write(lastReceivedMessageIds.Count);
                        foreach(var msg in lastReceivedMessageIds)
                        {
                            writer.Write(msg.Key);
                            writer.Write(msg.Value.Length);
                            writer.Write(msg.Value);
                        }
                    }

                    writer.Write(lastReceivedHandshakeMessageTimestamp);

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

                }
                return m.ToArray();
            }
        }

        // Get the number of unread messages
        // TODO: optimize this
        public int getUnreadMessageCount()
        {
            int unreadCount = 0;
            lock(messages)
            {
                foreach (var i in messages.Keys)
                {
                    for (int j = messages[i].Count - 1; j >= 0; j--)
                    {
                        if (messages[i][j].read == true || messages[i][j].localSender == true)
                        {
                            break;
                        }
                        unreadCount++;
                    }
                }
            }
            return unreadCount;
        }

        // Flushes the temporary message history
        public bool flushHistory()
        {
            messages.Clear();
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
                        sm.sender = Node.walletStorage.getPrimaryAddress();
                        sm.transaction = new byte[1];
                        sm.sigdata = new byte[1];
                        sm.data = spixi_message.getBytes();
                        sm.encryptionType = StreamMessageEncryptionCode.rsa;
                        sm.id = new byte[] { 2 };

                        sm.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

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
            FriendMessage msg = messages[channel].Find(x => x.id.SequenceEqual(id));
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
                    Node.localStorage.writeMessages(walletAddress, channel, messages[channel]);
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
            FriendMessage msg = messages[channel].Find(x => x.id.SequenceEqual(id));
            if (msg == null)
            {
                Logging.error("Error trying to set received indicator, message does not exist");
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.confirmed)
                {
                    msg.confirmed = true;
                    Node.localStorage.writeMessages(walletAddress, channel, messages[channel]);
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
                    FriendList.saveToStorage();
                }
            }
        }

        public string nickname
        {
            get
            {
                return _nick;
            }
            set
            {
                if (_nick != value)
                {
                    _nick = value;
                    FriendList.saveToStorage();
                }
            }
        }

        public void endCall(byte[] session_id, bool call_accepted, long call_duration, bool local_sender)
        {
            if(session_id == null)
            {
                return;
            }
            lock (messages)
            {
                var fm = messages[0].Find(x => x.id.SequenceEqual(session_id));
                if(fm == null)
                {
                    Logging.warn("Cannot end call, no message with session ID exists.");
                    return;
                }
                if (call_accepted == true && messages[0].Last() != fm)
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
                    Node.localStorage.writeMessages(walletAddress, 0, messages[0]);
                    if (chat_page != null)
                    {
                        chat_page.insertMessage(fm, 0);
                    }
                }
            }
        }

        public bool hasMessage(int channel, byte[] message_id)
        {
            var fm = messages[channel].Find(x => x.id.SequenceEqual(message_id));
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
                lock (messages)
                {
                    if (!messages.ContainsKey(channel))
                    {
                        // Read messages from chat history
                        messages[channel] = Node.localStorage.readLastMessages(walletAddress, channel);
                    }
                    if (messages.ContainsKey(channel))
                    {
                        return messages[channel];
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Error reading contact's {0} messages: {1}", Base58Check.Base58CheckEncoding.EncodePlain(walletAddress), e);
            }
            return null;
        }

        public IxiNumber getMessagePrice(int msg_len)
        {
            return botInfo.cost * msg_len / 1000;
        }

        public void deleteMessage(byte[] msg_id, int channel)
        {
            lock (messages)
            {
                if (messages.ContainsKey(channel))
                {
                    FriendMessage fm = messages[channel].Find(x => x.id.SequenceEqual(msg_id));
                    if (fm != null)
                    {
                        messages[channel].Remove(fm);
                        Node.localStorage.writeMessages(walletAddress, channel, messages[channel]);
                    }
                }
            }
        }

        public void addReaction(byte[] sender_address, SpixiMessageReaction reaction_data, int channel)
        {
            lock (messages)
            {
                if (messages.ContainsKey(channel))
                {
                    FriendMessage fm = messages[channel].Find(x => x.id.SequenceEqual(reaction_data.msgId));
                    if (fm != null)
                    {
                        fm.addReaction(sender_address, reaction_data.reaction);
                        Node.localStorage.writeMessages(walletAddress, channel, messages[channel]);
                    }
                }
            }
        }
    }
}
