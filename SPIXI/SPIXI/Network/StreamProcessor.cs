using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.SpixiBot;
using SPIXI.CustomApps;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Network;
using SPIXI.Storage;
using SPIXI.VoIP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xamarin.Forms.Internals;

namespace SPIXI
{    
    class StreamProcessor
    {
        private static bool running = false;

        private static PendingMessageProcessor pendingMessageProcessor = null;

        // Initialize the global stream processor
        public static void initialize(string root_storage_path)
        {
            if (running)
            {
                return;
            }
            running = true;

            pendingMessageProcessor = new PendingMessageProcessor(root_storage_path);
            pendingMessageProcessor.start();
        }

        // Uninitialize the global stream processor
        public static void uninitialize()
        {
            running = false;
            if (pendingMessageProcessor != null)
            {
                pendingMessageProcessor.stop();
                pendingMessageProcessor = null;
            }
        }

        // Send an encrypted message using the S2 network
        public static void sendMessage(Friend friend, StreamMessage msg, bool add_to_pending_messages = true, bool send_to_server = true, bool send_push_notification = true, bool remove_after_sending = false)
        {
            pendingMessageProcessor.sendMessage(friend, msg, add_to_pending_messages, send_to_server, send_push_notification, remove_after_sending);
        }


        // Called when receiving encryption keys from the S2 node
        public static void handleReceivedKeys(byte[] sender, byte[] data)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                if(friend.handshakeStatus >= 3)
                {
                    return;
                }

                Logging.info("In received keys");

                friend.receiveKeys(data);

                friend.handshakeStatus = 3;

                sendNickname(friend);

