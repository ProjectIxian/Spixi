using DLT.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    class SpixiMessage
    {
        public SpixiMessageCode type;          // Spixi Message type
        public byte[] data = null;             // Actual message data

        public SpixiMessage()
        {
            type = SpixiMessageCode.chat;
            data = null;
        }

        public SpixiMessage(SpixiMessageCode in_type, byte[] in_data)
        {
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
                    // Write the type
                    writer.Write((int)type);

                    // Write the data
                    int data_length = data.Length;
                    writer.Write(data_length);

                    if (data_length > 0)
                        writer.Write(data);
                    else
                        writer.Write(0);
                }
                return m.ToArray();
            }
        }

    }
}
