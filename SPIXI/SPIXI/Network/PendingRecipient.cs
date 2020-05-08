using System.Collections.Generic;

namespace SPIXI.Network
{
    class PendingMessageHeader
    {
        public byte[] id = null;
        public string filePath = null;
        public bool sendToServer = false;
    }
    class PendingRecipient
    {
        public byte[] address = null;
        public List<PendingMessageHeader> messageQueue = new List<PendingMessageHeader>();
        public PendingRecipient(byte[] address)
        {
            this.address = address;
        }
    }
}
