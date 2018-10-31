using DLT;
using DLT.Meta;
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
                System.Int32 version = reader.ReadInt32();
                privateKey = reader.ReadString();
                publicKey = reader.ReadString();

                Logging.log(LogSeverity.info, String.Format("Wallet File Version: {0}", version));
                Logging.log(LogSeverity.info, String.Format("Private Key: {0}", privateKey));
                Logging.log(LogSeverity.info, String.Format("Public Key: {0}", publicKey));
                Logging.log(LogSeverity.info, String.Format("Public Node Address: {0}", address));

                // Read the enc keypair as well
                if (version > 1)
                {
                    encPrivateKey = reader.ReadString();
                    encPublicKey = reader.ReadString();

                    Logging.log(LogSeverity.info, String.Format("ENC Private Key: {0}", encPrivateKey));
                    Logging.log(LogSeverity.info, String.Format("ENC Public Key: {0}", encPublicKey));
                }
                else
                {
                    // Force generation of wallet
                    return false;
                }

                Address addr = new Address(publicKey);
                address = addr.ToString();

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
                System.Int32 version = 2; // Set the wallet version
                writer.Write(version);
                // Write the address keypair
                writer.Write(privateKey);
                writer.Write(publicKey);
                // Write the encryption keypair
                writer.Write(encPrivateKey);
                writer.Write(encPublicKey);
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
                CryptoManager.lib.generateKeys();
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error generating wallet: {0}", e.ToString()));
                return false;
            }

            privateKey = CryptoManager.lib.getPrivateKey();
            publicKey = CryptoManager.lib.getPublicKey();

            encPrivateKey = CryptoManager.lib.getEncPrivateKey();
            encPublicKey = CryptoManager.lib.getEncPublicKey();

            Address addr = new Address(publicKey);
            address = addr.ToString();

            Logging.log(LogSeverity.info, String.Format("Private Key: {0}", privateKey));
            Logging.log(LogSeverity.info, String.Format("Public Key: {0}", publicKey));

            Logging.log(LogSeverity.info, String.Format("ENC Private Key: {0}", encPrivateKey));
            Logging.log(LogSeverity.info, String.Format("ENC Public Key: {0}", encPublicKey));

            Logging.log(LogSeverity.info, String.Format("Your Address: {0}", address));

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
