using IXICore;
using IXICore.Meta;
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
        sentFunds,
        fileHeader,
        voiceCall,
        voiceCallEnd,
        appSession,
        appSessionEnd
    }

    public class FriendMessage
    {
        private byte[] _id;
        public string message;
        public long timestamp; // timestamp as specified by the sender
        public bool localSender;
        public bool read;
        public bool confirmed;
        public FriendMessageType type;
        public string transferId; // UID of file transfer if applicable
        public bool completed; // for file transfer, indicating whether the transfer completed
        public string filePath; // for file transfer
        public ulong fileSize; // for file transfer

        public byte[] senderAddress;
        public string senderNick = "";

        public long receivedTimestamp; // timestamp of when the message was received; used for storage purposes

        public string transactionId = "";
        public int payableDataLen = 0;

        public Dictionary<string, List<ReactionData>> reactions = new Dictionary<string, List<ReactionData>>();

        public FriendMessage(byte[] id, string msg, long time, bool local_sender, FriendMessageType t, byte[] sender_address = null, string sender_nick = "")
        {
            _id = id;
            message = msg;
            timestamp = time;
            localSender = local_sender;
            read = false;
            type = t;
            confirmed = false;
            senderAddress = sender_address;
            senderNick = sender_nick;
            transferId = "";
            completed = false;
            filePath = "";
            fileSize = 0;
            receivedTimestamp = Clock.getTimestamp();
        }

        public FriendMessage(string msg, long time, bool local_sender, FriendMessageType t, byte[] sender_address = null, string sender_nick = "")
        {
            message = msg;
            timestamp = time;
            localSender = local_sender;
            read = false;
            type = t;
            confirmed = false;
            senderAddress = sender_address;
            senderNick = sender_nick;
            transferId = "";
            completed = false;
            filePath = "";
            fileSize = 0;
            receivedTimestamp = Clock.getTimestamp();
        }

        public FriendMessage(byte[] bytes)
        {
            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int id_len = reader.ReadInt32();
                    if (id_len > 0)
                    {
                        _id = reader.ReadBytes(id_len);
                    }
                    type = (FriendMessageType)reader.ReadInt32();
                    message = reader.ReadString();
                    timestamp = reader.ReadInt64();
                    localSender = reader.ReadBoolean();
                    read = reader.ReadBoolean();
                    confirmed = reader.ReadBoolean();

                    int sender_address_len = reader.ReadInt32();
                    if (sender_address_len > 0)
                    {
                        senderAddress = reader.ReadBytes(sender_address_len);
                    }

                    senderNick = reader.ReadString();

                    transferId = reader.ReadString();

                    completed = reader.ReadBoolean();

                    filePath = reader.ReadString();
                    fileSize = reader.ReadUInt64();

                    receivedTimestamp = reader.ReadInt64();

                    // try/catch can be removed after upgrade
                    try
                    {
                        if(m.Position < m.Length)
                        {
                            transactionId = reader.ReadString();
                            payableDataLen = reader.ReadInt32();

                            int reaction_count = reader.ReadInt32();
                            for(int i = 0; i < reaction_count; i++)
                            {
                                string reaction_key = reader.ReadString();
                                List<ReactionData> reaction_datas = new List<ReactionData>();
                                reactions.Add(reaction_key, reaction_datas);
                                int reaction_user_count = reader.ReadInt32();
                                for(int j = 0; j < reaction_user_count; j++)
                                {
                                    int rd_len = reader.ReadInt32();
                                    ReactionData reaction_data = new ReactionData(reader.ReadBytes(rd_len));
                                    reaction_datas.Add(reaction_data);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Logging.info("");
                    }
                }
            }

        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id.Length);
                    writer.Write(id);
                    writer.Write((int)type);
                    writer.Write(message);
                    writer.Write(timestamp);
                    writer.Write(localSender);
                    writer.Write(read);
                    writer.Write(confirmed);

                    if (senderAddress != null)
                    {
                        writer.Write(senderAddress.Length);
                        writer.Write(senderAddress);
                    }
                    else
                    {
                        writer.Write((int)0);
                    }

                    writer.Write(senderNick);

                    writer.Write(transferId);
                    writer.Write(completed);

                    writer.Write(filePath);
                    writer.Write(fileSize);

                    writer.Write(receivedTimestamp);

                    writer.Write(transactionId);
                    writer.Write(payableDataLen);

                    lock (reactions)
                    {
                        writer.Write(reactions.Count);
                        foreach (var reaction in reactions)
                        {
                            writer.Write(reaction.Key);
                            writer.Write(reaction.Value.Count);
                            foreach (var reaction_data in reaction.Value)
                            {
                                byte[] rd_bytes = reaction_data.getBytes();
                                writer.Write(rd_bytes.Length);
                                writer.Write(rd_bytes);
                            }
                        }
                    }
                }
                return m.ToArray();
            }
        }


        public byte[] id
        {
            get
            {
                if (_id == null)
                {
                    _id = Guid.NewGuid().ToByteArray(); // Generate a new unique id
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        public bool addReaction(byte[] address, string reaction_data)
        {
            lock(reactions)
            {
                string reaction = reaction_data.Substring(0, reaction_data.IndexOf(':'));
                if(reaction == null || reaction == "")
                {
                    return false;
                }
                string data = null;
                if(reaction_data.Length > reaction.Length + 1)
                {
                    data = reaction_data.Substring(reaction.Length + 1);
                }

                if(!reactions.ContainsKey(reaction))
                {
                    reactions.Add(reaction, new List<ReactionData>());
                }

                if (reactions[reaction].Find(x => x.sender.SequenceEqual(address)) == null)
                {
                    reactions[reaction].Add(new ReactionData(address, data));
                    return true;
                }
            }
            return false;
        }
    }

    // Helper message class used for communicating with the UI
    public class FriendMessageHelper
    {
        public string walletAddress;
        public string nickname;
        public long timestamp;
        public string avatar;
        public string onlineString;
        public string excerpt;
        public int unreadCount;

        public FriendMessageHelper(string wa, string nick, long time, string av, string online, string ex, int unread)
        {
            walletAddress = wa;
            nickname = nick;
            timestamp = time;
            avatar = av;
            onlineString = online;
            excerpt = ex;
            unreadCount = unread;
        }

    }
}
