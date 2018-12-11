using DLT;
using DLT.Meta;
using IXICore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SPIXI.Wallet
{
    // SPIXI-specific wallet code
    class WalletStorage
    {
        private string filename;

        // Version 1 parameters
        public byte[] privateKey = null;
        public byte[] publicKey = null;
        public byte[] address = null;

        // Version 2 parameters
        public byte[] passwordHash = null;

        private static readonly int WALLET_VERSION = 2;

        public WalletStorage()
        {
            filename = "spixi.wal"; 
        }

        public WalletStorage(string file_name)
        {
            // Store the wallet in the system's personal user folder
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            filename = Path.Combine(path, file_name);
        }

        // Try to read wallet information from the file
        public bool readWallet()
        {
            if (File.Exists(filename) == false)
            {
                Logging.log(LogSeverity.error, "Cannot read wallet file.");

                // Generate a new wallet
                return false;// generateWallet();
            }

            Logging.log(LogSeverity.info, "Wallet file found, reading data...");

            BinaryReader reader;

            try
            {
                reader = new BinaryReader(new FileStream(filename, FileMode.Open));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot open wallet file. {0}", e.Message));
                return false;
            }

            try
            {
                // Read the wallet version
                System.Int32 version = reader.ReadInt32();

                if (version > WALLET_VERSION)
                {
                    Logging.error(string.Format("Wallet version mismatch, expecting {0}, got {1}", WALLET_VERSION, version));
                    return false;
                }

                // Read the encrypted keys
                int b_privateKeyLength = reader.ReadInt32();
                byte[] b_privateKey = reader.ReadBytes(b_privateKeyLength);

                int b_publicKeyLength = reader.ReadInt32();
                byte[] b_publicKey = reader.ReadBytes(b_publicKeyLength);

                // TODOSPIXI
                string password = "SPIXI";

                // Decrypt
                privateKey = CryptoManager.lib.decryptWithPassword(b_privateKey, password);
                publicKey = CryptoManager.lib.decryptWithPassword(b_publicKey, password);

                Address addr = new Address(publicKey);
                address = addr.address;


                // Read the lock password hash if found
                passwordHash = null;
                if (version > 1) // Version 1 does not include password lock
                {
                    int b_passwordHashLength = reader.ReadInt32();
                    if(b_passwordHashLength > 0)
                    {
                        // Password hash found, read it and decrypt it
                        byte[] b_passwordHash = reader.ReadBytes(b_passwordHashLength);
                        passwordHash = CryptoManager.lib.decryptWithPassword(b_passwordHash, password);
                    }
                }

            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot read from wallet file. {0}", e.Message));
                return false;
            }

            reader.Close();

            return true;
        }

        // Write the wallet to the file
        private bool writeWallet()
        {
            // TODOSPIXI replace with wallet password
            string password = "SPIXI";

            // Encrypt data first
            byte[] b_privateKey = CryptoManager.lib.encryptWithPassword(privateKey, password);
            byte[] b_publicKey = CryptoManager.lib.encryptWithPassword(publicKey, password);

            BinaryWriter writer;

            try
            {
                writer = new BinaryWriter(new FileStream(filename, FileMode.Create));
            }
            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot create wallet file. {0}", e.Message));
                return false;
            }

            try
            {
                System.Int32 version = WALLET_VERSION; // Set the wallet version
                writer.Write(version);

                // Write the address keypair
                writer.Write(b_privateKey.Length);
                writer.Write(b_privateKey);

                writer.Write(b_publicKey.Length);
                writer.Write(b_publicKey);

                // Write the password hash if set
                if (passwordHash == null)
                {
                    // No password hash, set the length to 0
                    writer.Write(0);
                }
                else
                {
                    // Encrypt and write the password hash
                    byte[] b_passwordHash = CryptoManager.lib.encryptWithPassword(passwordHash, password);
                    writer.Write(b_passwordHash.Length);
                    writer.Write(b_passwordHash);
                }

            }

            catch (IOException e)
            {
                Logging.log(LogSeverity.error, String.Format("Cannot write to wallet file. {0}", e.Message));
                return false;
            }

            writer.Close();

            return true;
        }

        // Deletes the wallet file if it exists
        public bool deleteWallet()
        {
            if (File.Exists(filename) == false)
            {
                return false;
            }

            File.Delete(filename);
            return true;
        }

        // Generate a new wallet with matching private/public key pairs
        public bool generateWallet()
        {
            Logging.log(LogSeverity.info, "Generating new wallet keys...");

            // Generate the private and public key pair
            try
            {
                if(!CryptoManager.lib.generateKeys(CoreConfig.defaultRsaKeySize))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error generating wallet: {0}", e.ToString()));
                return false;
            }

            privateKey = CryptoManager.lib.getPrivateKey();
            publicKey = CryptoManager.lib.getPublicKey();

            // Set the password hash to null
            passwordHash = null;

            Address addr = new Address(publicKey);
            address = addr.address;

            Logging.info(String.Format("Public Key: {0}", Crypto.hashToString(publicKey)));
            Logging.info(String.Format("Public Node Address: {0}", Base58Check.Base58CheckEncoding.EncodePlain(address)));

            // Write the new wallet data to the file
            return writeWallet();
        }

        // Obtain the mnemonic address
        public byte[] getWalletAddress()
        {
            return address;
        }
    }
}