                sendAvatar(friend);
            }
            else
            {
                // TODO TODO TODO handle this edge case, by displaying request to add notification to user
                Logging.error("Received keys for an unknown friend.");
            }
        }

        // Called when receiving file headers from the message recipient
        public static void handleFileHeader(byte[] sender, SpixiMessage data, byte[] message_id)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                FileTransfer transfer = new FileTransfer(data.data);

                string message_data = string.Format("{0}:{1}", transfer.uid, transfer.fileName);
                FriendMessage fm = FriendList.addMessageWithType(message_id, FriendMessageType.fileHeader, sender, data.channel, message_data);
                if (fm != null)
                {
                    fm.transferId = transfer.uid;
                    fm.filePath = transfer.fileName;
                    fm.fileSize = transfer.fileSize;
                    Node.localStorage.writeMessages(friend.walletAddress, transfer.channel, friend.getMessages(data.channel));
                }
            }
            else
            {
                Logging.error("Received File Header from an unknown friend.");
            }
        }

        // Called when accepting a file
        public static void handleAcceptFile(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                Logging.info("Received accept file");

                try
                {
                    using (MemoryStream m = new MemoryStream(data.data))
                    {
                        using (BinaryReader reader = new BinaryReader(m))
                        {
                            string uid = reader.ReadString();

                            TransferManager.receiveAcceptFile(friend, uid);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured while handling accept file from bytes: " + e);
                }
            }
            else
            {
                Logging.error("Received accept file from an unknown friend.");
            }
        }

        public static void handleRequestFileData(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                Logging.info("Received request file data");

                try
                {
                    using (MemoryStream m = new MemoryStream(data.data))
                    {
                        using (BinaryReader reader = new BinaryReader(m))
                        {
                            string uid = reader.ReadString();
                            ulong packet_number = reader.ReadUInt64();

                            TransferManager.sendFileData(friend, uid, packet_number);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.error("Exception occured while handling request file data from bytes: " + e);
                }

            }
            else
            {
                Logging.error("Received request file data from an unknown friend.");
            }
        }

        public static void handleFileData(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                TransferManager.receiveFileData(data.data, sender);
            }
            else
            {
                Logging.error("Received file data from an unknown friend.");
            }
        }

        public static void handleFileFullyReceived(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
            {
                Logging.error("Received file fully received from an unknown friend.");
                return;
            }

            TransferManager.completeFileTransfer(sender, Crypto.hashToString(data.data));
        }


        // Called when receiving received confirmation from the message recipient
        public static void handleMsgReceived(byte[] sender, int channel, byte[] msg_id)
        {
            Friend friend = FriendList.getFriend(sender);

            if (friend != null)
            {
                pendingMessageProcessor.removeMessage(friend, msg_id);

                Logging.info("Friend's handshake status is {0}", friend.handshakeStatus);

                if (msg_id.Length == 1)
                {
                    if (msg_id.SequenceEqual(new byte[] { 0 }))
                    {
                        if (friend.handshakeStatus == 0)
                        {
                            friend.handshakeStatus = 1;
                            Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                        }
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 1 }))
                    {
                        // ignore - accept add
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 2 }))
                    {
                        if (friend.handshakeStatus == 2)
                        {
                            friend.handshakeStatus = 3;
                            Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                        }
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 3 }))
                    {
                        // ignore - request nickname
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 4 }))
                    {
                        // ignore - request avatar
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 5 }))
                    {
                        // ignore - nickname
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 6 }))
                    {
                        // ignore - avatar
                        return;
                    }

                    if (msg_id.SequenceEqual(new byte[] { 10 }))
                    {
                        // ignore, bot related
                        return;
                    }
                }

                friend.setMessageReceived(channel, msg_id);
            }
            else
            {
                Logging.error("Received Message received confirmation for an unknown friend.");
            }
        }

        // Called when receiving read confirmation from the message recipient
        public static void handleMsgRead(byte[] sender, int channel, byte[] msg_id)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                friend.setMessageRead(channel, msg_id);
                pendingMessageProcessor.removeMessage(friend, msg_id);
            }
            else
            {
                Logging.error("Received Message read for an unknown friend.");
            }
        }

        // Called when an encryption key is received from the S2 server, as per step 4 of the WhitePaper
        /*private static void sendRsaEncryptedMessage(StreamMessage msg, string key, RemoteEndpoint endpoint)
        {
        // TODO TODO use transaction code for S2
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(msg.getID());
                    writer.Write(msg.recipientAddress);
                    writer.Write(msg.transactionID);
                }
            }
            Console.WriteLine("Sending encrypted message with key {0}", key);

                    using (MemoryStream m = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(m))
                        {
                            writer.Write(msg.getID());
                            writer.Write(msg.recipientAddress);
                            writer.Write(msg.transactionID);

                            byte[] encrypted_message = CryptoManager.lib.encryptDataS2(msg.data, key);
                            int encrypted_count = encrypted_message.Count();

                            writer.Write(encrypted_count);
                            writer.Write(encrypted_message);

                            byte[] ba = ProtocolMessage.prepareProtocolMessage(ProtocolMessageCode.s2data, m.ToArray());
                            socket.Send(ba, SocketFlags.None);


                            // Update the DLT transaction as well
                            Transaction transaction = new Transaction(0, msg.recipientAddress, Node.walletStorage.address);
                            transaction.id = msg.transactionID;
                            //transaction.data = Encoding.UTF8.GetString(checksum);
                            //ProtocolMessage.broadcastProtocolMessage(ProtocolMessageCode.updateTransaction, transaction.getBytes());

                        }
                    }
        }*/

        // Called when receiving S2 data from clients
        public static void receiveData(byte[] bytes, RemoteEndpoint endpoint)
        {
            StreamMessage message = new StreamMessage(bytes);

            if (message.data == null)
            {
                Logging.error(string.Format("Null message data."));
                return;
            }

            bool replaced_sender_address = false;
            byte[] real_sender_address = null;
            byte[] sender_address = message.sender;

            Friend tmp_friend = FriendList.getFriend(message.recipient);
            if (tmp_friend != null)
            {
                if (tmp_friend.bot)
                {
                    // message from a bot group chat
                    real_sender_address = message.sender;
                    sender_address = message.recipient;

                    replaced_sender_address = true;
                }else
                {
                    Logging.error("Received message intended for recipient {0} that isn't a bot.", Base58Check.Base58CheckEncoding.EncodePlain(tmp_friend.walletAddress));
                    return;
                }
            }else if(!Node.walletStorage.isMyAddress(message.recipient))
            {
                Logging.error("Received message for {0} but this address is not one of ours.", Base58Check.Base58CheckEncoding.EncodePlain(message.recipient));
                return;
            }


            //Logging.info("Received S2 data from {0} for {1}", Base58Check.Base58CheckEncoding.EncodePlain(sender_address), Base58Check.Base58CheckEncoding.EncodePlain(message.recipient));

            byte[] aes_key = null;
            byte[] chacha_key = null;

            Friend friend = FriendList.getFriend(sender_address);
            if (friend != null)
            {
                aes_key = friend.aesKey;
                chacha_key = friend.chachaKey;
                if(friend.publicKey == null && endpoint.presence.pubkey != null && endpoint.presence.wallet.SequenceEqual(friend.walletAddress))
                {
                    friend.publicKey = endpoint.presence.pubkey;
                }
            }

            int channel = 0;
            try
            {
                if (message.type == StreamMessageCode.error)
                {
                    // TODO Additional checks have to be added here, so that it's not possible to spoof errors (see .sender .reciver attributes in S2 as well) - it will somewhat be improved with protocol-level encryption as well
                    PresenceList.removeAddressEntry(friend.walletAddress);
                    friend.online = false;
                    friend.forcePush = true;
                    // TODO TODO current friend's keepalive has to be permanently discarded - i.e. save the timestamp
                    return;
                }

                // decrypt the message if necessary
                // TODO TODO TODO prevent encryption type downgrades
                if (message.encryptionType != StreamMessageEncryptionCode.none)
                {
                    if (!message.decrypt(Node.walletStorage.getPrimaryPrivateKey(), aes_key, chacha_key))
                    {
                        Logging.error("Could not decrypt message from {0}", Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        return;
                    }
                }

                // Extract the Spixi message
                SpixiMessage spixi_message = new SpixiMessage(message.data);

                if(spixi_message != null)
                {
                    channel = spixi_message.channel;
                }

                if(friend != null)
                {
                    if (message.encryptionType == StreamMessageEncryptionCode.none)
                    {
                        if (!friend.bot)
                        {
                            switch(spixi_message.type)
                            {
                                case SpixiMessageCode.msgReceived:
                                case SpixiMessageCode.requestAdd:
                                case SpixiMessageCode.acceptAddBot:
                                    break;
                                default:
                                    Logging.error("Expecting encrypted message from {0}", Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                                    return;
                            }
                        }
                    }
                }

                switch (spixi_message.type)
                {
                    case SpixiMessageCode.pubKey:
                        handlePubKey(sender_address, spixi_message.data);
                        break;
                    case SpixiMessageCode.chat:
                        {
                            // TODO Add a pending chat list for bots, add pending messages to the chat list until pubkey is received and uncoment the code below
                            /*if (replaced_sender_address && (!friend.contacts.ContainsKey(real_sender_address) || friend.contacts[real_sender_address].publicKey == null))
                            {
                                requestPubKey(friend, real_sender_address);
                            }
                            else if (replaced_sender_address && !message.verifySignature(friend.contacts[real_sender_address].publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                            }
                            else if (!replaced_sender_address && friend.bot && !message.verifySignature(friend.publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                            }
                            else
                            {*/
                            // Add the message to the friend list
                            FriendMessage fm = FriendList.addMessage(message.id, sender_address, spixi_message.channel, Encoding.UTF8.GetString(spixi_message.data), real_sender_address, message.timestamp);
                            if(fm != null)
                            {
                                if (friend.bot)
                                {
                                    fm.read = true;
                                    lock (friend.lastReceivedMessageIds)
                                    {
                                        friend.lastReceivedMessageIds.AddOrReplace(channel, message.id);
                                    }
                                    FriendList.saveToStorage();
                                }
                            }
                            //}
                        }
                        break;

                    case SpixiMessageCode.getNick:
                        {
                            // Send the nickname to the sender as requested
                            handleGetNick(sender_address, Encoding.UTF8.GetString(spixi_message.data));
                        }
                        break;

                    case SpixiMessageCode.nick:
                        {
                            // Set the nickname for the corresponding address
                            if (!replaced_sender_address && friend.publicKey != null
                                && message.encryptionType != StreamMessageEncryptionCode.spixi1
                                && !message.verifySignature(friend.publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                            }
                            else if (replaced_sender_address && (!friend.users.hasUser(real_sender_address) || friend.users.getUser(real_sender_address).publicKey == null))
                            {
                                requestPubKey(friend, real_sender_address);
                                requestNickname(friend, real_sender_address);
                            }
                            else if (replaced_sender_address && !message.verifySignature(friend.users.getUser(real_sender_address).publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                            }
                            else
                            {
                                if (spixi_message.data != null)
                                {
                                    FriendList.setNickname(sender_address, Encoding.UTF8.GetString(spixi_message.data), real_sender_address);
                                }
                                else
                                {
                                    FriendList.setNickname(sender_address, Base58Check.Base58CheckEncoding.EncodePlain(sender_address), real_sender_address);
                                }
                            }
                        }
                        break;

                    case SpixiMessageCode.getAvatar:
                        {
                            // Send the avatar to the sender as requested
                            handleGetAvatar(sender_address, Encoding.UTF8.GetString(spixi_message.data));
                        }
                        break;

                    case SpixiMessageCode.avatar:
                        {
                            // Set the avatar for the corresponding address
                            if (!replaced_sender_address && friend.publicKey != null
                                && message.encryptionType != StreamMessageEncryptionCode.spixi1
                                && !message.verifySignature(friend.publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                            }
                            else if (replaced_sender_address && (!friend.users.hasUser(real_sender_address) || friend.users.getUser(real_sender_address).publicKey == null))
                            {
                                requestPubKey(friend, real_sender_address);
                            }
                            else if (replaced_sender_address && !message.verifySignature(friend.users.getUser(real_sender_address).publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                            }
                            else
                            {
                                if (spixi_message.data != null)
                                {
                                    FriendList.setAvatar(sender_address, spixi_message.data, real_sender_address);
                                }
                                else
                                {
                                    FriendList.setAvatar(sender_address, null, real_sender_address);
                                }
                            }
                        }
                        break;

                    case SpixiMessageCode.sentFunds:
                        {
                            // Friend requested funds
                            handleSentFunds(message.id, sender_address, Encoding.UTF8.GetString(spixi_message.data));
                        }
                        break;

                    case SpixiMessageCode.requestFunds:
                        {
                            // Friend requested funds
                            handleRequestFunds(message.id, sender_address, Encoding.UTF8.GetString(spixi_message.data));
                        }
                        break;

                    case SpixiMessageCode.requestFundsResponse:
                        {
                            handleRequestFundsResponse(message.id, sender_address, Encoding.UTF8.GetString(spixi_message.data));
                        }
                        break;

                    case SpixiMessageCode.msgReceived:
                        {
                            handleMsgReceived(sender_address, spixi_message.channel, spixi_message.data);
                            // don't send confirmation back, so just return
                            return;
                        }

                    case SpixiMessageCode.msgRead:
                        {
                            handleMsgRead(sender_address, spixi_message.channel, spixi_message.data);
                            // don't send confirmation back, so just return
                            return;
                        }

                    case SpixiMessageCode.fileHeader:
                        {
                            handleFileHeader(sender_address, spixi_message, message.id);
                        }
                        break;

                    case SpixiMessageCode.acceptFile:
                        {
                            handleAcceptFile(sender_address, spixi_message);
                            break;
                        }

                    case SpixiMessageCode.requestFileData:
                        {
                            handleRequestFileData(sender_address, spixi_message);
                            // don't send confirmation back, so just return
                            return;
                        }

                    case SpixiMessageCode.fileData:
                        {
                            handleFileData(sender_address, spixi_message);
                            // don't send confirmation back, so just return
                            return;
                        }

                    case SpixiMessageCode.fileFullyReceived:
                        {
                            handleFileFullyReceived(sender_address, spixi_message);
                            // don't send confirmation back, so just return
                            return;
                        }

                    case SpixiMessageCode.appData:
                        {
                            // app data received, find the session id of the app and forward the data to it
                            handleAppData(sender_address, spixi_message.data);
                            return;
                        }

                    case SpixiMessageCode.appRequest:
                        {
                            // app request received
                            handleAppRequest(sender_address, message.recipient, spixi_message.data);
                            break;
                        }

                    case SpixiMessageCode.appRequestAccept:
                        {
                            handleAppRequestAccept(sender_address, spixi_message.data);
                            break;
                        }

                    case SpixiMessageCode.appRequestReject:
                        {
                            handleAppRequestReject(sender_address, spixi_message.data);
                            break;
                        }
                    case SpixiMessageCode.appEndSession:
                        {
                            handleAppEndSession(sender_address, spixi_message.data);
                            break;
                        }

                    case SpixiMessageCode.requestAdd:
                        {
                            // Friend request
                            if (!new Address(spixi_message.data).address.SequenceEqual(sender_address) || !message.verifySignature(spixi_message.data))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                            }
                            else
                            {
                                handleRequestAdd(message.id, sender_address, spixi_message.data, message.timestamp);
                            }
                        }
                        break;

                    case SpixiMessageCode.acceptAdd:
                        {
                            // Friend accepted request
                            byte[] pub_k = FriendList.findContactPubkey(friend.walletAddress);
                            if (pub_k == null)
                            {
                                Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                                return;
                            }
                            if (!message.verifySignature(pub_k))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                            }
                            else
                            {
                                if (friend.lastReceivedHandshakeMessageTimestamp < message.timestamp)
                                {
                                    friend.lastReceivedHandshakeMessageTimestamp = message.timestamp;
                                    handleAcceptAdd(sender_address, spixi_message.data);
                                }
                            }
                        }
                        break;

                    case SpixiMessageCode.keys:
                        {
                            if (!message.verifySignature(friend.publicKey))
                            {
                                Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                            }
                            else
                            {
                                if (friend.lastReceivedHandshakeMessageTimestamp < message.timestamp)
                                {
                                    friend.lastReceivedHandshakeMessageTimestamp = message.timestamp;
                                    handleReceivedKeys(sender_address, spixi_message.data);
                                }
                            }
                        }
                        break;

                    case SpixiMessageCode.acceptAddBot:
                        {
                            // Friend accepted request
                            handleAcceptAddBot(sender_address, spixi_message.data);
                        }
                        break;

                    case SpixiMessageCode.botAction:
                        onBotAction(spixi_message.data, friend, channel);
                        break;

                    case SpixiMessageCode.msgDelete:
                        if (friend.bot && !message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                        }
                        else
                        {
                            handleMsgDelete(friend, message.id, spixi_message.data, channel);
                        }
                        break;

                    case SpixiMessageCode.msgReaction:
                        if (friend.bot && !message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(real_sender_address));
                        }
                        else
                        {
                            handleMsgReaction(friend, message.id, spixi_message.data, channel);
                        }
                        break;

                    case SpixiMessageCode.msgTyping:
                        if(friend.bot)
                        {
                            return;
                        }
                        if(friend.chat_page != null)
                        {
                            friend.chat_page.showTyping();
                        }
                        return;
                }

                if (friend == null)
                {
                    friend = FriendList.getFriend(sender_address);
                }

                if (friend == null)
                {
                    Logging.error("Cannot send received confirmation, friend is null");
                    return;
                }
            }catch(Exception e)
            {
                Logging.error("Exception occured in StreamProcessor.receiveData: " + e);
            }

            // Send received confirmation
            StreamMessage msg_received = new StreamMessage();
            msg_received.type = StreamMessageCode.info;
            msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            msg_received.recipient = sender_address;
            msg_received.data = new SpixiMessage(SpixiMessageCode.msgReceived, message.id, channel).getBytes();
            msg_received.transaction = new byte[1];
            msg_received.sigdata = new byte[1];
            msg_received.encryptionType = StreamMessageEncryptionCode.none;

            sendMessage(friend, msg_received, true, true, false, true);
        }

        public static void handleMsgDelete(Friend friend, byte[] msg_id, byte[] msg_id_to_del, int channel)
        {
            if (friend.deleteMessage(msg_id_to_del, channel))
            {
                if (friend.bot)
                {
                    lock (friend.lastReceivedMessageIds)
                    {
                        friend.lastReceivedMessageIds.AddOrReplace(channel, msg_id);
                    }
                    FriendList.saveToStorage();
                }
            }
        }
        public static void handleMsgReaction(Friend friend, byte[] msg_id, byte[] reaction_data, int channel)
        {
            if (friend.addReaction(friend.walletAddress, new SpixiMessageReaction(reaction_data), channel))
            {
                if (friend.bot)
                {
                    lock (friend.lastReceivedMessageIds)
                    {
                        friend.lastReceivedMessageIds.AddOrReplace(channel, msg_id);
                    }
                    FriendList.saveToStorage();
                }
            }
        }

        private static void handlePubKey(byte[] sender_wallet, byte[] pub_key)
        {
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Logging.error("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            if(!friend.bot)
            {
                return;
            }

            byte[] address = new Address(pub_key).address;
            friend.users.setPubKey(address, pub_key);

            Node.localStorage.writeAccountFile();
        }

        // Sends the nickname back to the sender, detects if it should fetch the sender's nickname and fetches it automatically
        private static void handleGetNick(byte[] sender_wallet, string text)
        {
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Logging.error("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            sendNickname(friend);

            return;
        }

        private static void handleGetAvatar(byte[] sender_wallet, string text)
        {
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Logging.error("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            sendAvatar(friend);

            return;
        }

        private static void handleRequestAdd(byte[] id, byte[] sender_wallet, byte[] pub_key, long received_timestamp)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            if (!(new Address(pub_key)).address.SequenceEqual(sender_wallet))
            {
                Logging.error("Received invalid pubkey in handleRequestAdd for {0}", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            Logging.info("In handle request add");

            Friend new_friend = FriendList.addFriend(sender_wallet, pub_key, Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet), null, null, 0, false);

            if (new_friend != null)
            {
                new_friend.lastReceivedHandshakeMessageTimestamp = received_timestamp;
                new_friend.handshakeStatus = 1;
                FriendList.addMessageWithType(id, FriendMessageType.requestAdd, sender_wallet, 0, "");
                requestNickname(new_friend);
            }else
            {
                Friend friend = FriendList.getFriend(sender_wallet);
                if(friend.lastReceivedHandshakeMessageTimestamp >= received_timestamp)
                {
                    return;
                }
                friend.lastReceivedHandshakeMessageTimestamp = received_timestamp;
                bool reset_keys = true;
                if(friend.handshakeStatus > 0 && friend.handshakeStatus < 3)
                {
                    reset_keys = false;
                }
                friend.handshakeStatus = 1;
                if (friend.approved)
                {
                    sendAcceptAdd(friend, reset_keys);
                }
            }
        }

        private static void handleAcceptAdd(byte[] sender_wallet, byte[] aes_key)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Logging.error("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            if (friend.handshakeStatus > 1)
            {
                return;
            }

            Logging.info("In handle accept add");

            friend.aesKey = aes_key;

            friend.generateKeys();

            friend.handshakeStatus = 2;

            friend.sendKeys(2);

            sendNickname(friend);

            sendAvatar(friend);

            FriendList.addMessage(new byte[] { 1 }, friend.walletAddress, 0, string.Format(SpixiLocalization._SL("global-friend-request-accepted"), friend.nickname));
        }

        private static void handleAcceptAddBot(byte[] sender_wallet, byte[] aes_key)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Logging.error("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            if (friend.handshakeStatus > 1)
            {
                return;
            }

            friend.aesKey = aes_key;

            friend.setBotMode();

            friend.handshakeStatus = 3;

            sendNickname(friend);

            sendGetBotInfo(friend);

            FriendList.addMessage(new byte[] { 1 }, friend.walletAddress, 0, string.Format(SpixiLocalization._SL("global-friend-request-accepted"), friend.nickname));
        }


        private static void handleRequestFunds(byte[] id, byte[] sender_wallet, string amount)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                return;
            }

            if (new IxiNumber(amount) > 0)
            {
                FriendList.addMessageWithType(id, FriendMessageType.requestFunds, sender_wallet, 0, amount);
            }
        }

        public static void handleRequestFundsResponse(byte[] id, byte[] sender_wallet, string msg_id_tx_id)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                return;
            }

            string[] msg_id_tx_id_split = msg_id_tx_id.Split(':');
            byte[] msg_id = null;
            string tx_id = null;
            if(msg_id_tx_id_split.Length == 2)
            {
                msg_id = Crypto.stringToHash(msg_id_tx_id_split[0]);
                tx_id = msg_id_tx_id_split[1];
            }else
            {
                msg_id = Crypto.stringToHash(msg_id_tx_id);
            }

            FriendMessage msg = friend.getMessages(0).Find(x => x.id.SequenceEqual(msg_id));
            if(msg == null)
            {
                return;
            }
            string status = SpixiLocalization._SL("chat-payment-status-pending");
            if (tx_id != null)
            {
                msg.message = ":" + tx_id;
            }
            else
            {
                tx_id = "";
                status = SpixiLocalization._SL("chat-payment-status-declined");
                msg.message = "::" + msg.message; // declined
            }

            // Write to chat history
            Node.localStorage.writeMessages(friend.walletAddress, 0, friend.getMessages(0));

            if (friend.chat_page != null)
            {
                friend.chat_page.updateRequestFundsStatus(msg_id, tx_id, status);
            }
        }

        private static void handleSentFunds(byte[] id, byte[] sender_wallet, string txid)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                return;
            }

            FriendList.addMessageWithType(id, FriendMessageType.sentFunds, sender_wallet, 0, txid);
        }

        private static void handleAppData(byte[] sender_address, byte[] app_data_raw)
        {
            // TODO use channels and drop SpixiAppData
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            if (VoIPManager.hasSession(app_data.sessionId))
            {
                VoIPManager.onData(app_data.sessionId, app_data.data);
                return;
            }
            CustomAppPage app_page = Node.customAppManager.getAppPage(app_data.sessionId);
            if(app_page == null)
            {
                Logging.error("App with session id: {0} does not exist.", Crypto.hashToString(app_data.sessionId));
                return;
            }
            app_page.networkDataReceive(sender_address, app_data.data);
        }

        public static void sendAppRequest(Friend friend, string app_id, byte[] session_id, byte[] data)
        {
            // TODO use channels and drop SpixiAppData
            SpixiMessage spixi_msg = new SpixiMessage();
            spixi_msg.type = SpixiMessageCode.appRequest;
            spixi_msg.data = new SpixiAppData(session_id, data, app_id).getBytes();

            StreamMessage new_msg = new StreamMessage();
            new_msg.type = StreamMessageCode.data;
            new_msg.recipient = friend.walletAddress;
            new_msg.sender = Node.walletStorage.getPrimaryAddress();
            new_msg.transaction = new byte[1];
            new_msg.sigdata = new byte[1];
            new_msg.data = spixi_msg.getBytes();

            sendMessage(friend, new_msg);
        }

        public static void sendAppRequestAccept(Friend friend, byte[] session_id, byte[] data = null)
        {
            // TODO use channels and drop SpixiAppData
            SpixiMessage spixi_msg = new SpixiMessage();
            spixi_msg.type = SpixiMessageCode.appRequestAccept;
            spixi_msg.data = new SpixiAppData(session_id, data).getBytes();

            StreamMessage new_msg = new StreamMessage();
            new_msg.type = StreamMessageCode.data;
            new_msg.recipient = friend.walletAddress;
            new_msg.sender = Node.walletStorage.getPrimaryAddress();
            new_msg.transaction = new byte[1];
            new_msg.sigdata = new byte[1];
            new_msg.data = spixi_msg.getBytes();

            sendMessage(friend, new_msg, true, false, false);
        }

        public static void sendAppRequestReject(Friend friend, byte[] session_id, byte[] data = null)
        {
            // TODO use channels and drop SpixiAppData
            SpixiMessage spixi_msg = new SpixiMessage();
            spixi_msg.type = SpixiMessageCode.appRequestReject;
            spixi_msg.data = new SpixiAppData(session_id, data).getBytes();

            StreamMessage new_msg = new StreamMessage();
            new_msg.type = StreamMessageCode.data;
            new_msg.recipient = friend.walletAddress;
            new_msg.sender = Node.walletStorage.getPrimaryAddress();
            new_msg.transaction = new byte[1];
            new_msg.sigdata = new byte[1];
            new_msg.data = spixi_msg.getBytes();

            sendMessage(friend, new_msg);
        }

        public static void sendAppData(Friend friend, byte[] session_id, byte[] data)
        {
            // TODO use channels and drop SpixiAppData
            SpixiMessage spixi_msg = new SpixiMessage();
            spixi_msg.type = SpixiMessageCode.appData;
            spixi_msg.data = (new SpixiAppData(session_id, data)).getBytes();

            StreamMessage msg = new StreamMessage();
            msg.type = StreamMessageCode.data;
            msg.recipient = friend.walletAddress;
            msg.sender = Node.walletStorage.getPrimaryAddress();
            msg.transaction = new byte[1];
            msg.sigdata = new byte[1];
            msg.data = spixi_msg.getBytes();

            sendMessage(friend, msg, false, false, false);
        }

        public static void sendAppEndSession(Friend friend, byte[] session_id, byte[] data = null)
        {
            // TODO use channels and drop SpixiAppData
            SpixiMessage spixi_msg = new SpixiMessage();
            spixi_msg.type = SpixiMessageCode.appEndSession;
            spixi_msg.data = (new SpixiAppData(session_id, data)).getBytes();

            StreamMessage msg = new StreamMessage();
            msg.type = StreamMessageCode.data;
            msg.recipient = friend.walletAddress;
            msg.sender = Node.walletStorage.getPrimaryAddress();
            msg.transaction = new byte[1];
            msg.sigdata = new byte[1];
            msg.data = spixi_msg.getBytes();

            sendMessage(friend, msg, true, true, false);
        }

        private static void handleAppRequest(byte[] sender_address, byte[] recipient_address, byte[] app_data_raw)
        {
            CustomAppManager am = Node.customAppManager;

            Friend friend = FriendList.getFriend(sender_address);
            if (friend == null)
            {
                Logging.error("Received app request from an unknown contact.");
                return;
            }

            if (!Node.walletStorage.isMyAddress(recipient_address))
            {
                return;
            }

            // TODO use channels and drop SpixiAppData
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            
            if(app_data.sessionId == null)
            {
                Logging.error("App session id is null.");
                return;
            }

            CustomAppPage app_page = am.getAppPage(app_data.sessionId);
            if (app_page != null)
            {
                Logging.error("App with session id: {0} already exists.", Crypto.hashToString(app_data.sessionId));
                return;
            }

            string app_id = app_data.appId;

            app_page = am.getAppPage(sender_address, app_id);
            if (app_page != null)
            {
                // TODO, maybe kill the old session and restart instead
                Logging.warn("App with sender: {0} already exists, updating session id.", Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                app_page.sessionId = app_data.sessionId;
                return;
            }
            
            byte[][] user_addresses = new byte[][] { sender_address };
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                CustomApp app = am.getApp(app_id);
                if (app == null)
                {
                    if (app_id == "spixi.voip")
                    {
                        if (!friend.hasMessage(0, app_data.sessionId))
                        {
                            if (VoIPManager.onReceivedCall(friend, app_data.sessionId, app_data.data))
                            {
                                FriendList.addMessageWithType(app_data.sessionId, FriendMessageType.voiceCall, sender_address, 0, "");
                            }
                            Node.refreshAppRequests = true;
                        }
                        return;
                    }else
                    {
                        // app doesn't exist
                        Logging.error("App with id {0} is not installed.", app_id);
                        return;
                    }
                }
                if (FriendList.addMessageWithType(app_data.sessionId, FriendMessageType.appSession, sender_address, 0, app.id) != null)
                {
                    app_page = new CustomAppPage(app_id, sender_address, user_addresses, am.getAppEntryPoint(app_id));
                    app_page.myRequestAddress = recipient_address;
                    app_page.requestedByAddress = sender_address;
                    app_page.sessionId = app_data.sessionId;
                    am.addAppPage(app_page);

                    Node.refreshAppRequests = true;
                }
            });
        }

        private static void handleAppRequestAccept(byte[] sender_address, byte[] app_data_raw)
        {
            // TODO use channels and drop SpixiAppData
            SpixiAppData app_data = new SpixiAppData(app_data_raw);

            if (VoIPManager.hasSession(app_data.sessionId))
            {
                VoIPManager.onAcceptedCall(app_data.sessionId, app_data.data);
                Node.refreshAppRequests = true;
                return;
            }

            CustomAppPage page = Node.customAppManager.getAppPage(app_data.sessionId);
            if(page == null)
            {
                Logging.info("App session does not exist.");
                return;
            }

            page.accepted = true;

            page.appRequestAcceptReceived(sender_address, app_data.data);

            Node.refreshAppRequests = true;
        }

        public static void handleAppRequestReject(byte[] sender_address, byte[] app_data_raw)
        {
            // TODO use channels and drop SpixiAppData
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            byte[] session_id = app_data.sessionId;

            if (VoIPManager.hasSession(session_id))
            {
                VoIPManager.onRejectedCall(session_id);
                Node.refreshAppRequests = true;
                return;
            }

            CustomAppPage page = Node.customAppManager.getAppPage(session_id);
            if (page == null)
            {
                Logging.info("App session does not exist.");
                return;
            }

            page.appRequestRejectReceived(sender_address, app_data.data);

            Node.refreshAppRequests = true;
        }

        public static void handleAppEndSession(byte[] sender_address, byte[] app_data_raw)
        {
            // TODO use channels and drop SpixiAppData
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            byte[] session_id = app_data.sessionId;

            if (VoIPManager.hasSession(session_id))
            {
                VoIPManager.onHangupCall(session_id);
                Node.refreshAppRequests = true;
                return;
            }

            CustomAppPage page = Node.customAppManager.getAppPage(session_id);
            if (page == null)
            {
                Logging.info("App session does not exist.");
                return;
            }

            page.appEndSessionReceived(sender_address, app_data.data);
            Node.refreshAppRequests = true;
        }

        public static void sendAcceptAdd(Friend friend, bool reset_keys)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            if (friend.handshakeStatus > 1)
            {
                return;
            }

            Logging.info("Sending accept add");

            if (reset_keys)
            {
                friend.aesKey = null;
                friend.chachaKey = null;
                friend.generateKeys();
            }

            FriendList.saveToStorage();

            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.acceptAdd, friend.aesKey);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();
            message.encryptionType = StreamMessageEncryptionCode.rsa;
            message.id = new byte[] { 1 };

            message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

            sendMessage(friend, message);

            ProtocolMessage.resubscribeEvents();
        }

        public static void sendNickname(Friend friend)
        {
            SpixiMessage reply_spixi_message = new SpixiMessage(SpixiMessageCode.nick, Encoding.UTF8.GetBytes(Node.localStorage.nickname));

            // Send the nickname message to friend
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();
            reply_message.id = new byte[] { 5 };

            if(friend.bot)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.none;
                reply_message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }
            else if (friend.aesKey == null || friend.chachaKey == null)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.rsa;
                reply_message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }

            sendMessage(friend, reply_message, true, true, false);
        }

        public static void sendAvatar(Friend friend)
        {
            byte[] avatar_bytes = Node.localStorage.getOwnAvatarBytes();

            if (avatar_bytes == null)
            {
                return;
            }

            SpixiMessage reply_spixi_message = new SpixiMessage(SpixiMessageCode.avatar, avatar_bytes);

            // Send the nickname message to friend
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();
            reply_message.id = new byte[] { 6 };

            if (friend.bot)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.none;
                reply_message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }


            sendMessage(friend, reply_message, true, true, false);
        }

        // Requests the nickname of the sender
        public static void requestPubKey(Friend friend, byte[] contact_address)
        {
            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getPubKey, contact_address);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();
            if (friend.bot)
            {
                message.id = new byte[contact_address.Length + 1];
                message.id[0] = 0;
                Array.Copy(contact_address, 0, message.id, 1, contact_address.Length);
            }

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            sendMessage(friend, message, true, true, false);
        }

        // Requests the nickname of the sender
        public static void requestNickname(Friend friend, byte[] contact_address = null)
        {
            if (contact_address == null)
            {
                contact_address = new byte[1];
            }

            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getNick, contact_address);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();
            if (!friend.bot)
            {
                message.id = new byte[] { 3 };
            }
            else
            {
                message.id = new byte[contact_address.Length + 1];
                message.id[0] = 1;
                Array.Copy(contact_address, 0, message.id, 1, contact_address.Length);
            }

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            sendMessage(friend, message, true, true, false);
        }

        // Requests the avatar of the sender
        public static void requestAvatar(Friend friend, byte[] contact_address = null)
        {
            if (contact_address == null)
            {
                contact_address = new byte[1];
            }

            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getAvatar, contact_address);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();
            if (!friend.bot)
            {
                message.id = new byte[] { 4 };
            }else
            {
                message.id = new byte[contact_address.Length + 1];
                message.id[0] = 2;
                Array.Copy(contact_address, 0, message.id, 1, contact_address.Length);
            }

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            sendMessage(friend, message, true, true, false);
        }

        public static void sendContactRequest(Friend friend)
        {
            Logging.info("Sending contact request");


            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.requestAdd, IxianHandler.getWalletStorage().getPrimaryPublicKey());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 0 };

            message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

            sendMessage(friend, message);
        }

        private static void sendGetMessages(Friend friend, int channel, byte[] id)
        {
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botGetMessages, id, channel);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;

            sendMessage(friend, message);
        }

        public static void sendGetBotInfo(Friend friend)
        {
            SpixiBotAction sba = new SpixiBotAction(SpixiBotActionCode.getInfo, null);
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botAction, sba.getBytes());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 11 };

            sendMessage(friend, message);
        }

        public static void sendGetBotChannels(Friend friend)
        {
            SpixiBotAction sba = new SpixiBotAction(SpixiBotActionCode.getChannels, null);
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botAction, sba.getBytes());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 12 };

            sendMessage(friend, message);
        }

        public static void sendGetBotUsers(Friend friend)
        {
            SpixiBotAction sba = new SpixiBotAction(SpixiBotActionCode.getUsers, null);
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botAction, sba.getBytes());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 13 };

            sendMessage(friend, message);
        }

        public static void sendGetBotGroups(Friend friend)
        {
            SpixiBotAction sba = new SpixiBotAction(SpixiBotActionCode.getGroups, null);
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botAction, sba.getBytes());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 14 };

            sendMessage(friend, message);
        }

        public static void deletePendingMessages()
        {
            pendingMessageProcessor.deleteAll();
        }


        public static void onBotAction(byte[] action_data, Friend bot, int channel_id)
        {
            if(!bot.bot)
            {
                Logging.warn("Received onBotAction for a non-bot");
                return;
            }
            SpixiBotAction sba = new SpixiBotAction(action_data);
            switch (sba.action)
            {
                case SpixiBotActionCode.channel:
                    BotChannel channel = new BotChannel(sba.data);
                    bot.channels.setChannel(channel.channelName, channel);
                    byte[] last_msg_id = null;
                    lock (bot.lastReceivedMessageIds)
                    {
                        if (bot.lastReceivedMessageIds.ContainsKey(channel.index))
                        {
                            last_msg_id = bot.lastReceivedMessageIds[channel.index];
                        }
                    }
                    sendGetMessages(bot, channel.index, last_msg_id);
                    break;

                case SpixiBotActionCode.info:
                    BotInfo bi = new BotInfo(sba.data);
                    if(bot.botInfo == null || bi.settingsGeneratedTime != bot.botInfo.settingsGeneratedTime)
                    {
                        bot.botInfo = bi;
                        FriendList.setNickname(bot.walletAddress, bi.serverName, null);
                        Node.localStorage.writeAccountFile();
                        bot.groups.clear();
                        // TODO TODO delete deleted groups locally
                        sendGetBotGroups(bot);
                    }
                    sendGetBotUsers(bot);
                    bot.channels.clear();
                    // TODO TODO delete deleted channels locally
                    sendGetBotChannels(bot);
                    break;

                case SpixiBotActionCode.user:
                    BotContact user = new BotContact(sba.data, false);
                    bot.users.setUser(user);
                    break;

                case SpixiBotActionCode.getPayment:
                    onGetPayment(sba, bot, channel_id);
                    break;
            }
        }

        public static void onGetPayment(SpixiBotAction sba, Friend bot, int channel_id)
        {
            StreamTransactionRequest sta = new StreamTransactionRequest(sba.data);
            FriendMessage fm = bot.getMessages(channel_id).Find(x => x.id.SequenceEqual(sta.messageID));
            if(fm == null)
            {
                Logging.error("Unable to find message with id " + sta.messageID);
                return;
            }

            if (fm.transactionId == "")
            {
                SortedDictionary<byte[], IxiNumber> to_list = new SortedDictionary<byte[], IxiNumber>();

                byte[] from = Node.walletStorage.getPrimaryAddress();
                IxiNumber price = bot.getMessagePrice(fm.payableDataLen);
                if (price > sta.cost)
                {
                    // TODO TODO notify the user somehow
                    Logging.warn("Received payment request for " + Crypto.hashToString(fm.id) + " that has higher than expected amount.");
                    return;
                }

                if(price == 0)
                {
                    Logging.warn("Received payment request for " + Crypto.hashToString(fm.id) + " but requested price is 0.");
                    return;
                }

                to_list.Add(bot.walletAddress, sta.cost);

                IxiNumber fee = ConsensusConfig.transactionPrice;
                byte[] pubKey = Node.walletStorage.getPrimaryPublicKey();

                Transaction tx = new Transaction((int)Transaction.Type.Normal, fee, to_list, from, null, pubKey, IxianHandler.getHighestKnownNetworkBlockHeight());

                IxiNumber total_amount = tx.amount + tx.fee;

                if (Node.balance.balance < total_amount)
                {
                    // TODO TODO notify the user somehow
                    Logging.warn("Received payment request for " + Crypto.hashToString(fm.id) + " but balance is too low.");
                    return;
                }

                StreamTransaction st = new StreamTransaction(fm.id, tx);
                sendBotAction(bot, SpixiBotActionCode.payment, st.getBytes(), channel_id, true);

                fm.transactionId = tx.id;

                Node.localStorage.writeMessages(bot.walletAddress, channel_id, bot.getMessages(channel_id));

                TransactionCache.addUnconfirmedTransaction(tx);
            }
            else
            {
                Transaction tx = TransactionCache.getTransaction(fm.transactionId);
                if (tx == null)
                {
                    tx = TransactionCache.getUnconfirmedTransaction(fm.transactionId);
                }
                // TODO TODO TODO handle expired/failed transaction
                if (tx == null)
                {
                    // TODO TODO TODO do something
                    Logging.warn("Tx " + fm.transactionId + " was already prepared for bot payment but is null now.");
                }
                else
                {
                    IxiNumber total_amount = tx.amount + tx.fee;

                    if (Node.balance.balance < total_amount)
                    {
                        // TODO TODO notify the user somehow
                        Logging.warn("Tx " + fm.transactionId + " was already prepared for bot payment but balance is too low now.");
                        return;
                    }

                    StreamTransaction st = new StreamTransaction(fm.id, tx);
                    sendBotAction(bot, SpixiBotActionCode.payment, st.getBytes(), channel_id, true);
                }
            }
        }

        public static void sendBotAction(Friend bot, SpixiBotActionCode action, byte[] data, int channel = 0, bool sign = false)
        {
            SpixiBotAction sba = new SpixiBotAction(action, data);

            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.botAction, sba.getBytes(), channel);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = bot.walletAddress;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            if (bot.bot)
            {
                message.encryptionType = StreamMessageEncryptionCode.none;
            }

            if(sign)
            {
                message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }

            sendMessage(bot, message);
        }

        public static void sendMsgDelete(Friend friend, byte[] msg_id, int channel = 0)
        {
            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.msgDelete, msg_id, channel);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.data;
            message.recipient = friend.walletAddress;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            if (friend.bot)
            {
                message.encryptionType = StreamMessageEncryptionCode.none;
                message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }

            sendMessage(friend, message);
        }

        public static void sendReaction(Friend friend, byte[] msg_id, string reaction, int channel = 0)
        {
            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.msgReaction, new SpixiMessageReaction(msg_id, reaction).getBytes(), channel);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.data;
            message.recipient = friend.walletAddress;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            if (friend.bot)
            {
                message.encryptionType = StreamMessageEncryptionCode.none;
                message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());
            }

            sendMessage(friend, message);
        }

        public static void sendTyping(Friend friend)
        {
            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.msgTyping, null, 0);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            sendMessage(friend, message, false, false, false, true);
        }
    }
}