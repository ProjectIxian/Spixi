﻿using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using Spixi;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using IFilePicker = SPIXI.Interfaces.IFilePicker;

namespace SPIXI
{
    public class FriendList
    {
        public static List<Friend> friends = new List<Friend>();

        private static Cuckoo friendMatcher = new Cuckoo(128); // default size of 128, will be increased if neccessary

        public static string accountsPath { get; private set; } = "Acc";

        public static bool contactsLoaded = false;

        public static void init(string base_path)
        {
            accountsPath = Path.Combine(base_path, accountsPath);
            if(!Directory.Exists(accountsPath))
            {
                Directory.CreateDirectory(accountsPath);
            }
            contactsLoaded = false;
        }

        // Retrieves a friend based on the wallet_address
        public static Friend getFriend(Address wallet_address)
        {
            foreach (Friend friend in friends)
            {
                if (friend.walletAddress.SequenceEqual(wallet_address))
                {
                    return friend;
                }
            }
            return null;
        }

        // Set the nickname for a specific wallet address
        public static void setNickname(Address wallet_address, string nick, Address real_sender_address)
        {
            Friend friend = getFriend(wallet_address);
            if (friend == null)
            {
                Logging.error("Received nickname for a friend that's not in the friend list.");
                return;
            }
            if (friend.bot && real_sender_address != null)
            {
                if (!friend.users.hasUser(real_sender_address))
                {
                    friend.users.setPubKey(real_sender_address, null);
                }
                if (friend.users.getUser(real_sender_address).getNick() != nick)
                {
                    friend.users.getUser(real_sender_address).setNick(nick);
                    lock (friend.channels.channels)
                    {
                        foreach (var channel in friend.channels.channels)
                        {
                            List<FriendMessage> messages = friend.getMessages(channel.Value.index);
                            if (messages == null)
                            {
                                continue;
                            }
                            // update messages with the new nick
                            for (int i = messages.Count - 1, j = 0; i >= 0; i--, j++)
                            {
                                if (j > 1000)
                                {
                                    break;
                                }
                                if (messages[i].senderNick != "")
                                {
                                    continue;
                                }
                                if (messages[i].senderAddress == null || real_sender_address == null)
                                {
                                    Logging.warn("Sender address is null");
                                    continue;
                                }
                                if (messages[i].senderAddress.SequenceEqual(real_sender_address))
                                {
                                    messages[i].senderNick = nick;
                                }
                            }
                        }
                    }
                    // update UI with the new nick
                    if (friend.chat_page != null)
                    {
                        Logging.info("Updating group chat nicks");
                        friend.chat_page.updateGroupChatNicks(real_sender_address, nick);
                    }

                    friend.users.writeContactsToFile();
                }
            }
            else
            {
                if(friend.nickname != nick)
                {
                    friend.nickname = nick;
                    Node.shouldRefreshContacts = true;
                }
            }
        }
        // Set the avatar for a specific wallet address
        public static void setAvatar(Address wallet_address, byte[] avatar, Address real_sender_address)
        {
            Friend friend = getFriend(wallet_address);
            if (friend == null)
            {
                Logging.error("Received nickname for a friend that's not in the friend list.");
                return;
            }
            string address;
            if (friend.bot && real_sender_address != null)
            {
                address = real_sender_address.ToString();
            }
            else
            {
                address = wallet_address.ToString();
            }
            Node.localStorage.deleteAvatar(address);
            if (avatar != null)
            {
                Node.localStorage.writeAvatar(address, avatar);
                Node.localStorage.writeAvatar(address + "_128", SFilePicker.ResizeImage(avatar, 128, 128, 100));
            }
            Node.shouldRefreshContacts = true;
        }

        public static FriendMessage addMessage(byte[] id, Address wallet_address, int channel, string message, Address sender_address = null, long timestamp = 0, bool fire_local_notification = true)
        {
            return addMessageWithType(id, FriendMessageType.standard, wallet_address, channel, message, false, sender_address, timestamp, fire_local_notification);
        }

