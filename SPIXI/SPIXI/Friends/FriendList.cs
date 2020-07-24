﻿using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Interfaces;
using SPIXI.Meta;
using SPIXI.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace SPIXI
{
    public class FriendList
    {
        public static List<Friend> friends = new List<Friend>();

        private static Cuckoo friendMatcher = new Cuckoo(128); // default size of 128, will be increased if neccessary

        public static void saveToStorage()
        {
            Node.localStorage.requestWriteAccountFile();
        }

        // Retrieves a friend based on the wallet_address
        public static Friend getFriend(byte[] wallet_address)
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
        public static void setNickname(byte[] wallet_address, string nick, byte[] real_sender_address)
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
                friend.nickname = nick;
                Node.shouldRefreshContacts = true;
            }
        }
        // Set the avatar for a specific wallet address
        public static void setAvatar(byte[] wallet_address, byte[] avatar, byte[] real_sender_address)
        {
            Friend friend = getFriend(wallet_address);
            if (friend == null)
            {
                Logging.error("Received nickname for a friend that's not in the friend list.");
                return;
            }
            if (friend.bot && real_sender_address != null)
            {
                // TODO implement
                /*if (!friend.contacts.ContainsKey(real_sender_address))
                {
                    friend.contacts.Add(real_sender_address, new BotContact());
                }
                if (friend.contacts[real_sender_address].nick != nick)
                {
                    friend.contacts[real_sender_address].nick = nick;
                    // update messages with the new nick
                    for (int i = friend.messages.Count - 1, j = 0; i >= 0; i--, j++)
                    {
                        if (j > 1000)
                        {
                            break;
                        }
                        if (friend.messages[i].senderNick != "")
                        {
                            continue;
                        }
                        if (friend.messages[i].senderAddress == null || real_sender_address == null)
                        {
                            Logging.warn("Sender address is null");
                            continue;
                        }
                        if (friend.messages[i].senderAddress.SequenceEqual(real_sender_address))
                        {
                            friend.messages[i].senderNick = nick;
                        }
                    }
                    // update UI with the new nick
                    if (friend.chat_page != null)
                    {
                        Logging.info("Updating group chat nicks");
                        friend.chat_page.updateGroupChatNicks(real_sender_address, nick);
                    }
                }*/
            }
            else
            {
                Node.localStorage.deleteAvatar(Base58Check.Base58CheckEncoding.EncodePlain(wallet_address));
                Node.localStorage.writeAvatar(Base58Check.Base58CheckEncoding.EncodePlain(wallet_address), avatar);
                Node.localStorage.writeAvatar(Base58Check.Base58CheckEncoding.EncodePlain(wallet_address) + "_128", DependencyService.Get<IPicturePicker>().ResizeImage(avatar, 128, 128));
                Node.shouldRefreshContacts = true;
            }
        }

        public static FriendMessage addMessage(byte[] id, byte[] wallet_address, int channel, string message, byte[] sender_address = null, long timestamp = 0, bool fire_local_notification = true)
        {
            return addMessageWithType(id, FriendMessageType.standard, wallet_address, channel, message, false, sender_address, timestamp, fire_local_notification);
        }

        public static FriendMessage addMessageWithType(byte[] id, FriendMessageType type, byte[] wallet_address, int channel, string message, bool local_sender = false, byte[] sender_address = null, long timestamp = 0, bool fire_local_notification = true, int payable_data_len = 0)
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

            Node.shouldRefreshContacts = true;

            if (!friend.online)
            {
                using (MemoryStream mw = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(mw))
                    {
                        writer.Write(wallet_address.Length);
                        writer.Write(wallet_address);

                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M' }, ProtocolMessageCode.getPresence, mw.ToArray(), null);
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
                            StreamProcessor.requestPubKey(friend, sender_address);
                        }
                        StreamProcessor.requestNickname(friend, sender_address);
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
                            if(messages.Last() == tmp_msg)
                            {
                                friend.setLastMessage(tmp_msg, channel);
                                friend.setLastReceivedMessageIds(tmp_msg.id, channel);
                                FriendList.saveToStorage();
                            }
                        }
                        return null;
                    }
                }
                else if(!local_sender)
                {
                    Logging.error("Message id sent by {0} is null!", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                    return null;
                }
                messages.Add(friend_message);
            }

            if(set_read)
            {
                friend_message.confirmed = true;
                friend_message.read = true;
            }

            friend.setLastMessage(friend_message, channel);
            friend.setLastReceivedMessageIds(friend_message.id, channel);
            FriendList.saveToStorage();

            // If a chat page is visible, insert the message directly
            if (friend.chat_page != null)
            {
                friend.chat_page.insertMessage(friend_message, channel);
            }else if(!set_read)
            {
                friend.unreadMessageCount++;
                FriendList.saveToStorage();
            }

            // Send a local push notification if Spixi is not in the foreground
            if (fire_local_notification && !local_sender)
            {
                if (App.isInForeground == false || friend.chat_page == null)
                {
                    // don't fire notification for nickname and avatar
                    if(!friend_message.id.SequenceEqual(new byte[] { 4 }) && !friend_message.id.SequenceEqual(new byte[] { 5 }))
                    {
                        DependencyService.Get<IPushService>().showLocalNotification("Spixi", "New Message", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                    }
                }
            }

            ISystemAlert alert = DependencyService.Get<ISystemAlert>();
            if (alert != null)
                alert.flash();

            // Write to chat history
            Node.localStorage.requestWriteMessages(wallet_address, channel);

            return friend_message;
        }

        // Sort the friend list alphabetically based on nickname
        public static void sortFriends()
        {
            friends = friends.OrderBy(x => x.nickname).ToList();
        }

        public static Friend addFriend(byte[] wallet_address, byte[] public_key, string name, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approved = true)
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

            // Add new friend to the friendlist
            friends.Add(new_friend);

            Node.shouldRefreshContacts = true;

            if (new_friend.approved)
            {
                lock (friendMatcher)
                {
                    if (friendMatcher.Add(new_friend.walletAddress) == Cuckoo.CuckooStatus.NotEnoughSpace)
                    {
                        // rebuild cuckoo filter with a larger size
                        friendMatcher = new Cuckoo(friendMatcher.numItems * 2);
                        foreach (Friend f in friends)
                        {
                            friendMatcher.Add(f.walletAddress);
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
            friends.Clear();
            return true;
        }

        // Removes a friend from the list
        public static bool removeFriend(Friend friend)
        {
            // Remove history file
            Node.localStorage.deleteMessages(friend.walletAddress);

            // Delete avatar
            Node.localStorage.deleteAvatar(Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));

            if(!friends.Remove(friend))
            {
                return false;
            }

            lock(friendMatcher)
            {
                friendMatcher.Delete(friend.walletAddress);
            }

            // Write changes to storage
            saveToStorage();

            Node.shouldRefreshContacts = true;

            return true;
        }

        // Finds a presence entry's pubkey
        public static byte[] findContactPubkey(byte[] wallet_address)
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
        public static string getRelayHostname(byte[] wallet_address)
        {
            string hostname = null;
            Presence presence = PresenceList.getPresenceByAddress(wallet_address);
            if (presence == null)
            {
                using (MemoryStream mw = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(mw))
                    {
                        writer.Write(wallet_address.Length);
                        writer.Write(wallet_address);

                        CoreProtocolMessage.broadcastProtocolMessage(new char[] { 'M' }, ProtocolMessageCode.getPresence, mw.ToArray(), null);
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
                            Node.shouldRefreshContacts = true;
                        }
                        friend.online = true;
                    }else
                    {
                        if (friend.online == true)
                        {
                            Node.shouldRefreshContacts = true;
                        }
                        friend.online = false;
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
            lock (friends)
            {
                foreach (var entry in friends)
                {
                    using (MemoryStream m = new MemoryStream(1280))
                    {
                        using (BinaryWriter writer = new BinaryWriter(m))
                        {
                            writer.Write(entry.walletAddress.Length);
                            writer.Write(entry.walletAddress);

                            CoreProtocolMessage.broadcastProtocolMessageToSingleRandomNode(new char[] { 'M' }, ProtocolMessageCode.getPresence, m.ToArray(), 0, null);
                        }
                    }
                }
            }
        }

        public static void broadcastNicknameChange()
        {
            lock (friends)
            {
                foreach (var friend in friends)
                {
                    if (friend.approved)
                    {
                        StreamProcessor.sendNickname(friend);
                    }
                }
            }
        }

        public static void broadcastAvatarChange()
        {
            lock (friends)
            {
                foreach (var friend in friends)
                {
                    if (friend.handshakeStatus >= 3)
                    {
                        StreamProcessor.sendAvatar(friend);
                    }
                }
            }
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
    }
}
