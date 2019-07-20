using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Meta;
using SPIXI.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SPIXI
{
    class FriendList
    {
        public static List<Friend> friends = new List<Friend>();

        private static List<byte[]> cachedHiddenMatchAddresses = new List<byte[]>();

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
                    // Already in the list
                    return friend;
                }
            }
            return null;
        }

        // Set the nickname for a specific wallet address
        public static void setNickname(byte[] wallet_address, string nick)
        {
            // Go through each friend and check for a matching wallet address
            foreach (Friend friend in friends)
            {
                if (friend.walletAddress.SequenceEqual(wallet_address))
                {
                    friend.nickname = nick;

                    saveToStorage();
                    return;
                }
            }
        }

        public static void addMessage(byte[] wallet_address, string message)
        {
            addMessageWithType(FriendMessageType.standard, wallet_address, message);
        }

        public static void addMessageWithType(FriendMessageType type, byte[] wallet_address, string message)
        {
            foreach (Friend friend in friends)
            {
                if (friend.walletAddress.SequenceEqual(wallet_address))
                {
                    DateTime dt = DateTime.Now;
                    // TODO: message date should be fetched, not generated here
                    FriendMessage friend_message = new FriendMessage(message, String.Format("{0:t}", dt), true, type);
                    friend.messages.Add(friend_message);

                    // If a chat page is visible, insert the message directly
                    if (friend.chat_page != null)
                    {
                        friend.chat_page.insertMessage(friend_message);
                    }
                    else
                    {
                        //CrossLocalNotifications.Current.Show(string.Format("New message from {0}",friend.nickname), message, 100, DateTime.Now.AddSeconds(1));
                    }

                    // Write to chat history
                    Node.localStorage.writeMessagesFile(wallet_address, friend.messages);

                    return;
                }
            }

            // No matching contact found in friendlist
            // Add the contact, then issue the message again
            // TODO: need to fetch the stage 1 public key somehow here
            // Ignoring such messages for now
            //addFriend(wallet_address, "pubkey", "Unknown");
            //addMessage(wallet_address, message);
        }


        public static Friend addFriend(byte[] wallet_address, byte[] public_key, string name, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approved = true)
        {
            foreach (Friend friend in friends)
            {
                if (friend.walletAddress.SequenceEqual(wallet_address))
                {
                    // Already in the list
                    return null;
                }
            }

            Friend new_friend = new Friend(wallet_address, public_key, name, aes_key, chacha_key, key_generated_time, approved);

            // Add new friend to the friendlist
            friends.Add(new_friend);

            if (approved)
            {
                cachedHiddenMatchAddresses = null;
                ProtocolMessage.resubscribeEvents();
            }

            return new_friend;
        }

        // Scan the presence list for new contacts
        public static void refreshList()
        {
            return;
            /*foreach (Presence presence in PresenceList.presences)
            {
                // Show only client nodes as contacts
                bool client_found = false;
                foreach (PresenceAddress addr in presence.addresses)
                {
                    if (addr.type == 'C')
                    {
                        client_found = true;
                        break;
                    }
                }

                if (client_found == false)
                {
                    continue;
                }


                byte[] wallet = presence.wallet;
                byte[] pubkey = presence.pubkey;
                string name = "Unknown";

                if (wallet.SequenceEqual(Node.walletStorage.getPrimaryAddress()))
                {
                    name = Node.localStorage.nickname;
                }

                addFriend(wallet, pubkey, name);
            }*/
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

            cachedHiddenMatchAddresses = null;

            // Write changes to storage
            stat = saveToStorage();
            return stat;
        }

        // Finds a presence entry's pubkey
        public static byte[] findContactPubkey(byte[] wallet_address)
        {
            Friend f = getFriend(wallet_address);
            if(f != null)
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
                    friend.messages.Clear();

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
                        friend.online = true;
                    }else
                    {
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

        public static List<byte[]> getHiddenMatchAddresses()
        {
            if(cachedHiddenMatchAddresses != null)
            {
                return cachedHiddenMatchAddresses;
            }

            lock (friends)
            {
                if(friends.Count() == 0)
                {
                    return null;
                }

                AddressClient ac = new AddressClient();
                foreach (var friend in friends)
                {
                    if (friend.approved)
                    {
                        ac.addAddress(friend.walletAddress);
                    }
                }

                Random rnd = new Random();
                cachedHiddenMatchAddresses = ac.generateHiddenMatchAddresses(rnd, CoreConfig.matcherBytesPerAddress);

                return cachedHiddenMatchAddresses;
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
                foreach (var entry in friends)
                {
                    StreamProcessor.sendNickname(entry);
                }
            }
        }

        public static void resetHiddenMatchAddressesCache()
        {
            cachedHiddenMatchAddresses = null;
        }
    }
}
