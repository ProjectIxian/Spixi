using System.IO;

namespace SPIXI
{
    public class ReactionData
    {
        byte[] sender = null;
        string data = null;

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
                    writer.Write(data);
                }
                return m.ToArray();
            }
        }
    }
}
