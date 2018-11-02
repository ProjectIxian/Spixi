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

        public byte[] privateKey = null;
        public byte[] publicKey = null;
        public byte[] address = null;

        public WalletStorage()
        {
            filename = "ixian.wal";
            //readWallet();
        }

        public WalletStorage(string file_name)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            filename = Path.Combine(path, file_name);
            //readWallet();
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

                if (version != 1)
                {
                    Logging.error(string.Format("Wallet version mismatch, expecting {0}, got {1}", 1, version));
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

                Console.WriteLine();
                Console.Write("Your IXIAN address is ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(address);
                Console.ResetColor();
                Console.WriteLine();

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
            // TODOSPIXI
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
                System.Int32 version = 1; // Set the wallet version
                writer.Write(version);

                // Write the address keypair
                writer.Write(b_privateKey.Length);
                writer.Write(b_privateKey);

                writer.Write(b_publicKey.Length);
                writer.Write(b_publicKey);
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
                CryptoManager.lib.generateKeys(CoreConfig.defaultRsaKeySize);
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error generating wallet: {0}", e.ToString()));
                return false;
            }

            privateKey = CryptoManager.lib.getPrivateKey();
            publicKey = CryptoManager.lib.getPublicKey();

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
