using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Meta;
using SPIXI.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SPIXI.Network
{
    class PendingMessageProcessor
    {
        Thread pendingMessagesThread; // Thread that checks the offline messages list for outstanding messages
        bool running = true;

        List<PendingRecipient> pendingRecipients = new List<PendingRecipient>();

        string storagePath = "MsgQueue";
        public PendingMessageProcessor(string root_storage_path)
        {
            storagePath = Path.Combine(root_storage_path, storagePath);
            if(!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            loadLegacy();

            running = true;

            pendingMessagesThread = new Thread(messageProcessorLoop);
            pendingMessagesThread.Start();
        }

        private void loadMessageQueue()
        {
            lock (pendingRecipients)
            {
                var dir_enum = Directory.EnumerateDirectories(storagePath);
                foreach (string dir_path in dir_enum)
                {
                    try
                    {
                        Logging.info("Loading messages for " + Path.GetFileName(dir_path));
                        Friend friend = FriendList.getFriend(Base58Check.Base58CheckEncoding.DecodePlain(Path.GetFileName(dir_path)));
                        if (friend == null)
                        {
                            Directory.Delete(dir_path, true);
                            continue;
                        }
                        PendingRecipient pr = new PendingRecipient(friend.walletAddress);
                        pendingRecipients.Add(pr);
                        var file_arr = Directory.GetFiles(dir_path).OrderBy(x => x);
                        foreach (string file_path in file_arr)
                        {
                            PendingMessage pm = new PendingMessage(file_path);
                            if (pr.messageQueue.Find(x => x.id.SequenceEqual(pm.streamMessage.id)) == null)
                            {
                                pr.messageQueue.Add(new PendingMessageHeader { id = pm.streamMessage.id, filePath = file_path, sendToServer = pm.sendToServer });
                            }
                            else
                            {
                                File.Delete(file_path);
                            }
                        }
                    }catch(Exception e)
                    {
                        Logging.error("Unknown exception occured in loadMessageQueue: " + e);
                    }
                }
            }
        }

        public void stop()
        {
            running = false;
            pendingMessagesThread = null;
        }

        public void loadLegacy()
        {
            List<OfflineMessage> msgs = Node.localStorage.readOfflineMessagesFile();
            foreach(var msg in msgs)
            {
                Friend friend = FriendList.getFriend(msg.message.recipient);
                sendMessage(friend, msg.message, true, msg.offlineAndServer, msg.sendPushNotification);
            }
            Node.localStorage.deleteOfflineMessagesFile();
        }

        public void processPendingMessages()
        {
            lock(pendingRecipients)
            {
                List<PendingRecipient> recipients_to_remove = new List<PendingRecipient>();
                foreach(PendingRecipient recipient in pendingRecipients)
                {
                    Friend friend = FriendList.getFriend(recipient.address);
                    if (friend == null)
                    {
                        recipients_to_remove.Add(recipient);
                        continue;
                    }
                    List<PendingMessageHeader> message_headers = null;
                    if (!friend.online)
                    {
                        message_headers = recipient.messageQueue.FindAll(x => x.sendToServer);
                    }else
                    {
                        message_headers = recipient.messageQueue;
                    }
                    if(message_headers != null && message_headers.Count > 0)
                    {
                        foreach(var message_header in message_headers)
                        {
                            bool sent = processMessage(friend, message_header.filePath);
                            if(message_header.sendToServer && !sent)
                            {
                                break;
                            }
                        }
                    }
                }

                foreach(PendingRecipient recipient in recipients_to_remove)
                {
                    Directory.Delete(Path.Combine(storagePath, Base58Check.Base58CheckEncoding.EncodePlain(recipient.address)), true);
                    pendingRecipients.Remove(recipient);
                }
            }
        }

        public void sendMessage(Friend friend, StreamMessage msg, bool add_to_pending_messages, bool send_to_server, bool send_push_notification)
        {
            lock (pendingRecipients)
            {
                PendingMessage pm = new PendingMessage(msg, send_to_server, send_push_notification);
                PendingMessageHeader tmp_msg_header = getPendingMessageHeader(friend, msg.id);
                if (tmp_msg_header != null)
                {
                    pm.filePath = tmp_msg_header.filePath;
                }
                PendingRecipient tmp_recipient = pendingRecipients.Find(x => x.address.SequenceEqual(msg.recipient));
                if(tmp_recipient == null)
                {
                    tmp_recipient = new PendingRecipient(msg.recipient);
                    pendingRecipients.Add(tmp_recipient);
                }
                if (add_to_pending_messages)
                {
                    pm.save(storagePath);
                    if (tmp_msg_header == null)
                    {
                        tmp_recipient.messageQueue.Add(new PendingMessageHeader() { filePath = pm.filePath, id = pm.streamMessage.id, sendToServer = pm.sendToServer });
                    }
                }
                if (tmp_recipient.messageQueue.Count == 1 || !add_to_pending_messages)
                {
                    sendMessage(friend, pm, add_to_pending_messages);
                }
            }
        }

        private bool processMessage(Friend friend, string file_path)
        {
            PendingMessage pm = new PendingMessage(file_path);
            return sendMessage(friend, pm);
        }

        public bool removeMessage(Friend friend, byte[] msg_id)
        {
            lock (pendingRecipients)
            {
                PendingRecipient pending_recipient = pendingRecipients.Find(x => x.address.SequenceEqual(friend.walletAddress));
                if (pending_recipient != null)
                {
                    PendingMessageHeader tmp_msg_header = pending_recipient.messageQueue.Find(x => x.id.SequenceEqual(msg_id));
                    if (tmp_msg_header != null)
                    {
                        pending_recipient.messageQueue.Remove(tmp_msg_header);
                        if (File.Exists(tmp_msg_header.filePath))
                        {
                            File.Delete(tmp_msg_header.filePath);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private bool sendMessage(Friend friend, PendingMessage pending_message, bool add_to_pending_messages = true)
        {
            StreamMessage msg = pending_message.streamMessage;
            bool send_to_server = pending_message.sendToServer;
            bool send_push_notification = pending_message.sendPushNotification;

            // TODO this function has to be improved and node's wallet address has to be added
            if ((friend.publicKey != null && msg.encryptionType == StreamMessageEncryptionCode.rsa) || (msg.encryptionType != StreamMessageEncryptionCode.rsa && friend.aesKey != null && friend.chachaKey != null))
            {
                if (msg.encryptionType == StreamMessageEncryptionCode.none)
                {
                    // upgrade encryption type
                    msg.encryptionType = StreamMessageEncryptionCode.spixi1;
                }
                if (!msg.encrypt(friend.publicKey, friend.aesKey, friend.chachaKey))
                {
                    Logging.warn("Could not encrypt message for {0}!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                    return false;
                }
            }
            else if (msg.encryptionType != StreamMessageEncryptionCode.none)
            {
                if (friend.publicKey == null)
                {
                    byte[] pub_k = FriendList.findContactPubkey(friend.walletAddress);
                    friend.publicKey = pub_k;
                }

                Logging.warn("Could not send message to {0}, due to missing encryption keys!", Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient));
                return false;
            }

            bool sent = false;
            if (friend.online)
            {
                string hostname = friend.searchForRelay();
                if (hostname != "" && hostname != null)
                {
                    StreamClientManager.connectTo(hostname, null); // TODO replace null with node address
                    sent = StreamClientManager.sendToClient(hostname, ProtocolMessageCode.s2data, msg.getBytes(), msg.id);
                }
            }

            if (!friend.online || !sent)
            {
                if (send_to_server)
                {
                    send_to_server = Config.enablePushNotifications;
                    if (friend.bot)
                    {
                        send_to_server = false;
                        send_push_notification = false;
                    }
                }
                if (send_to_server)
                {
                    if (OfflinePushMessages.sendPushMessage(msg, send_push_notification))
                    {
                        pending_message.sendToServer = false;
                        if (add_to_pending_messages)
                        {
                            pending_message.save(storagePath);
                        }
                        PendingMessageHeader tmp_msg_header = getPendingMessageHeader(friend, pending_message.streamMessage.id);
                        if (tmp_msg_header != null)
                        {
                            tmp_msg_header.sendToServer = false;
                        }
                        return true;
                    }
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

        private PendingMessageHeader getPendingMessageHeader(Friend friend, byte[] msg_id)
        {
            PendingRecipient pending_recipient = pendingRecipients.Find(x => x.address.SequenceEqual(friend.walletAddress));
            if (pending_recipient != null)
            {
                return pending_recipient.messageQueue.Find(x => x.id.SequenceEqual(msg_id));
            }
            return null;
        }

        private void messageProcessorLoop()
        {
            loadMessageQueue();

            while (running)
            {
                try
                {
                    //sendPendingRequests();
                    processPendingMessages();
                }
                catch (Exception e)
                {
                    Logging.error("Unknown exception occured in messageProcessorLoop: " + e);
                }

                Thread.Sleep(5000);
            }
        }

        private void sendPendingRequests()
        {
            lock (FriendList.friends)
            {
                List<Friend> friend_list = new List<Friend>();
                if (Config.enablePushNotifications)
                {
                    friend_list = FriendList.friends.FindAll(x => x.handshakeStatus < 5);
                }
                else
                {
                    friend_list = FriendList.friends.FindAll(x => x.handshakeStatus < 5 && x.online);
                }
                foreach (var friend in friend_list)
                {
                    if (friend.handshakePushed)
                    {
                        continue;
                    }
                    switch (friend.handshakeStatus)
                    {
                        // Add friend request has been sent but no confirmation has been received
                        case 0:
                            if (friend.approved)
                            {
                                Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                                StreamProcessor.sendContactRequest(friend);
                            }
                            break;

                        // Request has been accepted but no confirmation received, resend acceptance
                        case 1:
                            if (friend.approved && friend.aesKey != null)
                            {
                                Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                                StreamProcessor.sendAcceptAdd(friend, false);
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
                            StreamProcessor.sendNickname(friend);
                            break;

                        // Avatar confirmation hasn't been received
                        case 4:
                            Logging.info("Sending pending request for: {0}, status: {1}", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress), friend.handshakeStatus);
                            if (friend.online)
                            {
                                StreamProcessor.sendAvatar(friend);
                            }
                            break;
                    }
                }
            }
        }
    }
}
