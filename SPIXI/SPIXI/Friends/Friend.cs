using DLT;
using DLT.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI
{
    public enum FriendMessageType
    {
        standard,
        requestAdd,
        requestFunds,
        sentFunds
    }


    public class FriendMessage
    {
        public string message;
        public string timestamp;
        public bool from;
        public bool read;
        public FriendMessageType type;

        public FriendMessage(string msg, string time, bool fr)
        {
            message = msg;
            timestamp = time;
            from = fr;
            read = false;
            type = FriendMessageType.standard;
        }

        public FriendMessage(string msg, string time, bool fr, FriendMessageType t)
        {
            message = msg;
            timestamp = time;
            from = fr;
            read = false;
            type = t;
        }
    }


    public class Friend
    {
        public string wallet_address
        {
            get;
            set;
        }

        public string pubkey
        {
            get;
            set;
        }

        public string nickname
        {
            get;
            set;
        }

        public bool online = false;

        public List<FriendMessage> messages = new List<FriendMessage>();

        public SingleChatPage chat_page = null;

        public bool approved = true;

        public Friend(string wallet, string public_key, string nick, bool approve = true)
        {
            wallet_address = wallet;
            pubkey = public_key;
            nickname = nick;
            approved = approve;

            // Read messages from chat history
            messages = Node.localStorage.readMessagesFile(wallet);
        }

        // Get the number of unread messages
        // TODO: optimize this
        public int getUnreadMessageCount()
        {
            int unreadCount = 0;
            foreach(FriendMessage message in messages)
            {
                if(message.read == false)
                {
                    unreadCount++;
                }
            }
            return unreadCount;
        }

        // Flushes the temporary message history
        public bool flushHistory()
        {
            messages.Clear();
            return true;
        }

        // Deletes the history file and flushes the temporary history
        public bool deleteHistory()
        {

            if (Node.localStorage.deleteMessagesFile(wallet_address) == false)
                return false;

            if (flushHistory() == false)
                return false;

            return true;
        }

        // Check if the last message is unread. Returns true if it is unread.
        public bool checkLastUnread()
        {
            if (messages.Count < 1)
                return false;
            FriendMessage last_message = messages[messages.Count - 1];
            if (last_message.read == false)
                return true;

            return false;
        }

        public int getMessageCount()
        {
            return messages.Count;
        }

        // Set last message as read
        public void setLastRead()
        {
            if (messages.Count < 1)
                return;
            FriendMessage last_message = messages[messages.Count - 1];
            last_message.read = true;
        }

        // Retrieve the friend's connected S2 node. Returns null if not found
        public string getRelayIP()
        {
            string ip = null;
            lock(PresenceList.presences)
            {
                // TODO: optimize this
                // Go through each presence
                foreach(Presence presence in PresenceList.presences)
                {
                    // Check if it matches the friend's wallet address
                    if(presence.wallet.Equals(wallet_address, StringComparison.Ordinal))
                    {
                        // Go through each presence address searching for C nodes
                        foreach (PresenceAddress addr in presence.addresses)
                        {
                            // Only check Client nodes
                            if (addr.type == 'C')
                            {
                                // We have a potential candidate here, store it
                                string candidate_ip = addr.address;

                                // Go through each presence again. This should be more optimized.
                                foreach(Presence s2presence in PresenceList.presences)
                                {
                                    // Go through each single address
                                    foreach (PresenceAddress s2addr in s2presence.addresses)
                                    {
                                        // Only check Relay nodes that have the candidate ip
                                        if (s2addr.type == 'R' && s2addr.address.Equals(candidate_ip, StringComparison.Ordinal))
                                        {
                                            // We found the friend's connected s2 node
                                            ip = s2addr.address;
                                            break;
                                        }
                                    }
                                }
                            }
                            // If we find a valid node ip, don't continue searching
                            if (ip != null)
                                break;
                        }
                        break;
                    }
                }
            }
            // Finally, return the ip address of the node
            return ip;
        }
    }
}
