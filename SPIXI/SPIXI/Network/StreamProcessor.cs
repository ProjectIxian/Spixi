using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Meta;
using SPIXI.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SPIXI
{
    class StreamProcessor
    {
        static List<StreamMessage> offlineMessages = new List<StreamMessage>(); // List that stores messages until receiving contact is online
        private static Thread offlineMessagesThread; // Thread that checks the offline messages list for outstanding messages
        private static bool continueRunning = false;

        // Initialize the global stream processor
        public static void initialize()
        {
            continueRunning = true;

            // Read the persistent offline messages
            offlineMessages = Node.localStorage.readOfflineMessagesFile();

            offlineMessagesThread = new Thread(streamProcessorLoop);
            offlineMessagesThread.Start();
        }

        // Uninitialize the global stream processor
        public static void uninitialize()
        {
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
            List<StreamMessage> cache = new List<StreamMessage>();

            bool writeToFile = false;
            lock (offlineMessages)
            {
                // Go through each message
                foreach (StreamMessage message in offlineMessages)
                {
                    try
                    {
                        // Extract the public key from the Presence List
                        Friend f = FriendList.getFriend(message.recipient);
                        if (f == null)
                        {
                            continue;
                        }
                        // Send the message
                        if (sendMessage(f, message, false))
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
                foreach (StreamMessage message in cache)
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
                foreach(var friend in FriendList.friends.FindAll(x => x.handshakeStatus < 4 && x.online))
                {
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
                    }
                }
            }
        }

        private static void addOfflineMessage(StreamMessage msg)
        {
            lock(offlineMessages)
            {
                offlineMessages.Add(msg);
                //
                Node.localStorage.writeOfflineMessagesFile(offlineMessages);
            }
        }

        // Send an encrypted message using the S2 network
        public static bool sendMessage(Friend friend, StreamMessage msg, bool add_to_offline_messages = true)
        {
            // TODO this function has to be improved and node's wallet address has to be added


            string hostname = friend.searchForRelay();

            if (friend.publicKey != null && (msg.encryptionType == StreamMessageEncryptionCode.rsa || (friend.aesKey != null && friend.chachaKey != null)))
            {
                msg.encrypt(friend.publicKey, friend.aesKey, friend.chachaKey);
            }else if(msg.encryptionType != StreamMessageEncryptionCode.none || !friend.online)
            {
                if (friend.publicKey == null)
                {
                    byte[] pub_k = FriendList.findContactPubkey(friend.walletAddress);
                    friend.publicKey = pub_k;
                }



                StreamClientManager.connectTo(hostname, null); // TODO replace null with node address
                Console.WriteLine("Could not send message to {0}, due to missing encryption keys, adding to offline queue!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                if (add_to_offline_messages)
                {
                    addOfflineMessage(msg);
                }
                return false;
            }

            if(!StreamClientManager.sendToClient(hostname, ProtocolMessageCode.s2data, msg.getBytes(), Encoding.UTF8.GetBytes(msg.getID())))
            {
                StreamClientManager.connectTo(hostname, null); // TODO replace null with node address
                Console.WriteLine("Could not send message to {0}, adding to offline queue!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                if (add_to_offline_messages)
                {
                    addOfflineMessage(msg);
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
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                friend.handshakeStatus = 3;

                friend.receiveKeys(data);
            }
            else
            {
                // TODO TODO TODO handle this edge case, by displaying request to add notification to user
                Logging.error("Received keys for an unknown friend.");
            }
        }

        // Called when receiving received confirmation from the message recipient
        public static void handleMsgReceived(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);

            Logging.info("Received msg received confirmation for: {0}, data: {1}", Base58Check.Base58CheckEncoding.EncodePlain(sender), Crypto.hashToString(data.id));

            if (friend != null)
            {
                Logging.info("Friend's handshake status is {0}", friend.handshakeStatus);

                if(data.id.SequenceEqual(new byte[] { 0 }))
                {
                    if (friend.handshakeStatus == 0)
                    {
                        friend.handshakeStatus = 1;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
                    return;
                }

                if (data.id.SequenceEqual(new byte[] { 2 }))
                {
                    if (friend.handshakeStatus == 2)
                    {
                        friend.handshakeStatus = 3;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
                    return;
                }

                if (data.id.SequenceEqual(new byte[] { 4 }))
                {
                    if (friend.handshakeStatus == 3)
                    {
                        friend.handshakeStatus = 4;
                        Logging.info("Set handshake status to {0}", friend.handshakeStatus);
                    }
                    return;
                }

                friend.setMessageReceived(data.id);
            }
            else
            {
                Logging.error("Received Message received confirmation for an unknown friend.");
            }
        }

        // Called when receiving read confirmation from the message recipient
        public static void handleMsgRead(byte[] sender, SpixiMessage data)
        {
            Friend friend = FriendList.getFriend(sender);
            if (friend != null)
            {
                friend.setMessageRead(data.id);
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

            Logging.info("Received S2 data from {0} for {1}", Base58Check.Base58CheckEncoding.EncodePlain(message.sender), Base58Check.Base58CheckEncoding.EncodePlain(message.recipient));

            Friend friend = null;

            // decrypt the message if necessary
            // TODO TODO TODO add message receive queue for when the keys aren't available yet
            if (message.encryptionType != StreamMessageEncryptionCode.none)
            {
                byte[] aes_key = null;
                byte[] chacha_key = null;

                friend = FriendList.getFriend(message.sender);
                if(friend != null)
                {
                    aes_key = friend.aesKey;
                    chacha_key = friend.chachaKey;
                }
                if(!message.decrypt(Node.walletStorage.getPrimaryPrivateKey(), aes_key, chacha_key))
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
                        // Add the message to the friend list
                        FriendList.addMessage(spixi_message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data));
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
                        if (spixi_message.data != null)
                        {
                            FriendList.setNickname(message.sender, Encoding.UTF8.GetString(spixi_message.data));
                        }else
                        {
                            FriendList.setNickname(message.sender, Base58Check.Base58CheckEncoding.EncodePlain(message.sender));
                        }
                    }
                    break;

                case SpixiMessageCode.requestAdd:
                    {
                        // Friend request
                        handleRequestAdd(spixi_message.id, message.sender, spixi_message.data);
                    }
                    break;

                case SpixiMessageCode.acceptAdd:
                    {
                        // Friend accepted request
                        handleAcceptAdd(message.sender, spixi_message.data);
                    }
                    break;

                case SpixiMessageCode.requestFunds:
                    {
                        // Friend requested funds
                        handleRequestFunds(spixi_message.id, message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.keys:
                    {
                        handleReceivedKeys(message.sender, spixi_message.data);
                    }
                    break;

                case SpixiMessageCode.msgReceived:
                    {
                        handleMsgReceived(message.sender, spixi_message);
                        // don't send confirmation back, so just return
                        return;
                    }

                case SpixiMessageCode.msgRead:
                    {
                        handleMsgRead(message.sender, spixi_message);
                        // don't send confirmation back, so just return
                        return;
                    }
            }

            if(friend == null)
            {
                friend = FriendList.getFriend(message.sender);
            }

            if(friend == null)
            {
                Logging.error("Cannot send received confirmation, friend is null");
            }

            // Send received confirmation
            StreamMessage msg_received = new StreamMessage();
            msg_received.type = StreamMessageCode.info;
            msg_received.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            msg_received.recipient = message.sender;
            msg_received.data = new SpixiMessage(spixi_message.id, SpixiMessageCode.msgReceived, null).getBytes();
            msg_received.transaction = new byte[1];
            msg_received.sigdata = new byte[1];
            msg_received.encryptionType = StreamMessageEncryptionCode.none;

            sendMessage(friend, msg_received, true);
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

        private static void handleRequestAdd(byte[] id, byte[] sender_wallet, byte[] pub_key)
        {
            if(!(new Address(pub_key)).address.SequenceEqual(sender_wallet))
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
            }else
            {
                friend.aesKey = aes_key;
            }

            friend.handshakeStatus = 2;

            friend.generateKeys();

            friend.sendKeys(2);

            requestNickname(friend);

            sendNickname(friend);

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

            FriendList.addMessageWithType(id, FriendMessageType.requestFunds, sender_wallet, amount);
        }

        public static void sendAcceptAdd(Friend friend)
        {
            if(friend.handshakeStatus > 1)
            {
                return;
            }

            friend.aesKey = null;
            friend.chachaKey = null;

            friend.generateKeys();

            SpixiMessage spixi_message = new SpixiMessage(new byte[] { 1 }, SpixiMessageCode.acceptAdd, friend.aesKey);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();
            message.encryptionType = StreamMessageEncryptionCode.rsa;

            StreamProcessor.sendMessage(friend, message);

            ProtocolMessage.resubscribeEvents();
        }

        public static void sendNickname(Friend friend)
        {
            if (friend.handshakeStatus == 4)
            {
                friend.handshakeStatus = 3;
            }

            SpixiMessage reply_spixi_message = new SpixiMessage(new byte[] { 4 }, SpixiMessageCode.nick, Encoding.UTF8.GetBytes(Node.localStorage.nickname));

            // Send the nickname message to friend
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();

            if(friend.aesKey == null || friend.chachaKey == null)
            {
                reply_message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            StreamProcessor.sendMessage(friend, reply_message);
        }

        // Requests the nickname of the sender
        public static void requestNickname(Friend friend)
        {
            // Prepare the message and send to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(new byte[] { 3 }, SpixiMessageCode.getNick, new byte[1]);

            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.recipient = friend.walletAddress;
            message.sender = Node.walletStorage.getPrimaryAddress();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.data = spixi_message.getBytes();

            if (friend.aesKey == null || friend.chachaKey == null)
            {
                message.encryptionType = StreamMessageEncryptionCode.rsa;
            }

            StreamProcessor.sendMessage(friend, message);
        }

        public static void sendContactRequest(Friend friend)
        {
            // Send the message to the S2 nodes
            SpixiMessage spixi_message = new SpixiMessage(new byte[] { 0 }, SpixiMessageCode.requestAdd, IxianHandler.getWalletStorage().getPrimaryPublicKey());


            StreamMessage message = new StreamMessage();
            message.type = StreamMessageCode.info;
            message.sender = IxianHandler.getWalletStorage().getPrimaryAddress();
            message.recipient = friend.walletAddress;
            message.data = spixi_message.getBytes();
            message.transaction = new byte[1];
            message.sigdata = new byte[1];
            message.encryptionType = StreamMessageEncryptionCode.none;

            StreamProcessor.sendMessage(friend, message);
        }
    }
}