using DLT;
using DLT.Meta;
using IXICore;
using IXICore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SPIXI.Wallet
{

    // SPIXI-specific wallet code
    class SpixiWalletStorage : WalletStorage
    {

        public SpixiWalletStorage()
        {
            filename = "spixi.wal"; 
        }

        public SpixiWalletStorage(string file_name)
        {
            // Store the wallet in the system's personal user folder
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            filename = Path.Combine(path, file_name);
        }


        private void readWallet_v1(BinaryReader reader)
        {
            string password = "";

            // Read the encrypted keys
            int b_privateKeyLength = reader.ReadInt32();
            byte[] b_privateKey = reader.ReadBytes(b_privateKeyLength);

            int b_publicKeyLength = reader.ReadInt32();
            byte[] b_publicKey = reader.ReadBytes(b_publicKeyLength);

            byte[] b_last_nonce = null;
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int b_last_nonceLength = reader.ReadInt32();
                b_last_nonce = reader.ReadBytes(b_last_nonceLength);
            }

            bool success = false;
            while (!success)
            {

                password = "SPIXISPIXISPIXI"; // TODO connect with UI

                success = true;
                try
                {
                    // Decrypt
                    privateKey = CryptoManager.lib.decryptWithPassword(b_privateKey, password);
                    publicKey = CryptoManager.lib.decryptWithPassword(b_publicKey, password);
                    walletPassword = password;
                }
                catch (Exception)
                {
                    Logging.error(string.Format("Incorrect password"));
                    Logging.flush();
                    success = false;
                }

            }

            Address addr = new Address(publicKey);
            lastAddress = address = addr.address;

            masterSeed = address;
            seedHash = address;
            derivedMasterSeed = masterSeed;

            IxianKeyPair kp = new IxianKeyPair();
            kp.privateKeyBytes = privateKey;
            kp.publicKeyBytes = publicKey;
            kp.addressBytes = address;
            lock (myKeys)
            {
                myKeys.Add(address, kp);
            }
            lock (myAddresses)
            {
                AddressData ad = new AddressData() { nonce = new byte[1] { 0 }, keyPair = kp };
                myAddresses.Add(address, ad);

                if (b_last_nonce != null)
                {
                    byte[] last_nonce_bytes = CryptoManager.lib.decryptWithPassword(b_last_nonce, walletPassword);
                    bool last_address_found = false;
                    while (last_address_found == false)
                    {
                        if (kp.lastNonceBytes != null && last_nonce_bytes.SequenceEqual(kp.lastNonceBytes))
                        {
                            last_address_found = true;
                        }
                        else
                        {
                            generateNewAddress(addr, false);
                        }
                    }
                }
            }
        }

        private void readWallet_v3(BinaryReader reader)
        {
            // Read the master seed
            int b_master_seed_length = reader.ReadInt32();
            byte[] b_master_seed = reader.ReadBytes(b_master_seed_length);

            string password = "";

            bool success = false;
            while (!success)
            {
                password = "SPIXI"; // TODO connect with UI
                success = true;
                try
                {
                    // Decrypt
                    masterSeed = CryptoManager.lib.decryptWithPassword(b_master_seed, password);
                    seedHash = Crypto.sha512sqTrunc(masterSeed);
                    walletPassword = password;
                }
                catch (Exception)
                {
                    Logging.error(string.Format("Incorrect password"));
                    Logging.flush();
                    success = false;
                }

            }

            int key_count = reader.ReadInt32();
            for (int i = 0; i < key_count; i++)
            {
                int len = reader.ReadInt32();
                if (reader.BaseStream.Position + len > reader.BaseStream.Length)
                {
                    Logging.error("Wallet file is corrupt, expected more data than available.");
                    break;
                }
                byte[] enc_private_key = reader.ReadBytes(len);

                len = reader.ReadInt32();
                if (reader.BaseStream.Position + len > reader.BaseStream.Length)
                {
                    Logging.error("Wallet file is corrupt, expected more data than available.");
                    break;
                }
                byte[] enc_public_key = reader.ReadBytes(len);

                len = reader.ReadInt32();
                if (reader.BaseStream.Position + len > reader.BaseStream.Length)
                {
                    Logging.error("Wallet file is corrupt, expected more data than available.");
                    break;
                }
                byte[] enc_nonce = null;
                if (len > 0)
                {
                    enc_nonce = reader.ReadBytes(len);
                }

                byte[] dec_private_key = CryptoManager.lib.decryptWithPassword(enc_private_key, password);
                byte[] dec_public_key = CryptoManager.lib.decryptWithPassword(enc_public_key, password);
                byte[] tmp_address = (new Address(dec_public_key)).address;

                IxianKeyPair kp = new IxianKeyPair();
                kp.privateKeyBytes = dec_private_key;
                kp.publicKeyBytes = dec_public_key;
                kp.addressBytes = tmp_address;
                if (enc_nonce != null)
                {
                    kp.lastNonceBytes = CryptoManager.lib.decryptWithPassword(enc_nonce, password);
                }

                if (privateKey == null)
                {
                    privateKey = dec_private_key;
                    publicKey = dec_public_key;
                    lastAddress = address = tmp_address;
                }

                lock (myKeys)
                {
                    myKeys.Add(tmp_address, kp);
                }
                lock (myAddresses)
                {
                    AddressData ad = new AddressData() { nonce = new byte[1] { 0 }, keyPair = kp };
                    myAddresses.Add(tmp_address, ad);
                }
            }

            int seed_len = reader.ReadInt32();
            byte[] enc_derived_seed = reader.ReadBytes(seed_len);
            derivedMasterSeed = CryptoManager.lib.decryptWithPassword(enc_derived_seed, password);
        }

        // Try to read wallet information from the file
        new public bool readWallet()
        {
            if (File.Exists(filename) == false)
            {
                Logging.log(LogSeverity.error, "Cannot read wallet file.");

                // Don't generate a new wallet for spixi
                return false;
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
                walletVersion = reader.ReadInt32();
                if (walletVersion == 1 || walletVersion == 2)
                {
                    readWallet_v1(reader);
                }
                else if (walletVersion == 3)
                {
                    readWallet_v3(reader);
                }
                else
                {
                    Logging.error("Unknown wallet version {0}", walletVersion);
                    walletVersion = 0;
                    return false;
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
        private bool writeWallet_v1(string password)
        {
            if (password.Length < 10)
                return false;

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
                Logging.error("Cannot create wallet file. {0}", e.Message);
                return false;
            }

            try
            {
                writer.Write(walletVersion);

                // Write the address keypair
                writer.Write(b_privateKey.Length);
                writer.Write(b_privateKey);

                writer.Write(b_publicKey.Length);
                writer.Write(b_publicKey);

                if (myKeys.First().Value.lastNonceBytes != null)
                {
                    byte[] b_last_nonce = CryptoManager.lib.encryptWithPassword(myKeys.First().Value.lastNonceBytes, password);
                    writer.Write(b_last_nonce.Length);
                    writer.Write(b_last_nonce);
                }

            }

            catch (IOException e)
            {
                Logging.error("Cannot write to wallet file. {0}", e.Message);
                return false;
            }

            writer.Close();

            return true;
        }

        // Write the wallet to the file
        private bool writeWallet_v3(string password)
        {
            if (password.Length < 10)
                return false;

            BinaryWriter writer;
            try
            {
                writer = new BinaryWriter(new FileStream(filename, FileMode.Create));
            }
            catch (IOException e)
            {
                Logging.error("Cannot create wallet file. {0}", e.Message);
                return false;
            }

            try
            {
                writer.Write(walletVersion);

                // Write the master seed
                byte[] enc_master_seed = CryptoManager.lib.encryptWithPassword(masterSeed, password);
                writer.Write(enc_master_seed.Length);
                writer.Write(enc_master_seed);

                lock (myKeys)
                {
                    writer.Write(myKeys.Count());

                    foreach (var entry in myKeys)
                    {
                        byte[] enc_private_key = CryptoManager.lib.encryptWithPassword(entry.Value.privateKeyBytes, password);
                        writer.Write(enc_private_key.Length);
                        writer.Write(enc_private_key);

                        byte[] enc_public_key = CryptoManager.lib.encryptWithPassword(entry.Value.publicKeyBytes, password);
                        writer.Write(enc_public_key.Length);
                        writer.Write(enc_public_key);

                        if (entry.Value.lastNonceBytes != null)
                        {
                            byte[] enc_nonce = CryptoManager.lib.encryptWithPassword(entry.Value.lastNonceBytes, password);
                            writer.Write(enc_nonce.Length);
                            writer.Write(enc_nonce);
                        }
                        else
                        {
                            writer.Write((int)0);
                        }
                    }
                }

                byte[] enc_derived_master_seed = CryptoManager.lib.encryptWithPassword(derivedMasterSeed, password);
                writer.Write(enc_derived_master_seed.Length);
                writer.Write(enc_derived_master_seed);
            }

            catch (IOException e)
            {
                Logging.error("Cannot write to wallet file. {0}", e.Message);
                return false;
            }

            writer.Close();

            return true;
        }

        // Write the wallet to the file
        new public bool writeWallet(string password)
        {
            if (walletVersion == 1 || walletVersion == 2)
            {
                return writeWallet_v1(walletPassword);
            }
            if (walletVersion == 3)
            {
                return writeWallet_v3(walletPassword);
            }
            return false;
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
        public bool generateWallet(string pass)
        {
            Logging.info("A new wallet will be generated for you.");

            // Request a password
            string password = pass;

            walletVersion = 1;
            walletPassword = password;

            Logging.log(LogSeverity.info, "Generating primary wallet keys, this may take a while, please wait...");

            IxianKeyPair kp = CryptoManager.lib.generateKeys(CoreConfig.defaultRsaKeySize, true);

            if (kp == null)
            {
                Logging.error("Error creating wallet, unable to generate a new keypair.");
                return false;
            }

            privateKey = kp.privateKeyBytes;
            if (walletVersion <= 1)
            {
                publicKey = kp.publicKeyBytes;
            }
            else
            {
                List<byte> tmp_pub_key = kp.publicKeyBytes.ToList();
                tmp_pub_key.Insert(0, 1); // prepend address version
                publicKey = tmp_pub_key.ToArray();
            }

            Address addr = new Address(publicKey);
            lastAddress = address = addr.address;

            masterSeed = address;
            seedHash = address;
            derivedMasterSeed = masterSeed;

            kp.addressBytes = address;

            myKeys.Add(address, kp);
            myAddresses.Add(address, new AddressData() { keyPair = kp, nonce = new byte[1] { 0 } });


            Logging.info(String.Format("Public Key: {0}", Crypto.hashToString(publicKey)));
            Logging.info(String.Format("Public Node Address: {0}", Base58Check.Base58CheckEncoding.EncodePlain(address)));

            // Write the new wallet data to the file
            return writeWallet(password);
        }

    }
}
