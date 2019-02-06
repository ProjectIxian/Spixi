using DLT;
using DLT.Meta;
using DLT.Network;
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
        static List<StreamMessage> messages = new List<StreamMessage>(); // List that stores messages until the keypair is generated

        static List<StreamMessage> offlineMessages = new List<StreamMessage>(); // List that stores messages until receiving contact is online
        private static Thread offlineMessagesThread; // Thread that checks the offline messages list for outstanding messages
        private static bool continueRunning = false;

        // Initialize the global stream processor
        public static void initialize()
        {
            continueRunning = true;
            offlineMessagesThread = new Thread(offlineLoop);
            offlineMessagesThread.Start();
        }

        // Uninitialize the global stream processor
        public static void uninitialize()
        {
            continueRunning = false;
        }

        // Thread for checking offline message queue
        private static void offlineLoop()
        {
            List<StreamMessage> cache = new List<StreamMessage>();

            // Read the persistent offline messages
            offlineMessages = Node.localStorage.readOfflineMessagesFile();

            // Only check for offline messages when the loop is active
            while (continueRunning)
            {
                bool writeToFile = false;
                lock (offlineMessages)
                {
                    // Go through each message
                    foreach (StreamMessage message in offlineMessages)
                    {
                        // Extract the public key from the Presence List
                        byte[] pub_k = FriendList.findContactPubkey(message.recipient);
                        if (pub_k == null)
                        {
                            // No public key found means the contact is still offline
                            continue;
                        }
                        // Send the message
                        sendMessage(message);
                        // Add the message to the removal cache
                        cache.Add(message);
                    }

                    // Check the removal cache for messages
                    foreach (StreamMessage message in cache)
                    {
                        writeToFile = true;
                        offlineMessages.Remove(message);
                    }

                    // Finally, clear the removal cache
                    cache.Clear();
                }

                // Save changes to the offline messages file
                if(writeToFile)
                {
                    Node.localStorage.writeOfflineMessagesFile(offlineMessages);
                }

                // Wait 5 seconds before next round
                Thread.Sleep(5000);
            }
            Thread.Yield();
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
        public static void sendMessage(StreamMessage msg, string hostname = null)
        {

            StreamClientManager.broadcastData(ProtocolMessageCode.s2data, msg.getBytes());

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

        // Called when receiving an encryption key from the S2 node
        public static void receivedKeys(byte[] data, Socket socket)
        {
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    string messageID = reader.ReadString();
                    string publicKey = reader.ReadString();

                    lock (messages)
                    {
                        StreamMessage tmp_message = null;
                        foreach (StreamMessage message in messages)
                        {
                            if (message.getID().Equals(messageID, StringComparison.Ordinal))
                            {
                                // Encrypt and send the message
                                sendEncryptedMessage(message, publicKey, socket);
                                tmp_message = message;
                                break;
                            }
                        }
                        // Remove this message from the message queue
                        messages.Remove(tmp_message);
                    }
                }
            }
        }

        // Called when an encryption key is received from the S2 server, as per step 4 of the WhitePaper
        private static void sendEncryptedMessage(StreamMessage msg, string key, Socket socket)
        {
 /*           Console.WriteLine("Sending encrypted message with key {0}", key);

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
            }*/
        }

        // Prepare a Spixi S2 message. Encrypts the provided text combined with a SpixiMessageCode using the provided publicKey
        public static byte[] prepareSpixiMessage(StreamMessageCode code, string text, byte[] publicKey)
        {
            Logging.info(string.Format("PREP: {0} : {1}", (int)code, text));
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    // NOTE: storing a 0 value integer as the initial part of the message will result in decryption errors
                    // on the other client. As such, we add a dummy offset of 23
                    int safe_code = (int)code + 23;
                    writer.Write(safe_code);
                    writer.Write(text);

                    byte[] encrypted_message = CryptoManager.lib.encryptWithRSA(m.ToArray(), publicKey);

                    return encrypted_message;
                }
            }
        }

        // Called when receiving S2 data from clients
        public static void receiveData(byte[] bytes, RemoteEndpoint endpoint)
        {
            Logging.info(string.Format("Receiving S2 data "));

            StreamMessage message = new StreamMessage(bytes);
            if(message.data == null)
            {
                Logging.error(string.Format("Null message data."));
                return;
            }

            // Extract the Spixi message
            SpixiMessage spixi_message = new SpixiMessage(message.data);

            switch(spixi_message.type)
            {
                case SpixiMessageCode.chat:
                    {
                        // Add the message to the friend list
                        FriendList.addMessage(message.sender, Encoding.UTF8.GetString(spixi_message.data));
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
                        FriendList.setNickname(message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;

                case SpixiMessageCode.requestAdd:
                    {
                        // Friend request
                        handleRequestAdd(message.sender);
                    }
                    break;

                case SpixiMessageCode.acceptAdd:
                    {
                        // Friend accepted request
                        handleAcceptAdd(message.sender);
                    }
                    break;

                case SpixiMessageCode.requestFunds:
                    {
                        // Friend requested funds
                        handleRequestFunds(message.sender, Encoding.UTF8.GetString(spixi_message.data));
                    }
                    break;
            }
        }

        // Sends the nickname back to the sender, detects if it should fetch the sender's nickname and fetches it automatically
        private static void handleGetNick(byte[] sender_wallet, string text)
        {
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                byte[] pub_k = FriendList.findContactPubkey(sender_wallet);
                if (pub_k == null)
                {
                    Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));

                    foreach (Presence pr in PresenceList.presences)
                    {
                        Console.WriteLine("Presence: {0}", Base58Check.Base58CheckEncoding.EncodePlain(pr.wallet));
                    }
                    return;
                }

                friend = new Friend(sender_wallet, pub_k, "Unknown");
                FriendList.addFriend(sender_wallet, pub_k, "Unknown");

                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getNick, new byte[1]);

                // Also request the nickname of the sender
                // Prepare the message and send to the S2 nodes
                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.info;
                message.recipient = sender_wallet;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                string relayip = friend.searchForRelay();
                StreamProcessor.sendMessage(message, relayip);

            }

            SpixiMessage reply_spixi_message = new SpixiMessage(SpixiMessageCode.nick, Encoding.UTF8.GetBytes(Node.localStorage.nickname));

            // Send the nickname message to the S2 nodes
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();
            StreamProcessor.sendMessage(reply_message, friend.searchForRelay());

            return;
        }

        private static void handleRequestAdd(byte[] sender_wallet)
        {
            byte[] pub_k = FriendList.findContactPubkey(sender_wallet);
            if (pub_k == null)
            {
                Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));

                foreach (Presence pr in PresenceList.presences)
                {
                    Console.WriteLine("Presence: {0}", Base58Check.Base58CheckEncoding.EncodePlain(pr.wallet));
                }
                return;
            }

            FriendList.addFriend(sender_wallet, pub_k, "New Contact", false);
            FriendList.addMessageWithType(FriendMessageType.requestAdd, sender_wallet, "");
        }

        private static void handleAcceptAdd(byte[] sender_wallet)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                byte[] pub_k = FriendList.findContactPubkey(sender_wallet);
                if (pub_k == null)
                {
                    Console.WriteLine("Contact {0} not found in presence list!", Base58Check.Base58CheckEncoding.EncodePlain(sender_wallet));

                    foreach (Presence pr in PresenceList.presences)
                    {
                        Console.WriteLine("Presence: {0}", Base58Check.Base58CheckEncoding.EncodePlain(pr.wallet));
                    }
                    return;
                }

                friend = new Friend(sender_wallet, pub_k, "New Contact");
                FriendList.addFriend(sender_wallet, pub_k, "New Contact");

                // Also request the nickname of the sender
                // Prepare the message and send to the S2 nodes
                SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.getNick, new byte[1]);

                StreamMessage message = new StreamMessage();
                message.type = StreamMessageCode.info;
                message.recipient = friend.walletAddress;
                message.sender = Node.walletStorage.getPrimaryAddress();
                message.transaction = new byte[1];
                message.sigdata = new byte[1];
                message.data = spixi_message.getBytes();

                string relayip = friend.searchForRelay();
                StreamProcessor.sendMessage(message, relayip);


            }

            SpixiMessage reply_spixi_message = new SpixiMessage(SpixiMessageCode.nick, Encoding.UTF8.GetBytes(Node.localStorage.nickname));

            // Send the nickname message to the S2 nodes
            StreamMessage reply_message = new StreamMessage();
            reply_message.type = StreamMessageCode.info;
            reply_message.recipient = friend.walletAddress;
            reply_message.sender = Node.walletStorage.getPrimaryAddress();
            reply_message.transaction = new byte[1];
            reply_message.sigdata = new byte[1];
            reply_message.data = reply_spixi_message.getBytes();
            StreamProcessor.sendMessage(reply_message, friend.searchForRelay());

        }


        private static void handleRequestFunds(byte[] sender_wallet, string amount)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender_wallet);
            if (friend == null)
            {
                return;
            }

            FriendList.addMessageWithType(FriendMessageType.requestFunds, sender_wallet, amount);
        }



    }
}