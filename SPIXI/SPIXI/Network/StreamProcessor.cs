using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Meta;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SPIXI
{
    class OfflineMessage
    {
        public StreamMessage message = null;
        public bool sendPushNotification = false;
    }
    
    class StreamProcessor
    {
        static List<OfflineMessage> offlineMessages = new List<OfflineMessage>(); // List that stores messages until receiving contact is online
        private static Thread offlineMessagesThread; // Thread that checks the offline messages list for outstanding messages
        private static bool continueRunning = false;

        private static Dictionary<byte[], OfflineMessage> pendingMessages = new Dictionary<byte[], OfflineMessage>(new ByteArrayComparer()); // List of pending messages that might need to be resent

        private static bool running = false;

        // Initialize the global stream processor
        public static void initialize()
        {
            if (running)
            {
                return;
            }

            running = true;

            continueRunning = true;

            // Read the persistent offline messages
            offlineMessages = Node.localStorage.readOfflineMessagesFile();

            offlineMessagesThread = new Thread(streamProcessorLoop);
            offlineMessagesThread.Start();
        }

        // Uninitialize the global stream processor
        public static void uninitialize()
        {
            running = false;
            continueRunning = false;
        }

        private static void streamProcessorLoop()
        {
            // Only check for offline messages when the loop is active
            while (continueRunning)
            {
                try
                {
                    sendOfflineMessages();
                    sendPendingRequests();
                    sendPendingMessages();
                }catch(Exception e)
                {
                    Logging.error("Unknown exception occured in streamProcessorLoop: " + e);
                }

                // Wait 5 seconds before next round
                Thread.Sleep(5000);
            }
        }

        // Thread for checking offline message queue
        private static void sendOfflineMessages()
        {
            List<OfflineMessage> cache = new List<OfflineMessage>();

            bool writeToFile = false;
            lock (offlineMessages)
            {
                if (offlineMessages.Count > 0)
                {
                    Logging.info("Sending {0} offline messages", offlineMessages.Count);
                }
                // Go through each message
                foreach (OfflineMessage message in offlineMessages)
                {
                    try
                    {
                        // Extract the public key from the Presence List
                        Friend f = FriendList.getFriend(message.message.recipient);
                        if (f == null)
                        {
                            cache.Add(message);
                            continue;
                        }
                        // Send the message
                        if (sendMessage(f, message.message, false, message.sendPushNotification))
                        {
                            // Add the message to the removal cache
                            cache.Add(message);
                        }
                    }catch(Exception e)
                    {
                        Logging.error("Exception occured while trying to send offline message {0}", e);
                    }

                }

                // Check the removal cache for messages
                foreach (OfflineMessage message in cache)
                {
                    writeToFile = true;
                    offlineMessages.Remove(message);
                }

            }

            // Finally, clear the removal cache
            cache.Clear();

            // Save changes to the offline messages file
            if (writeToFile)
            {
                Node.localStorage.writeOfflineMessagesFile(offlineMessages);
            }
        }

        private static void sendPendingRequests()
        {
            lock(FriendList.friends)
            {
                List<Friend> friend_list = new List<Friend>();
                if(Config.enablePushNotifications)
                {
                    friend_list = FriendList.friends.FindAll(x => x.handshakeStatus < 5);
                }
                else
                {
                    friend_list = FriendList.friends.FindAll(x => x.handshakeStatus < 5 && x.online);
                }
                foreach (var friend in friend_list)
                {
                    if(friend.handshakePushed)
                    {
                        continue;
                    }
                    switch(friend.handshakeStatus)
                    {
                        // Add friend request has been sent but no confirmation has been received
                        case 0:
                            if(friend.approved)
                            {
                                Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                                sendContactRequest(friend);
                            }
                            break;

                        // Request has been accepted but no confirmation received, resend acceptance
                        case 1:
                            if(friend.approved && friend.aesKey != null)
                            {
                                Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                                sendAcceptAdd(friend);
                            }
                            break;

                        // Acceptance has been received and the second encryption key was sent but no confirmation received, resend second key
                        case 2:
                            Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                            friend.sendKeys(2);
                            break;

                        // Nickname confirmation hasn't been received
                        case 3:
                            Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                            sendNickname(friend);
                            break;

                        // Avatar confirmation hasn't been received
                        case 4:
                            Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                            sendAvatar(friend);
                            break;
                    }
                }
            }
        }

        private static void sendPendingMessages()
        {
            List<OfflineMessage> cache = new List<OfflineMessage>();

            lock (pendingMessages)
            {
                if (pendingMessages.Count > 0)
                {
                    Logging.info("Sending {0} pending messages", pendingMessages.Count);
                }
                // Go through each message
                foreach (var entry in pendingMessages)
                {
                    try
                    {
                        OfflineMessage message = entry.Value;
                        if(message.message.timestamp + 5 < Clock.getTimestamp())
                        {
                            cache.Add(message);
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.error("Exception occured while trying to send pending message {0}", e);
                    }

                }

                // Check the removal cache for messages
                foreach (OfflineMessage message in cache)
                {
                    pendingMessages.Remove(message.message.id);
                    Friend f = FriendList.getFriend(message.message.recipient);
                    if (f == null)
                    {
                        continue;
                    }
                    sendMessage(f, message.message, true, message.sendPushNotification);
                }

            }

            // Finally, clear the removal cache
            cache.Clear();
        }

        private static void addOfflineMessage(StreamMessage msg, bool store_to_server, bool send_push_notification)
        {
            lock(pendingMessages)
            {
                if(pendingMessages.ContainsKey(msg.id))
                {
                    pendingMessages.Remove(msg.id);
                }
            }

            if (store_to_server)
            {
                if (OfflinePushMessages.sendPushMessage(msg, send_push_notification))
                {
                    return;
                }
            }

            // Use offline queue when notifications are disabled or when we don't have enough data yet
            lock(offlineMessages)
            {
                if(offlineMessages.Find(x => x.message.id.SequenceEqual(msg.id)) != null)
                {
                    Logging.info("Message already exists in the offline queue, not adding.");
                    return;
                }

                offlineMessages.Add(new OfflineMessage() { message = msg, sendPushNotification = send_push_notification });
                //
                Node.localStorage.writeOfflineMessagesFile(offlineMessages);
            }
        }

        // Send an encrypted message using the S2 network
        public static bool sendMessage(Friend friend, StreamMessage msg, bool add_to_offline_messages = true, bool push = true, bool add_to_pending_messages = true)
        {
            // TODO this function has to be improved and node's wallet address has to be added
            if ((friend.publicKey != null && msg.encryptionType == StreamMessageEncryptionCode.rsa) || (friend.aesKey != null && friend.chachaKey != null))
            {
                if(msg.encryptionType == StreamMessageEncryptionCode.none)
                {
                    // upgrade encryption type
                    msg.encryptionType = StreamMessageEncryptionCode.spixi1;
                }
                msg.encrypt(friend.publicKey, friend.aesKey, friend.chachaKey);
            }else if(msg.encryptionType != StreamMessageEncryptionCode.none)
            {
                if (friend.publicKey == null)
                {
                    byte[] pub_k = FriendList.findContactPubkey(friend.walletAddress);
                    friend.publicKey = pub_k;
                }

                Logging.warn("Could not send message to {0}, due to missing encryption keys, adding to offline queue!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                if (add_to_offline_messages)
                {
                    addOfflineMessage(msg, false, push);
                }
                return false;
            }

            if (add_to_pending_messages)
            {
                lock (pendingMessages)
                {
                    pendingMessages.AddOrReplace(msg.id, new OfflineMessage() { message = msg, sendPushNotification = push });
                }
            }

            string hostname = friend.searchForRelay();

            if (!friend.online || !StreamClientManager.sendToClient(hostname, ProtocolMessageCode.s2data, msg.getBytes(), msg.id))
            {
                if (hostname != "")
                {
                    StreamClientManager.connectTo(hostname, null); // TODO replace null with node address
                }
                Logging.warn("Could not send message to {0}, adding to offline queue!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                if (add_to_offline_messages || Config.enablePushNotifications)
                {
                    if (friend.bot)
                    {
                        push = false;
                    }
                    addOfflineMessage(msg, Config.enablePushNotifications, push);
                }
                return false;
            }

            return true;

            /*         string pub_k = FriendList.findContactPubkey(msg.recipientAddress);
                     if (pub_k.Length < 1)
                     {
                         Console.WriteLine("Contact {0} not found, adding to offline queue!", msg.recipientAddress);
                         addOfflineMessage(msg);
                         return;
                     }


                     // Create a new IXIAN transaction
                     //  byte[] checksum = Crypto.sha256(encrypted_message);
                     Transaction transaction = new Transaction(0, msg.recipientAddress, Node.walletStorage.address);
                     //  transaction.data = Encoding.UTF8.GetString(checksum);
                     msg.transactionID = transaction.id;
                     //ProtocolMessage.broadcastProtocolMessage(ProtocolMessageCode.newTransaction, transaction.getBytes());

                     // Add message to the queue
                     messages.Add(msg);

                     // Request a new keypair from the S2 Node
                     if(hostname == null)
                         ProtocolMessage.broadcastProtocolMessage(ProtocolMessageCode.s2generateKeys, Encoding.UTF8.GetBytes(msg.getID()));
                     else
                     {
                         NetworkClientManager.sendData(ProtocolMessageCode.s2generateKeys, Encoding.UTF8.GetBytes(msg.getID()), hostname);
                     }*/
        }


        // Called when receiving encryption keys from the S2 node
        public static void handleReceivedKeys(byte[] sender, byte[] data)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                friend.receiveKeys(data);

                friend.handshakeStatus = 3;
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
                // TODO it would be better if this was handled by acceptFile
                FileTransfer transfer = TransferManager.prepareIncomingFileTransfer(data.data, sender);
                // END OF TODO

                string message_data = string.Format("{0}:{1}", transfer.uid, transfer.fileName);
                FriendMessage fm = FriendList.addMessageWithType(message_id, FriendMessageType.fileHeader, sender, message_data);
                fm.transferId = transfer.uid;
                fm.filePath = transfer.fileName;
                fm.fileSize = transfer.fileSize;
                Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);
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
        public static void handleMsgReceived(byte[] sender, byte[] msg_id)
        {
            Friend friend = FriendList.getFriend(sender);

            if (friend != null)
            {
                lock (pendingMessages)
                {
                    if (pendingMessages.ContainsKey(msg_id))
                    {
                        pendingMessages.Remove(msg_id);
                    }
                }

                Logging.info("Friend's handshake status is {0}", friend.handshakeStatus);

                if(msg_id.SequenceEqual(new byte[] { 0 }))
                {
                    if (friend.handshakeStatus == 0)
                    {
                        friend.handshakeStatus = 1;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
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

                if (msg_id.SequenceEqual(new byte[] { 4 }))
                {
                    if (friend.handshakeStatus == 3)
                    {
                        friend.handshakeStatus = 4;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
                    return;
                }

                if (msg_id.SequenceEqual(new byte[] { 5 }))
                {
                    if (friend.handshakeStatus == 4)
                    {
                        friend.handshakeStatus = 5;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
                    return;
                }

                friend.setMessageReceived(msg_id);
            }
            else
            {
                Logging.error("Received Message received confirmation for an unknown friend.");
            }
        }

        // Called when receiving read confirmation from the message recipient
        public static void handleMsgRead(byte[] sender, byte[] msg_id)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                friend.setMessageRead(msg_id);
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
            if(message.data == null)
            {
                Logging.error(string.Format("Null message data."));
                return;
            }

            byte[] sender_address = message.sender;

            if (endpoint != null)
            {
                if (endpoint.presence.wallet.SequenceEqual(message.recipient))
                {
                    // message from a bot group chat
                    sender_address = message.sender;
                    message.sender = message.recipient;
                }
            }

            Logging.info("Received S2 data from {0} for {1}", Base58Check.Base58CheckEncoding.EncodePlain(message.sender), Base58Check.Base58CheckEncoding.EncodePlain(message.recipient));

            byte[] aes_key = null;
            byte[] chacha_key = null;

            Friend friend = FriendList.getFriend(message.sender);
            if(friend != null)
            {
                aes_key = friend.aesKey;
                chacha_key = friend.chachaKey;
            }

            if(message.type == StreamMessageCode.error)
            {
                PresenceList.removeAddressEntry(friend.walletAddress);
                friend.online = false;
                lock (pendingMessages)
                {
                    if (pendingMessages.ContainsKey(message.data))
                    {
                        OfflineMessage om = pendingMessages[message.data];
                        StreamMessage sm = om.message;
                        pendingMessages.Remove(sm.id);
                        sendMessage(friend, sm, true, om.sendPushNotification);
                    }
                }
                return;
            }

            // decrypt the message if necessary
            // TODO TODO TODO add message receive queue for when the keys aren't available yet
            // TODO TODO TODO prevent encryption type downgrades
            if (message.encryptionType != StreamMessageEncryptionCode.none)
            {
                if (!message.decrypt(Node.walletStorage.getPrimaryPrivateKey(), aes_key, chacha_key))
                {
                    Logging.error("Could not decrypt message from {0}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                    return;
                }
            }

            // Extract the Spixi message
            SpixiMessage spixi_message = new SpixiMessage(message.data);

            switch(spixi_message.type)
            {
                case SpixiMessageCode.chat:
                    {
                        if(sender_address == null)
                        {
                            Logging.info("Sender address is null");
                        }
                        if (friend.bot && !message.verifySignature(spixi_message.data))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            // no need to verify sigs, as we're communicating using secure keys

                            // Add the message to the friend list
                            FriendList.addMessage(message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data), sender_address, message.timestamp);
                        }
                    }
                    break;

                case SpixiMessageCode.getNick:
                    {
                        // Send the nickname to the sender as requested
                        handleGetNick(message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.nick:
                    {
                        // Set the nickname for the corresponding address
                        if (!message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            if (spixi_message.data != null)
                            {
                                FriendList.setNickname(message.sender, Encoding.UTF8.GetString(spixi_message.data), sender_address);
                            }else
                            {
                                FriendList.setNickname(message.sender, Base58Check.Base58CheckEncoding.EncodePlain(message.sender), sender_address);
                            }
                        }
                    }
                    break;

                case SpixiMessageCode.getAvatar:
                    {
                        // Send the avatar to the sender as requested
                        handleGetAvatar(message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.avatar:
                    {
                        // Set the avatar for the corresponding address
                        if (!message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            if (spixi_message.data != null)
                            {
                                FriendList.setAvatar(message.sender, spixi_message.data, sender_address);
                            }
                            else
                            {
                                FriendList.setAvatar(message.sender, null, sender_address);
                            }
                        }
                    }
                    break;
                case SpixiMessageCode.requestAdd:
                    {
                        // Friend request
                        if (!message.verifySignature(spixi_message.data))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            handleRequestAdd(message.id, message.sender, spixi_message.data);
                        }
                    }
                    break;

                case SpixiMessageCode.acceptAdd:
                    {
                        // Friend accepted request
                        if (!message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            handleAcceptAdd(message.sender, spixi_message.data);
                        }
                    }
                    break;

                case SpixiMessageCode.sentFunds:
                    {
                        // Friend requested funds
                        handleSentFunds(message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.requestFunds:
                    {
                        // Friend requested funds
                        handleRequestFunds(message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.requestFundsResponse:
                    {
                        handleRequestFundsResponse(message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.keys:
                    {
                        if (!message.verifySignature(friend.publicKey))
                        {
                            Logging.error("Unable to verify signature for message type: {0}, id: {1}, from: {2}.", message.type, Crypto.hashToString(message.id), Base58Check.Base58CheckEncoding.EncodePlain(sender_address));
                        }
                        else
                        {
                            handleReceivedKeys(message.sender, spixi_message.data);
                        }
                    }
                    break;

                case SpixiMessageCode.msgReceived:
                    {
                        handleMsgReceived(message.sender, spixi_message.data);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.msgRead:
                    {
                        handleMsgRead(message.sender, spixi_message.data);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.fileHeader:
                    {
                        handleFileHeader(message.sender, spixi_message, message.id);
                    }
                    break;

                case SpixiMessageCode.acceptFile:
                    {
                        handleAcceptFile(message.sender, spixi_message);
                        break;
                    }

                case SpixiMessageCode.requestFileData:
                    {
                        handleRequestFileData(message.sender, spixi_message);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.fileData:
                    {
                        handleFileData(message.sender, spixi_message);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.fileFullyReceived:
                    {
                        handleFileFullyReceived(message.sender, spixi_message);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.acceptAddBot:
                    {
                        // Friend accepted request
                        handleAcceptAddBot(message.sender, spixi_message.data);
                    }
                    break;

                case SpixiMessageCode.appData:
                    {
                        // app data received, find the session id of the app and forward the data to it
                        handleAppData(message.sender, spixi_message.data);
                    }
                    break;

                case SpixiMessageCode.appRequest:
                    {
                        // app request received
                        handleAppRequest(message.sender, spixi_message.data);
                        break;
                    }
            }

            if (friend == null)
            {
                friend = FriendList.getFriend(message.sender);
            }

            if(friend == null)
            {
                Logging.error("Cannot send received confirmation, friend is null");
                return;
            }

            // Send received confirmation
            StreamMessage msg_received = new StreamMessage();
            msg_received.type = StreamMessageCode.info;
            msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            msg_received.recipient = message.sender;
            msg_received.data = new SpixiMessage(SpixiMessageCode.msgReceived, message.id).getBytes();
            msg_received.transaction = new byte[1];
            msg_received.sigdata = new byte[1];
            msg_received.encryptionType = StreamMessageEncryptionCode.none;

            sendMessage(friend, msg_received, true, false, false);
        }

        // Sends the nickname back to the sender, detects if it should fetch the sender's nickname and fetches it automatically
        private static void handleGetNick(byte[] sender_wallet, string text)
        {
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
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
                Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            sendAvatar(friend);

            return;
        }

        private static void handleRequestAdd(byte[] id, byte[] sender_wallet, byte[] pub_key)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            if (!(new Address(pub_key)).address.SequenceEqual(sender_wallet))
            {
                Logging.error("Received invalid pubkey in handleRequestAdd for {0}", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                return;
            }

            Friend new_friend = FriendList.addFriend(sender_wallet, pub_key, Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet), null, null, 0, false);

            if (new_friend != null)
            {
                new_friend.handshakeStatus = 1;
                FriendList.addMessageWithType(id, FriendMessageType.requestAdd, sender_wallet, "");
                requestNickname(new_friend);
            }else
            {
                Friend friend = FriendList.getFriend(sender_wallet);
                friend.handshakeStatus = 1;
                if (friend.approved)
                {
                    sendAcceptAdd(friend);
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
                byte[] pub_k = FriendList.findContactPubkey(sender_wallet);
                if (pub_k == null)
                {
                    Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                    return;
                }

                friend = FriendList.addFriend(sender_wallet, pub_k, Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet), aes_key, null, 0);
            }
            else
            {
                friend.aesKey = aes_key;
            }

            friend.generateKeys();

            friend.handshakeStatus = 2;

            friend.sendKeys(2);

            requestNickname(friend);

            sendNickname(friend);

            FriendList.addMessage(null, friend.walletAddress, friend.nickname + " has accepted your friend request.");
        }

        private static void handleAcceptAddBot(byte[] sender_wallet, byte[] aes_key)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                byte[] pub_k = FriendList.findContactPubkey(sender_wallet);
                if (pub_k == null)
                {
                    Logging.warn("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));
                    return;
                }

                friend = FriendList.addFriend(sender_wallet, pub_k, Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet), aes_key, null, 0);
            }
            else
            {
                friend.aesKey = aes_key;
            }

            friend.bot = true;

            friend.handshakeStatus = 3;

            sendGetMessages(friend);

            requestNickname(friend);

            //sendNickname(friend);

            FriendList.addMessage(null, friend.walletAddress, friend.nickname + " has accepted your friend request.");
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
                FriendList.addMessageWithType(id, FriendMessageType.requestFunds, sender_wallet, amount);
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

            FriendMessage msg = friend.messages.Find(x => x.id.SequenceEqual(msg_id));
            if(msg == null)
            {
                return;
            }
            string status = "PENDING";
            if (tx_id != null)
            {
                msg.message = ":" + tx_id;
            }
            else
            {
                tx_id = "";
                status = "DECLINED";
                msg.message = "::" + msg.message; // declined
            }

            // Write to chat history
            Node.localStorage.writeMessagesFile(friend.walletAddress, friend.messages);

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

            FriendList.addMessageWithType(id, FriendMessageType.sentFunds, sender_wallet, txid);
        }

        private static void handleAppData(byte[] sender_address, byte[] app_data_raw)
        {
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            CustomAppPage app_page = FriendList.getAppPage(app_data.sessionId);
            if(app_page == null)
            {
                Logging.error("App with session id: {0} does not exist.", Crypto.hashToString(app_data.sessionId));
                return;
            }
            app_page.networkDataReceive(sender_address, app_data.data);
        }

        private static void handleAppRequest(byte[] sender_address, byte[] app_data_raw)
        {
            SpixiAppData app_data = new SpixiAppData(app_data_raw);
            CustomAppPage app_page = FriendList.getAppPage(app_data.sessionId);
            if (app_page != null)
            {
                Logging.error("App with session id: {0} already exists.", Crypto.hashToString(app_data.sessionId));
                return;
            }
            byte[][] user_addresses = new byte[][] { sender_address };
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                CustomAppPage page = new CustomAppPage(sender_address, user_addresses, "custom_app.html");
                page.sessionId = app_data.sessionId;
                Xamarin.Forms.Application.Current.MainPage.Navigation.PushAsync(page, Config.defaultXamarinAnimations);
            });
        }

        public static void sendAcceptAdd(Friend friend)
        {
            // TODO TODO secure this function to prevent "downgrade"; possibly other handshake functions need securing

            if (friend.handshakeStatus > 1)
            {
                return;
            }

            friend.aesKey = null;
            friend.chachaKey = null;
            friend.generateKeys();

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

            StreamProcessor.sendMessage(friend, message);

            ProtocolMessage.resubscribeEvents();
        }

        public static void sendNickname(Friend friend)
        {
            if (friend.handshakeStatus == 4)
            {
                friend.handshakeStatus = 3;
            }

            SpixiMessage reply_spixi_message = new SpixiMessage(SpixiMessageCode.nick, Encoding.UTF8.GetBytes(Node.localStorage.nickname));

            // Send the nickname message to friend
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();
            reply_message.id = new byte[] { 4 };

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            reply_message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

            StreamProcessor.sendMessage(friend, reply_message);
        }

        public static void sendAvatar(Friend friend)
        {
            byte[] avatar_bytes = Node.localStorage.getOwnAvatarBytes();

            if (avatar_bytes == null)
            {
                friend.handshakeStatus = 5;
                return;
            }

            if (friend.handshakeStatus == 5)
            {
                friend.handshakeStatus = 4;
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
            reply_message.id = new byte[] { 5 };

            /*if (friend.aesKey == null || friend.chachaKey == null)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.rsa;
            }*/

            reply_message.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

            StreamProcessor.sendMessage(friend, reply_message);
        }

        // Requests the nickname of the sender
        public static void requestNickname(Friend friend, byte[] contact_address = null)
        {
            if(contact_address == null)
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
            message.id = new byte[] { 3 };

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            StreamProcessor.sendMessage(friend, message);
        }

        public static void sendContactRequest(Friend friend)
        {
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

            StreamProcessor.sendMessage(friend, message);
        }

        public static void sendGetMessages(Friend friend)
        {
            byte[] last_message_id = null;

            if(friend.messages.Count > 0)
            {
                last_message_id = friend.messages[friend.messages.Count - 1].id;
            }

            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getMessages, last_message_id);


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;
            message.id = new byte[] { 10 };

            StreamProcessor.sendMessage(friend, message);
        }
    }
}