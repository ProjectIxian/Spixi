using IXICore;
using IXICore.Meta;
using System;
using System.IO;
using System.Linq;

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

    // The encryption message codes available in S2.
    public enum StreamMessageEncryptionCode
    {
        none,
        rsa,
        spixi1
    }

    class StreamMessage
    {
        public StreamMessageCode type;          // Stream Message type
        public byte[] sender = null;            // Sender wallet
        public byte[] recipient = null;         // Recipient wallet 

        public byte[] transaction = null;       // Unsigned transaction
        public byte[] data = null;              // Actual message data, encrypted
        public byte[] sigdata = null;           // Signature data, encrypted

        public StreamMessageEncryptionCode encryptionType;

        public bool encrypted = false; // used locally to avoid double encryption of data
        public bool sigEncrypted = false; // used locally to avoid double encryption of tx sig

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
            encryptionType = StreamMessageEncryptionCode.spixi1;
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

                        int encryption_type = reader.ReadInt32();
                        encryptionType = (StreamMessageEncryptionCode)encryption_type;

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

                        encrypted = reader.ReadBoolean();
                        sigEncrypted = reader.ReadBoolean();
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

                    // Write the encryption type
                    writer.Write((int)encryptionType);

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

                    writer.Write(encrypted);
                    writer.Write(sigEncrypted);

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
        public bool encrypt(byte[] public_key, byte[] aes_password, byte[] chacha_key)
        {
            if(encrypted)
            {
                return true;
            }
            byte[] encrypted_data = _encrypt(data, public_key, aes_password, chacha_key);
            if(encrypted_data != null)
            {
                data = encrypted_data;
                encrypted = true;
                return true;
            }
            return false;
        }

        public bool decrypt(byte[] private_key, byte[] aes_key, byte[] chacha_key)
        {
            byte[] decrypted_data = _decrypt(data, private_key, aes_key, chacha_key);
            if (decrypted_data != null)
            {
                data = decrypted_data;
                return true;
            }
            return false;
        }

        // Encrypts a provided signature with aes, then chacha based on the keys provided
        public bool encryptSignature(byte[] public_key, byte[] aes_password, byte[] chacha_key)
        {
            if (sigEncrypted)
            {
                return true;
            }
            byte[] encrypted_data = _encrypt(sigdata, public_key, aes_password, chacha_key);
            if (encrypted_data != null)
            {
                sigdata = encrypted_data;
                sigEncrypted = true;
                return true;
            }
            return false;
        }

        public bool decryptSignature(byte[] private_key, byte[] aes_key, byte[] chacha_key)
        {
            byte[] decrypted_data = _decrypt(sigdata, private_key, aes_key, chacha_key);
            if (decrypted_data != null)
            {
                sigdata = decrypted_data;
                return true;
            }
            return false;
        }

        private byte[] _encrypt(byte[] data_to_encrypt, byte[] public_key, byte[] aes_key, byte[] chacha_key)
        {
            if (encryptionType == StreamMessageEncryptionCode.spixi1)
            {
                if (aes_key != null && chacha_key != null)
                {
                    byte[] aes_encrypted = CryptoManager.lib.encryptDataAES(data_to_encrypt, aes_key);
                    byte[] chacha_encrypted = CryptoManager.lib.encryptWithChacha(aes_encrypted, chacha_key);
                    return chacha_encrypted;
                }
                else
                {
                    Logging.error("Cannot encrypt message, no AES and CHACHA keys were provided.");
                }
            }
            else if (encryptionType == StreamMessageEncryptionCode.rsa)
            {
                if (public_key != null)
                {
                    return CryptoManager.lib.encryptWithRSA(data_to_encrypt, public_key);
                }
                else
                {
                    Logging.error("Cannot encrypt message, no RSA key was provided.");
                }
            }
            else
            {
                Logging.error("Cannot encrypt message, invalid encryption type {0} was specified.", encryptionType);
            }
            return null;
        }

        private byte[] _decrypt(byte[] data_to_decrypt, byte[] private_key, byte[] aes_key, byte[] chacha_key)
        {
            if(encryptionType == StreamMessageEncryptionCode.spixi1)
            {
                if (aes_key != null && chacha_key != null)
                {
                    byte[] chacha_decrypted = CryptoManager.lib.decryptWithChacha(data_to_decrypt, chacha_key);
                    byte[] aes_decrypted = CryptoManager.lib.decryptDataAES(chacha_decrypted, aes_key);
                    return aes_decrypted;
                }else
                {
                    Logging.error("Cannot decrypt message, no AES and CHACHA keys were provided.");
                }
            }
            else if (encryptionType == StreamMessageEncryptionCode.rsa)
            {
                if(private_key != null)
                {
                    return CryptoManager.lib.decryptWithRSA(data_to_decrypt, private_key);
                }else
                {
                    Logging.error("Cannot decrypt message, no RSA key was provided.");
                }
            }
            else
            {
                Logging.error("Cannot decrypt message, invalid encryption type {0} was specified.", encryptionType);
            }
            return null;
        }

    }
}
