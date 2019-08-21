using IXICore.Meta;
using System;
using System.IO;

namespace SPIXI
{
    public enum SpixiMessageCode
    {
        chat,
        getNick,
        nick,
        requestAdd,
        acceptAdd,
        sentFunds,
        requestFunds,
        keys,
        msgRead,
        msgReceived, // this code will likely be replaced by payment to S2
        fileData,
        requestFileData,
        fileHeader,
        acceptFile,
        requestCall,
        acceptCall,
        rejectCall,
        callData
    }

    class SpixiMessage
    {
        public byte[] id;
        public SpixiMessageCode type;          // Spixi Message type
        public byte[] data = null;             // Actual message data

        public SpixiMessage()
        {
            id = null;
            type = SpixiMessageCode.chat;
            data = null;
        }

        public SpixiMessage(byte[] in_id, SpixiMessageCode in_type, byte[] in_data)
        {
            id = in_id;
            type = in_type;
            data = in_data;
        }

        public SpixiMessage(byte[] bytes)
        {
            try
            {
                using (MemoryStream m = new MemoryStream(bytes))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        int id_len = reader.ReadInt32();
                        if (id_len > 0)
                        {
                            id = reader.ReadBytes(id_len);
                        }

                        int message_type = reader.ReadInt32();
                        type = (SpixiMessageCode)message_type;

                        int data_length = reader.ReadInt32();
                        if (data_length > 0)
                            data = reader.ReadBytes(data_length);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while trying to construct SpixiMessage from bytes: " + e);
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    // Write the id
                    if (id != null)
                    {
                        writer.Write(id.Length);
                        writer.Write(id);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    // Write the type
                    writer.Write((int)type);

                    // Write the data
                    if (data != null)
                    {
                        writer.Write(data.Length);
                        writer.Write(data);
                    }else
                    {
                        writer.Write(0);
                    }
                }
                return m.ToArray();
            }
        }

    }
}
