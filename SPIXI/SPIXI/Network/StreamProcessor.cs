using DLT;
using DLT.Meta;
using DLT.Network;
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
    public enum SpixiMessageCode
    {
        chat,
        getNick,
        nick,
        requestAdd,
        acceptAdd,
        requestFunds
    }


    class Message
    {
        public string recipientAddress;
        public string transactionID;
        public byte[] data;
        private string id;

        public Message()
        {
            id = Guid.NewGuid().ToString();
        }

        public Message(byte[] bytes)
        {
            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    id = reader.ReadString();
                    recipientAddress = reader.ReadString();
                    int data_length = reader.ReadInt32();
                    data = reader.ReadBytes(data_length);
                }
            }
        }

        public string getID()
        {
            return id;
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id);
                    writer.Write(recipientAddress);
                    int data_length = data.Length;
                    writer.Write(data_length);
                    writer.Write(data);
                }
                return m.ToArray();
            }
        }

    }

    class StreamProcessor
    {
        static List<Message> messages = new List<Message>(); // List that stores messages until the keypair is generated

        static List<Message> offlineMessages = new List<Message>(); // List that stores messages until receiving contact is online
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
            List<Message> cache = new List<Message>();

            // Read the persistent offline messages
            offlineMessages = Node.localStorage.readOfflineMessagesFile();

            // Only check for offline messages when the loop is active
            while (continueRunning)
            {
                bool writeToFile = false;
                lock (offlineMessages)
                {
                    // Go through each message
                    foreach (Message message in offlineMessages)
                    {
                        // Extract the public key from the Presence List
                        string pub_k = FriendList.findContactPubkey(message.recipientAddress);
                        if (pub_k.Length < 1)
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
                    foreach (Message message in cache)
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

        private static void addOfflineMessage(Message msg)
        {
            lock(offlineMessages)
            {
                offlineMessages.Add(msg);
                //
                Node.localStorage.writeOfflineMessagesFile(offlineMessages);
            }
        }


        // Send an encrypted message using the S2 network
        public static void sendMessage(Message msg, string hostname = null)
        {
            string pub_k = FriendList.findContactPubkey(msg.recipientAddress);
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
            }
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
                        Message tmp_message = null;
                        foreach (Message message in messages)
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
        private static void sendEncryptedMessage(Message msg, string key, Socket socket)
        {
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
        }

        // Prepare a Spixi S2 message. Encrypts the provided text combined with a SpixiMessageCode using the provided publicKey
        public static byte[] prepareSpixiMessage(SpixiMessageCode code, string text, string publicKey)
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

                    byte[] encrypted_message = CryptoManager.lib.encryptData(m.ToArray(), publicKey);

                    return encrypted_message;
                }
            }
        }

        public static void receiveData(byte[] bytes, Socket socket)
        {
            Console.WriteLine("NET: Receiving S2 data!");

            //string message = Encoding.UTF8.GetString(data);
            //Console.WriteLine("Encrypted message: {0}", message);

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {

                    string sender = reader.ReadString();
                    string transaction_id = reader.ReadString();
                    string private_key = reader.ReadString();

                    int encrypted_bytes_count = reader.ReadInt32();
                    // Decrypt the S2 message
                    byte[] encrypted_message = CryptoManager.lib.decryptDataS2(reader.ReadBytes(encrypted_bytes_count),
                        private_key);

                    // Read and parse the Spixi message
                    readSpixiMessage(encrypted_message, sender);
                }
            }

        }

        // Extracts a Spixi message from an encrypted byte array
        public static void readSpixiMessage(byte[] bytes, string sender)
        {
            // Decrypt the client message
            byte[] decrypted_message =  CryptoManager.lib.decryptData(bytes, Node.walletStorage.encPrivateKey);

            SpixiMessageCode code = SpixiMessageCode.chat;
            using (MemoryStream m = new MemoryStream(decrypted_message))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int message_code = reader.ReadInt32() - 23;
                    code = (SpixiMessageCode)message_code;

                    string text_message = reader.ReadString();

                    // Parse the message
                    parseSpixiMessage(code, text_message, sender);
                }
            }
        }

        // Parse a provided Spixi message
        public static void parseSpixiMessage(SpixiMessageCode code, string text, string sender)
        {
            try
            {
                switch (code)
                {
                    case SpixiMessageCode.chat:
                        {
                            // Add the message to the friend list
                            FriendList.addMessage(sender, text);
                        }
                        break;

                    case SpixiMessageCode.getNick:
                        {
                            // Send the nickname to the sender as requested
                            handleGetNick(sender, text);
                        }
                        break;

                    case SpixiMessageCode.nick:
                        {
                            // Set the nickname for the corresponding address
                            FriendList.setNickname(sender, text);
                        }
                        break;

                    case SpixiMessageCode.requestAdd:
                        {
                            // Friend request
                            handleRequestAdd(sender);
                        }
                        break;

                    case SpixiMessageCode.acceptAdd:
                        {
                            // Friend accepted request
                            handleAcceptAdd(sender);
                        }
                        break;

                    case SpixiMessageCode.requestFunds:
                        {
                            // Friend requested funds
                            handleRequestFunds(sender, text);
                        }
                        break;

                    default:
                        break;
                }

            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error parsing spixi message. Details: {0}", e.ToString()));
            }
        }

        // Sends the nickname back to the sender, detects if it should fetch the sender's nickname and fetches it automatically
        private static void handleGetNick(string sender, string text)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
            {
                string pub_k = FriendList.findContactPubkey(sender);
                if (pub_k.Length < 1)
                {
                    Console.WriteLine("Contact {0} not found in presence list!", sender);

                    foreach (Presence pr in PresenceList.presences)
                    {
                        Console.WriteLine("Presence: {0}", pr.wallet);
                    }
                    return;
                }

                friend = new Friend(sender, pub_k, "Unknown");
                FriendList.addFriend(sender, pub_k, "Unknown");

                // Also request the nickname of the sender
                // Prepare the message and send to the S2 nodes
                byte[] encrypted_message_get = prepareSpixiMessage(SpixiMessageCode.getNick, "", pub_k);

                Message nick_message = new Message();
                nick_message.recipientAddress = sender;
                nick_message.data = encrypted_message_get;
                sendMessage(nick_message);
            }

            // Send the nickname message to the S2 nodes
            string recipient_address = friend.wallet_address;
            byte[] encrypted_message = prepareSpixiMessage(SpixiMessageCode.nick, Node.localStorage.nickname, friend.pubkey);

            Message reply_message = new Message();
            reply_message.recipientAddress = recipient_address;
            reply_message.data = encrypted_message;
            sendMessage(reply_message);

            return;
        }

        private static void handleRequestAdd(string sender)
        {
            string pub_k = FriendList.findContactPubkey(sender);
            if (pub_k.Length < 1)
            {
                Console.WriteLine("Contact {0} not found in presence list!", sender);

                foreach (Presence pr in PresenceList.presences)
                {
                    Console.WriteLine("Presence: {0}", pr.wallet);
                }
                return;
            }

            FriendList.addFriend(sender, pub_k, "New Contact", false);
            FriendList.addMessageWithType(FriendMessageType.requestAdd, sender, "");
        }

        private static void handleAcceptAdd(string sender)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
            {
                string pub_k = FriendList.findContactPubkey(sender);
                if (pub_k.Length < 1)
                {
                    Console.WriteLine("Contact {0} not found in presence list!", sender);

                    foreach (Presence pr in PresenceList.presences)
                    {
                        Console.WriteLine("Presence: {0}", pr.wallet);
                    }
                    return;
                }

                friend = new Friend(sender, pub_k, "New Contact");
                FriendList.addFriend(sender, pub_k, "New Contact");

                // Also request the nickname of the sender
                // Prepare the message and send to the S2 nodes
                byte[] encrypted_message_get = prepareSpixiMessage(SpixiMessageCode.getNick, "", pub_k);

                Message nick_message = new Message();
                nick_message.recipientAddress = sender;
                nick_message.data = encrypted_message_get;
                sendMessage(nick_message);
            }

            // Send the nickname message to the S2 nodes
            string recipient_address = friend.wallet_address;
            byte[] encrypted_message = prepareSpixiMessage(SpixiMessageCode.nick, Node.localStorage.nickname, friend.pubkey);

            Message reply_message = new Message();
            reply_message.recipientAddress = recipient_address;
            reply_message.data = encrypted_message;
            sendMessage(reply_message);
        }


        private static void handleRequestFunds(string sender, string amount)
        {
            // Retrieve the corresponding contact
            Friend friend = FriendList.getFriend(sender);
            if (friend == null)
            {
                return;
            }

            FriendList.addMessageWithType(FriendMessageType.requestFunds, sender, amount);
        }



    }
}