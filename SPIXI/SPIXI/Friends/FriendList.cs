using IXICore;
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
    class FriendList
    {
        public static List<Friend> friends = new List<Friend>();

        private static Cuckoo friendMatcher = new Cuckoo(128); // default size of 128, will be increased if neccessary

        private static Dictionary<byte[], CustomAppPage> appPages = new Dictionary<byte[], CustomAppPage>(new ByteArrayComparer());

        public static bool saveToStorage()
        {
            return Node.localStorage.writeAccountFile();
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
            if(friend == null)
            {
                Logging.error("Received nickname for a friend that's not in the friend list.");
                return;
            }
            if(friend.bot && real_sender_address != null)
            {
                if(!friend.contacts.ContainsKey(real_sender_address))
                {
                    friend.contacts.Add(real_sender_address, new BotContact());
                }
                if (friend.contacts[real_sender_address].nick != nick)
                {
                    friend.contacts[real_sender_address].nick = nick;
                    // update messages with the new nick
                    for (int i = friend.messages.Count - 1, j = 0; i >= 0; i--, j++)
                    {
                        if(j > 1000)
                        {
                            break;
                        }
                        if(friend.messages[i].senderNick != "")
                        {
                            continue;
                        }
                        if(friend.messages[i].senderAddress == null || real_sender_address == null)
                        {
                            Logging.warn("Sender address is null");
                            continue;
                        }
                        if(friend.messages[i].senderAddress.SequenceEqual(real_sender_address))
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
                }
            }
            else
            {
                friend.nickname = nick;
                Node.shouldRefreshContacts = true;
            }
        }

        public static void addMessage(byte[] id, byte[] wallet_address, string message, byte[] sender_address = null, long timestamp = 0, bool fire_local_notification = true)
        {
            addMessageWithType(id, FriendMessageType.standard, wallet_address, message, false, sender_address, timestamp, fire_local_notification);
        }

        public static FriendMessage addMessageWithType(byte[] id, FriendMessageType type, byte[] wallet_address, string message, bool local_sender = false, byte[] sender_address = null, long timestamp = 0, bool fire_local_notification = true)
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

            string sender_nick = "";
            if(friend.bot)
            {
                if (!local_sender)
                {
                    if (friend.contacts.ContainsKey(sender_address))
                    {
                        sender_nick = friend.contacts[sender_address].nick;
                    }
                    else
                    {
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
            if(friend.bot && local_sender)
            {
                friend_message.read = true;
            }

            lock (friend.messages)
            {
                // TODO should be optimized
                if(id != null && friend.messages.Find(x => x.id != null && x.id.SequenceEqual(id)) != null)
                {
                    Logging.warn("Message with id {0} was already in message list.", Crypto.hashToString(id));
                    return null;
                }
                friend.messages.Add(friend_message);
            }

            // If a chat page is visible, insert the message directly
            if (friend.chat_page != null)
            {
                friend.chat_page.insertMessage(friend_message);
            }

            // Send a local push notification if Spixi is not in the foreground
            if (fire_local_notification && !local_sender)
            {
                if (App.isInForeground == false || friend.chat_page == null)
                {
                    DependencyService.Get<IPushService>().showLocalNotification("Spixi", "New Message", Base58Check.Base58CheckEncoding.EncodePlain(friend.walletAddress));
                }
            }

            ISystemAlert alert = DependencyService.Get<ISystemAlert>();
            if (alert != null)
                alert.flash();

            // Write to chat history
            Node.localStorage.writeMessagesFile(wallet_address, friend.messages);

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
            Node.localStorage.deleteMessagesFile(friend.walletAddress);

            bool stat = friends.Remove(friend);
            if (!stat)
                return stat;

            lock(friendMatcher)
            {
                friendMatcher.Delete(friend.walletAddress);
            }

            // Write changes to storage
            stat = saveToStorage();

            Node.shouldRefreshContacts = true;

            return stat;
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

            byte[] wallet = presence.wallet;

            lock (presence)
            {
                // Go through each presence address searching for C nodes
                foreach (PresenceAddress addr in presence.addresses)
                {
                    // Only check Client nodes
                    if (addr.type == 'C')
                    {
                        // We have a potential candidate here, store it
                        hostname = addr.address;

                        string[] hostname_split = hostname.Split(':');

                        if (hostname_split.Count() == 2 && NetworkUtils.validateIP(hostname_split[0]))
                        {
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
                    lock (friend.messages)
                    {
                        friend.messages.Clear();
                    }

                    // Remove history file
                    Node.localStorage.deleteMessagesFile(friend.walletAddress);
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
                    if (PresenceList.getPresenceByAddress(friend.walletAddress) != null)
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
            // TODO TODO use hidden address matcher
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

        public static CustomAppPage getAppPage(byte[] session_id)
        {
            lock(appPages)
            {
                if(appPages.ContainsKey(session_id))
                {
                    return appPages[session_id];
                }
                return null;
            }
        }

        public static void addAppPage(CustomAppPage page)
        {
            lock(appPages)
            {
                appPages.Add(page.sessionId, page);
            }
        }

        public static void removeAppPage(byte[] session_id)
        {
            lock (appPages)
            {
                appPages.Remove(session_id);
            }
        }
    }
}
