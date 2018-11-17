using DLT;
using DLT.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SPIXI
{
    public enum StreamMessageCode
    {
        error,
        chat,
        getNick,
        nick,
        requestAdd,
        acceptAdd,
        requestFunds
    }

    class StreamMessage
    {
        public StreamMessageCode type;  // Stream Message type
        public byte[] sender;           // Sender wallet
        public byte[] recipient;        // Recipient wallet 

        public byte[] transaction;      // Unsigned transaction
        public byte[] data;             // Actual message data, encrypted
        public byte[] sigdata;          // Signature data, encrypted

        private string id;              // Message unique id

        public StreamMessage()
        {
            id = Guid.NewGuid().ToString(); // Generate a new unique id
            type = StreamMessageCode.chat;
            sender = Node.walletStorage.address;
        }

        public StreamMessage(byte[] bytes)
        {
            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    id = reader.ReadString();

                    int message_type = reader.ReadInt32();
                    type = (StreamMessageCode)message_type;

                    int sender_length = reader.ReadInt32();
                    sender = reader.ReadBytes(sender_length);

                    int recipient_length = reader.ReadInt32();
                    recipient = reader.ReadBytes(recipient_length);

                    int data_length = reader.ReadInt32();
                    data = reader.ReadBytes(data_length);

                    int tx_length = reader.ReadInt32();
                    transaction = reader.ReadBytes(tx_length);

                    int sig_length = reader.ReadInt32();
                    sigdata = reader.ReadBytes(sig_length);
                }
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
