using IXICore;
using IXICore.Meta;
using IXICore.Network;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public byte[] walletAddress;
        public byte[] publicKey;

        public string nickname
        {
            get;
            set;
        }

        public byte[] chachaKey = null;
        public string aesPassword = null;

        public string relayIP = null;
        public byte[] relayWallet = null;

        public bool online = false;

        public List<FriendMessage> messages = new List<FriendMessage>();

        public SingleChatPage chat_page = null;

        public bool approved = true;

        public Friend(byte[] wallet, byte[] public_key, string nick, bool approve = true)
        {
            walletAddress = wallet;
            publicKey = public_key;
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

            if (Node.localStorage.deleteMessagesFile(walletAddress) == false)
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


        // Generates a random chacha key and a random aes key
        // Returns the two keys encrypted using the supplied public key
        // Returns null if an error was encountered
        public byte[] generateKeys()
        {
            try
            {
                // Generate random chacha key
                Random random = new Random();
                Byte[] rbytes = new Byte[32];
                random.NextBytes(rbytes);
                chachaKey = rbytes.ToArray();

                // Generate random password for AES
                aesPassword = randomPassword(32);

                byte[] data = null;

                // Store both keys in a byte array
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        writer.Write(chachaKey.Length);
                        writer.Write(chachaKey);
                        writer.Write(aesPassword);
                        data = m.ToArray();
                    }
                }

                // Encrypt the data using RSA with the supplied public key
                return CryptoManager.lib.encryptWithRSA(data, publicKey);
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during generate keys: {0}", e.Message));
            }

            return null;
        }

        // Handles receiving and decryption of keys
        public bool receiveKeys(byte[] data)
        {
            try
            {
                // Decrypt data first
                byte[] decrypted = CryptoManager.lib.decryptWithRSA(data, Node.walletStorage.getPrimaryPrivateKey());

                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        // Read the chacha key
                        int length = reader.ReadInt32();
                        byte[] chacha = reader.ReadBytes(length);

                        // Assign the cacha key
                        chachaKey = chacha.ToArray();

                        // Read and assign the aes password
                        aesPassword = reader.ReadString();

                        // Everything succeeded
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during receive keys: {0}", e.Message));
            }

            return false;
        }

        // Retrieve the friend's connected S2 node address. Returns null if not found
        public string searchForRelay()
        {
            object[] relay = FriendList.getRelay(walletAddress);

            relayIP = null;
            relayWallet = null;
            if (relay != null)
            {
                // Store the last relay ip and wallet for this friend
                relayIP = (string)relay[0];
                relayWallet = (byte[])relay[1];
            }
            // Finally, return the ip address of the node
            return relayIP;
        }

        // Generate a random password string of a specified length
        // Used when generating aes password
        private static string randomPassword(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
