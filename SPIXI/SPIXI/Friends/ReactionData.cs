using System.IO;

namespace SPIXI
{
    public class ReactionData
    {
        public byte[] sender = null;
        public string data = null;

        public ReactionData(byte[] sender, string data)
        {
            this.sender = sender;
            this.data = data;
        }

        public ReactionData(byte[] contact_bytes)
        {
            using (MemoryStream m = new MemoryStream(contact_bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int sender_len = reader.ReadInt32();
                    sender = reader.ReadBytes(sender_len);
                    data = reader.ReadString();
                    if(data == "")
                    {
                        data = null;
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
                    writer.Write(sender.Length);
                    writer.Write(sender);
                    if (data != null)
                    {
                        writer.Write(data);
                    }else
                    {
                        writer.Write("");
                    }
                }
                return m.ToArray();
            }
        }
    }
}
