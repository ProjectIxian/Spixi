using IXICore;
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
        public Address address = null;
        public List<PendingMessageHeader> messageQueue = new List<PendingMessageHeader>();
        public PendingRecipient(Address address)
        {
            this.address = address;
        }
    }
}