        public static FriendMessage addMessageWithType(byte[] id, FriendMessageType type, Address wallet_address, int channel, string message, bool local_sender = false, Address sender_address = null, long timestamp = 0, bool fire_local_notification = true, int payable_data_len = 0)
        {
            Friend friend = getFriend(wallet_address);
            if(friend == null)
            {
                // No matching contact found in friendlist
                // Add the contact, then issue the message again?
                // TODO: need to fetch the stage 1 public key somehow here
                // Ignoring such messages for now
                //addFriend(wallet_address, "pubkey", "Unknown");
                //addMessage(wallet_address, message);

                Logging.warn("Received message but contact isn't in our contact list.");
                return null;
            }

            if (!friend.online)
            {
                using (MemoryStream mw = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(mw))
                    {
                        writer.WriteIxiVarInt(wallet_address.addressWithChecksum.Length);
                        writer.Write(wallet_address.addressWithChecksum);

                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.getPresence2, mw.ToArray(), null);
                    }
                }
            }

            bool set_read = false;

            string sender_nick = "";
            if(friend.bot && sender_address != null)
            {
                if(IxianHandler.getWalletStorage().isMyAddress(sender_address))
                {
                    if (!local_sender)
                    {
                        set_read = true;
                    }
                    local_sender = true;
                }
                if (!local_sender)
                {
                    if (friend.users.hasUser(sender_address) && friend.users.getUser(sender_address).getNick() != "")
                    {
                        sender_nick = friend.users.getUser(sender_address).getNick();
                    }
                    else
                    {
                        if(!friend.users.hasUser(sender_address) || friend.users.getUser(sender_address).publicKey == null)
                        {
                            StreamProcessor.requestBotUser(friend, sender_address);
                        }
                    }
                }
            }else
            {
                sender_nick = friend.nickname;
            }

            if(timestamp == 0)
            {
                timestamp = Clock.getTimestamp();
            }

            FriendMessage friend_message = new FriendMessage(id, message, timestamp, local_sender, type, sender_address, sender_nick);
            friend_message.payableDataLen = payable_data_len;

            List<FriendMessage> messages = friend.getMessages(channel);
            if(messages == null)
            {
                Logging.warn("Message with id {0} was sent to invalid channel {1}.", Crypto.hashToString(id), channel);
                return null;
            }
            lock (messages)
            {
                // TODO should be optimized
                if(id != null)
                {
                    FriendMessage tmp_msg = messages.Find(x => x.id != null && x.id.SequenceEqual(id));

                    if(tmp_msg != null)
                    {
                        if (!tmp_msg.localSender)
                        {
                            Logging.warn("Message with id {0} was already in message list.", Crypto.hashToString(id));
                        }else
                        {
                            friend.setMessageRead(channel, id);
                        }
                        if (messages.Last() == tmp_msg)
                        {
                            friend.metaData.setLastMessage(tmp_msg, channel);
                            friend.metaData.setLastReceivedMessageIds(tmp_msg.id, channel);
                            friend.saveMetaData();
                        }
                        return null;
                    }else
                    {
                        friend.metaData.setLastReceivedMessageIds(friend_message.id, channel);
                    }
                }
                else if(!local_sender)
                {
                    Logging.error("Message id sent by {0} is null!", friend.walletAddress.ToString());
                    return null;
                }
                messages.Add(friend_message);
            }

            bool old_message = false;
            // Check if the message was sent before the friend was added to the contact list
            if(friend.addedTimestamp > friend_message.timestamp)
            {
                old_message = true;               
            }

            if (set_read || old_message)
            {
                friend_message.confirmed = true;
                friend_message.read = true;
            }

            friend.metaData.setLastMessage(friend_message, channel);
            friend.saveMetaData();

            // If a chat page is visible, insert the message directly
            if (friend.chat_page != null)
            {
                friend.chat_page.insertMessage(friend_message, channel);
            }else if(!set_read)
            {
                // Increase the unread counter if this is a new message
                if(!old_message)
                    friend.metaData.unreadMessageCount++;

                friend.saveMetaData();
            }

            UIHelpers.setContactStatus(friend.walletAddress, friend.online, friend.getUnreadMessageCount(), message, timestamp);

            // Only send alerts if this is a new message
            if (old_message == false)
            {
                // Send a local push notification if Spixi is not in the foreground
                if (fire_local_notification && !local_sender)
                {
                    if (App.isInForeground == false || friend.chat_page == null)
                    {
                        // don't fire notification for nickname and avatar
                        if (!friend_message.id.SequenceEqual(new byte[] { 4 }) && !friend_message.id.SequenceEqual(new byte[] { 5 }))
                        {
                            if (friend.bot == false
                                || (friend.metaData.botInfo != null && friend.metaData.botInfo.sendNotification))
                            {
                                SPushService.showLocalNotification("Spixi", "New Message", friend.walletAddress.ToString());
                            }
                        }
                    }
                }

                SSystemAlert.flash();
            }
            // Write to chat history
            Node.localStorage.requestWriteMessages(wallet_address, channel);

