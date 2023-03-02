using IXICore;
using IXICore.Utils;
using Org.BouncyCastle.Utilities;
using System.Reflection.PortableExecutable;

namespace Spixi.Storage.Models
{
    public class StorageTransaction
    {
        // Corresponding Ixian transaction
        public Transaction transaction;

        // Additional details
        public bool confirmed = false;
        public ulong timeStamp = 0;
        public IxiNumber fiatValue = 0;

        public StorageTransaction() 
        { 
        }

        public StorageTransaction(StorageTransaction tx)
        {
            transaction = tx.transaction;
            confirmed = tx.confirmed;
            timeStamp = tx.timeStamp;
            fiatValue = tx.fiatValue;
        }

        public StorageTransaction(Transaction tx)
        {
            transaction = tx;
            if (transaction.timeStamp == 0)
            {
                Block bh = BlockHeaderStorage.getBlockHeader(transaction.applied);
                if (bh != null)
                {
                    transaction.timeStamp = bh.timestamp;
                }
                else
                {
                    transaction.timeStamp = Clock.getTimestamp();
                }
            }
            timeStamp = (ulong)transaction.timeStamp;
        }

        public StorageTransaction(byte[] bytes)
        {
            using MemoryStream m = new(bytes);
            using BinaryReader reader = new(m);

            int data_length = reader.ReadInt32();
            byte[] data = reader.ReadBytes(data_length);
            transaction = new Transaction(data, true);

            timeStamp = reader.ReadIxiVarUInt();
            fiatValue = reader.ReadIxiNumber();

            // Backward compatibility, add timestamp to the IXI transaction itself while in storage
            transaction.timeStamp = (long)timeStamp;
        }

        public byte[] getBytes()
        {
            using MemoryStream m = new();
            using BinaryWriter writer = new(m);

            byte[] data = transaction.getBytes(true);
            int data_length = data.Length;
            writer.Write(data_length);
            writer.Write(data);

            writer.WriteIxiVarInt(timeStamp);
            writer.WriteIxiNumber(fiatValue);

            return m.ToArray();
        }

    }
}
