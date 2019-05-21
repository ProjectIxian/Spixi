using DLT;
using DLT.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SPIXI
{
    // The message codes available in S2.
    // Error and Info are free, while data requires a transaction
    public enum StreamMessageCode
    {
        error,      // Reserved for S2 nodes only
        info,       // Free, limited message type
        data        // Paid, transaction-based type
    }

    class StreamMessage
    {
        public StreamMessageCode type;          // Stream Message type
        public byte[] sender = null;            // Sender wallet
        public byte[] recipient = null;         // Recipient wallet 

        public byte[] transaction = null;       // Unsigned transaction
        public byte[] data = null;              // Actual message data, encrypted
        public byte[] sigdata = null;           // Signature data, encrypted

        private string id;                      // Message unique id

        public StreamMessage()
        {
            id = Guid.NewGuid().ToString(); // Generate a new unique id
            type = StreamMessageCode.info;
            sender = null;
            recipient = null;
            transaction = null;
            data = null;
            sigdata = null;
        }

        public StreamMessage(byte[] bytes)
        {
            try
            {
                using (MemoryStream m = new MemoryStream(bytes))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        id = reader.ReadString();

                        int message_type = reader.ReadInt32();
                        type = (StreamMessageCode)message_type;

                        int sender_length = reader.ReadInt32();
                        if (sender_length > 0)
                            sender = reader.ReadBytes(sender_length);

                        int recipient_length = reader.ReadInt32();
                        if (recipient_length > 0)
                            recipient = reader.ReadBytes(recipient_length);

                        int data_length = reader.ReadInt32();
                        if (data_length > 0)
                            data = reader.ReadBytes(data_length);

                        int tx_length = reader.ReadInt32();
                        if (tx_length > 0)
                            transaction = reader.ReadBytes(tx_length);

                        int sig_length = reader.ReadInt32();
                        if (sig_length > 0)
                            sigdata = reader.ReadBytes(sig_length);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while trying to construct StreamMessage from bytes: " + e);
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id);

                    // Write the type
                    writer.Write((int)type);

                    // Write the sender
                    int sender_length = sender.Length;
                    writer.Write(sender_length);

                    if (sender_length > 0)
                        writer.Write(sender);
                    else
                        writer.Write(0);


                    // Write the recipient
                    int recipient_length = recipient.Length;
                    writer.Write(recipient_length);

                    if (recipient_length > 0)
                        writer.Write(recipient);
                    else
                        writer.Write(0);


                    // Write the data
                    int data_length = data.Length;
                    writer.Write(data_length);

                    if (data_length > 0)
                        writer.Write(data);
                    else
                        writer.Write(0);

                    // Write the tx
                    int tx_length = transaction.Length;
                    writer.Write(tx_length);

                    if (tx_length > 0)
                        writer.Write(transaction);
                    else
                        writer.Write(0);


                    // Write the sig
                    int sig_length = sigdata.Length;
                    writer.Write(sig_length);

                    if (sig_length > 0)
                        writer.Write(sigdata);
                    else
                        writer.Write(0);

                }
                return m.ToArray();
            }
        }

        // Returns the stream message id
        public string getID()
        {
            return id;
        }

        // Encrypts a provided message with aes, then chacha based on the keys provided
        public bool encryptMessage(byte[] message, string aesPassword, byte[] chachaKey)
        {
            byte[] aes_encrypted = CryptoManager.lib.encryptWithPassword(message, aesPassword);
            byte[] chacha_encrypted = CryptoManager.lib.encryptWithChacha(aes_encrypted, chachaKey);
            data = chacha_encrypted.ToArray();
            return true;
        }

        // Encrypts a provided signature with aes, then chacha based on the keys provided
        public bool encryptSignature(byte[] signature, string aesPassword, byte[] chachaKey)
        {
            byte[] aes_encrypted = CryptoManager.lib.encryptWithPassword(signature, aesPassword);
            byte[] chacha_encrypted = CryptoManager.lib.encryptWithChacha(aes_encrypted, chachaKey);
            sigdata = chacha_encrypted.ToArray();
            return true;
        }


    }
}