            return friend_message;
        }

        // Sort the friend list alphabetically based on nickname
        public static void sortFriends()
        {
            friends = friends.OrderBy(x => x.nickname).ToList();
        }

        public static Friend addFriend(Address wallet_address, byte[] public_key, string name, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approved = true)
        {
            Friend new_friend = new Friend(wallet_address, public_key, name, aes_key, chacha_key, key_generated_time, approved);
            return addFriend(new_friend);
        }

        public static Friend addFriend(Friend new_friend)
        {
            if(friends.Find(x => x.walletAddress.SequenceEqual(new_friend.walletAddress)) != null)
            {
                // Already in the list
                return null;
            }

            lock (friends)
            {
                // Add new friend to the friendlist
                friends.Add(new_friend);
            }

            Node.shouldRefreshContacts = true;

            if (new_friend.approved)
            {
                lock (friendMatcher)
                {
                    if (friendMatcher.Add(new_friend.walletAddress.addressNoChecksum) == Cuckoo.CuckooStatus.NotEnoughSpace)
                    {
                        // rebuild cuckoo filter with a larger size
                        friendMatcher = new Cuckoo(friendMatcher.numItems * 2);
                        lock (friends)
                        {
                            foreach (Friend f in friends)
                            {
                                friendMatcher.Add(f.walletAddress.addressNoChecksum);
                            }
                        }
                    }
                }
                ProtocolMessage.resubscribeEvents();
            }

            sortFriends();

            return new_friend;
        }

        // Clear the entire list of contacts
        public static bool clear()
        {
            lock (friends)
            {
                friends.Clear();
            }
            return true;
        }

        // Removes a friend from the list
        public static bool removeFriend(Friend friend)
        {
            // Remove history file
            Node.localStorage.deleteMessages(friend.walletAddress);

            // Delete avatar
            Node.localStorage.deleteAvatar(friend.walletAddress.ToString());

            lock (friends)
            {
                if (!friends.Remove(friend))
                {
                    return false;
                }
            }

            lock(friendMatcher)
            {
                friendMatcher.Delete(friend.walletAddress.addressNoChecksum);
            }

            // Write changes to storage
            friend.delete();

            Node.shouldRefreshContacts = true;

            return true;
        }

        // Finds a presence entry's pubkey
        public static byte[] findContactPubkey(Address wallet_address)
        {
            Friend f = getFriend(wallet_address);
            if(f != null && f.publicKey != null)
            {
                return f.publicKey;
            }

            Presence p = PresenceList.getPresenceByAddress(wallet_address);
            if(p != null && p.addresses.Find(x => x.type == 'C') != null)
            {
                return p.pubkey;
            }
            return null;
        }

        // Retrieve a presence entry connected S2 node. Returns null if not found
        public static string getRelayHostname(Address wallet_address)
        {
            string hostname = null;
            Presence presence = PresenceList.getPresenceByAddress(wallet_address);
            if (presence == null)
            {
                using (MemoryStream mw = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(mw))
                    {
                        writer.WriteIxiVarInt(wallet_address.addressWithChecksum.Length);
                        writer.Write(wallet_address.addressWithChecksum);

                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M', 'H' }, ProtocolMessageCode.getPresence2, mw.ToArray(), null);
                    }
                }
                return null;
            }

            lock (presence)
            {
                // Go through each presence address searching for C nodes
                foreach (PresenceAddress addr in presence.addresses)
                {
                    // Only check Client nodes
                    if (addr.type == 'C')
                    {
                        string[] hostname_split = addr.address.Split(':');

                        if (hostname_split.Count() == 2 && NetworkUtils.validateIP(hostname_split[0]))
                        {
                            hostname = addr.address;
                            break;
                        }
                    }
                }
            }

            // Finally, return the ip address of the node
            return hostname;
        }

        public static void deleteAccounts()
        {
            lock (friends)
            {
                foreach (Friend friend in friends)
                {
                    friend.delete();
                }
            }
        }

        // Deletes entire history for all friends in the friendlist
        public static void deleteEntireHistory()
        {
            lock (friends)
            {
                foreach (Friend friend in friends)
                {
                    // Clear messages from memory
                    friend.deleteHistory();
                }
            }
        }

        // Updates all contacts in the friendlist
        public static void Update()
        {
            lock (friends)
            {
                // Go through each friend and check for the pubkey in the PL
                foreach (Friend friend in friends)
                {
                    Presence presence = null;

                    try
                    {
                        presence = PresenceList.getPresenceByAddress(friend.walletAddress);
                    }
                    catch (Exception e)
                    {
                        Logging.error("Presence Error {0}", e.Message);
                        presence = null;
                    }

                    if (presence != null)
                    {
                        if(friend.online == false)
                        {
                            friend.online = true;
                            UIHelpers.setContactStatus(friend.walletAddress, friend.online, friend.getUnreadMessageCount(), "", 0);
                        }
                    }else
                    {
                        if (friend.online == true)
                        {
                            friend.online = false;
                            UIHelpers.setContactStatus(friend.walletAddress, friend.online, friend.getUnreadMessageCount(), "", 0);
                        }
                    }
                }
            }
        }

        // Returns the number of unread messages
        public static int getUnreadMessageCount()
        {
            int unreadCount = 0;
            lock (friends)
            {
                // Go through each friend and check for the pubkey in the PL
                foreach (Friend friend in friends)
                {
                    unreadCount += friend.getUnreadMessageCount();
                }
            }
            return unreadCount;
        }

        public static byte[] getFriendCuckooFilter()
        {
            lock (friendMatcher)
            {
                return friendMatcher.getFilterBytes();
            }
        }

        public static void requestAllFriendsPresences()
        {
            // TODO TODO use hidden address matcher
            List<Friend> tmp_friends = null;
            lock (friends)
            {
                tmp_friends = new List<Friend>(friends);
            }
            foreach (var entry in tmp_friends)
            {
                using (MemoryStream m = new MemoryStream(1280))
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        writer.WriteIxiVarInt(entry.walletAddress.addressWithChecksum.Length);
                        writer.Write(entry.walletAddress.addressWithChecksum);

                        CoreProtocolMessage.broadcastProtocolMessageToSingleRandomNode(new char[] { 'M', 'H' }, ProtocolMessageCode.getPresence2, m.ToArray(), 0, null);
                    }
                }
            }
        }

        public static void broadcastNicknameChange()
        {
            new Thread(() =>
            {
                List<Friend> tmp_friends = null;
                lock (friends)
                {
                    tmp_friends = new List<Friend>(friends);
                }
                foreach (var friend in tmp_friends)
                {
                    if (friend.approved)
                    {
                        StreamProcessor.sendNickname(friend);
                    }
                }
            }).Start();
        }

        public static void broadcastAvatarChange()
        {
            new Thread(() =>
            {
                List<Friend> tmp_friends = null;
                lock (friends)
                {
                    tmp_friends = new List<Friend>(friends);
                }
                foreach (var friend in tmp_friends)
                {
                    if (friend.handshakeStatus >= 3)
                    {
                        StreamProcessor.sendAvatar(friend);
                    }
                }
            }).Start();
        }


        public static void onLowMemory()
        {
            lock (friends)
            {
                foreach (var friend in friends)
                {
                    friend.freeMemory();
                }
            }
        }

        public static void loadContacts()
        {
            if(contactsLoaded)
            {
                return;
            }
            contactsLoaded = true;
            lock (friends)
            {
                friends.Clear();

                var accs = Directory.EnumerateDirectories(accountsPath);
                foreach(var acc in accs)
                {
                    string acc_path = Path.Combine(acc, "account.ixi");
                    if (File.Exists(acc_path))
                    {
                        try
                        {
                            Friend f = addFriend(new Friend(File.ReadAllBytes(acc_path)));
                            if (f != null)
                            {
                                f.loadMetaData();
                            }
                            else
                            {
                                Logging.error("Error adding contact {0}", acc);
                            }
                        }catch(Exception e)
                        {
                            Logging.error("Exception occured while loading contact {0}: {1}", acc, e);
                        }
                    }else
                    {
                        Logging.error("Error adding contact {0}, account.ixi doesn't exist", acc);
                    }
                }
            }
        }
    }
}
