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
    class WalletStorage
    {
        private string filename;

        private int walletVersion = 0;
        private string walletPassword = ""; // TODO TODO TODO TODO wallet password, seed and keys should be encrypted in memory

        private byte[] seedHash = null;
        private byte[] masterSeed = null;
        private byte[] derivedMasterSeed = null;

        private readonly Dictionary<byte[], IxianKeyPair> myKeys = new Dictionary<byte[], IxianKeyPair>(new IXICore.Utils.ByteArrayComparer());
        private readonly Dictionary<byte[], AddressData> myAddresses = new Dictionary<byte[], AddressData>(new IXICore.Utils.ByteArrayComparer());

        private byte[] privateKey = null;
        private byte[] publicKey = null;
        private byte[] address = null;
        private byte[] lastAddress = null;

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

                password = "SPIXI"; // TODO connect with UI

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
        public bool readWallet()
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
        public bool writeWallet(string password)
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
        public bool generateWallet()
        {
            Logging.info("A new wallet will be generated for you.");

            // Request a password
            string password = "SPIXI";  // TODO - integrate with UI

            walletVersion = 1;
            walletPassword = password;

            Logging.log(LogSeverity.info, "Generating primary wallet keys, this may take a while, please wait...");

            //IxianKeyPair kp = generateNewKeyPair(false);
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

        // Obtain the mnemonic address
        public byte[] getWalletAddress()
        {
            return address;
        }


        public byte[] getPrimaryAddress()
        {
            return address;
        }

        public byte[] getPrimaryPrivateKey()
        {
            return privateKey;
        }

        public byte[] getPrimaryPublicKey()
        {
            return publicKey;
        }

        public byte[] getLastAddress()
        {
            // TODO TODO TODO TODO TODO improve if possible for v3 wallets
            // Also you have to take into account what happens when loading from file and the difference between v1 and v2 wallets (key related)
            lock (myAddresses)
            {
                return lastAddress;
            }
        }

        public byte[] getSeedHash()
        {
            return seedHash;
        }

        public IxiNumber getMyTotalBalance(byte[] primary_address)
        {
            IxiNumber balance = 0;
            lock (myAddresses)
            {
                foreach (var entry in myAddresses)
                {
                    if (primary_address != null && !entry.Value.keyPair.addressBytes.SequenceEqual(primary_address))
                    {
                        continue;
                    }
                    IxiNumber amount = Node.walletState.getWalletBalance(entry.Key);
                    if (amount == 0)
                    {
                        continue;
                    }
                    balance += amount;
                }
            }
            return balance;
        }

        public Address generateNewAddress(Address key_primary_address, bool write_to_file = true)
        {
            if (walletVersion < 2)
            {
                return generateNewAddress_v0(key_primary_address, write_to_file);
            }
            else
            {
                return generateNewAddress_v1(key_primary_address, write_to_file);
            }
        }

        public Address generateNewAddress_v0(Address key_primary_address, bool write_to_file = true)
        {
            lock (myKeys)
            {
                if (!myKeys.ContainsKey(key_primary_address.address))
                {
                    return null;
                }

                IxianKeyPair kp = myKeys[key_primary_address.address];

                byte[] base_nonce = Crypto.sha512quTrunc(privateKey, publicKey.Length, 64);

                byte[] last_nonce = kp.lastNonceBytes;

                List<byte> new_nonce = base_nonce.ToList();
                if (last_nonce != null)
                {
                    new_nonce.AddRange(last_nonce);
                }
                kp.lastNonceBytes = Crypto.sha512quTrunc(new_nonce.ToArray(), 0, 0, 16);

                Address new_address = new Address(key_primary_address.address, kp.lastNonceBytes);

                lock (myAddresses)
                {
                    AddressData ad = new AddressData() { nonce = kp.lastNonceBytes, keyPair = kp };
                    myAddresses.Add(new_address.address, ad);
                    lastAddress = new_address.address;
                }

                if (write_to_file)
                {
                    writeWallet(walletPassword);
                }

                return new_address;
            }
        }

        public Address generateNewAddress_v1(Address key_primary_address, bool write_to_file = true)
        {
            lock (myKeys)
            {
                if (!myKeys.ContainsKey(key_primary_address.address))
                {
                    return null;
                }

                IxianKeyPair kp = myKeys[key_primary_address.address];

                byte[] base_nonce = Crypto.sha512sqTrunc(privateKey, publicKey.Length, 64);

                byte[] last_nonce = kp.lastNonceBytes;

                List<byte> new_nonce = base_nonce.ToList();
                if (last_nonce != null)
                {
                    new_nonce.AddRange(last_nonce);
                }
                kp.lastNonceBytes = Crypto.sha512sqTrunc(new_nonce.ToArray(), 0, 0, 16);

                Address new_address = new Address(key_primary_address.address, kp.lastNonceBytes);

                lock (myAddresses)
                {
                    AddressData ad = new AddressData() { nonce = kp.lastNonceBytes, keyPair = kp };
                    myAddresses.Add(new_address.address, ad);
                    lastAddress = new_address.address;
                }

                if (write_to_file)
                {
                    writeWallet(walletPassword);
                }

                return new_address;
            }
        }

        public IxianKeyPair generateNewKeyPair(bool writeToFile = true)
        {
            if (walletVersion < 3)
            {
                lock (myKeys)
                {
                    return myKeys.First().Value;
                }
            }

            IXICore.CryptoKey.KeyDerivation kd = new IXICore.CryptoKey.KeyDerivation(masterSeed);

            int key_count = 0;

            lock (myKeys)
            {
                key_count = myKeys.Count();
            }

            IxianKeyPair kp = kd.deriveKey(key_count, CoreConfig.defaultRsaKeySize, 65537);

            if (kp == null)
            {
                Logging.error("An error occured generating new key pair, unable to derive key.");
                return null;
            }

            if (!DLT.CryptoManager.lib.testKeys(Encoding.Unicode.GetBytes("TEST TEST"), kp))
            {
                Logging.error("An error occured while testing the newly generated keypair, unable to produce a valid address.");
                return null;
            }
            Address addr = new Address(kp.publicKeyBytes);

            if (addr.address == null)
            {
                Logging.error("An error occured generating new key pair, unable to produce a valid address.");
                return null;
            }
            lock (myKeys)
            {
                lock (myAddresses)
                {
                    if (!writeToFile)
                    {
                        myKeys.Add(addr.address, kp);
                        AddressData ad = new AddressData() { nonce = kp.lastNonceBytes, keyPair = kp };
                        myAddresses.Add(addr.address, ad);
                    }
                    else
                    {
                        if (writeWallet(walletPassword))
                        {
                            myKeys.Add(addr.address, kp);
                            AddressData ad = new AddressData() { nonce = kp.lastNonceBytes, keyPair = kp };
                            myAddresses.Add(addr.address, ad);
                        }
                        else
                        {
                            Logging.error("An error occured while writing wallet file.");
                            return null;
                        }
                    }
                }
            }

            return kp;
        }

        public IxianKeyPair getKeyPair(byte[] address)
        {
            lock (myKeys)
            {
                if (myKeys.ContainsKey(address))
                {
                    return myKeys[address];
                }
                return null;
            }
        }

        public AddressData getAddress(byte[] address)
        {
            lock (myAddresses)
            {
                if (myAddresses.ContainsKey(address))
                {
                    return myAddresses[address];
                }
            }
            return null;
        }

        public bool isMyAddress(byte[] address)
        {
            lock (myAddresses)
            {
                if (myAddresses.ContainsKey(address))
                {
                    return true;
                }
            }
            return false;
        }

        public List<byte[]> extractMyAddressesFromAddressList(SortedDictionary<byte[], IxiNumber> address_list)
        {
            lock (myAddresses)
            {
                List<byte[]> found_address_list = new List<byte[]>();
                foreach (var entry in address_list)
                {
                    if (myAddresses.ContainsKey(entry.Key))
                    {
                        found_address_list.Add(entry.Key);
                    }
                }
                if (found_address_list.Count > 0)
                {
                    return found_address_list;
                }
            }
            return null;
        }

        public List<Address> getMyAddresses()
        {
            lock (myAddresses)
            {
                return myAddresses.Select(x => new Address(x.Key)).ToList();
            }
        }

        public List<string> getMyAddressesBase58()
        {
            lock (myAddresses)
            {
                return myAddresses.Select(x => (new Address(x.Key)).ToString()).ToList();
            }
        }

        public SortedDictionary<byte[], IxiNumber> generateFromListFromAddress(byte[] from_address, IxiNumber total_amount_with_fee, bool full_pubkey = false)
        {
            lock (myAddresses)
            {
                SortedDictionary<byte[], IxiNumber> tmp_from_list = new SortedDictionary<byte[], IxiNumber>(new ByteArrayComparer());
                if (full_pubkey)
                {
                    if (!myAddresses.ContainsKey(from_address))
                    {
                        return null;
                    }
                    AddressData ad = myAddresses[from_address];
                    tmp_from_list.Add(ad.nonce, total_amount_with_fee);
                }
                else
                {
                    tmp_from_list.Add(new byte[1] { 0 }, total_amount_with_fee);
                }
                return tmp_from_list;
            }
        }

        public SortedDictionary<byte[], IxiNumber> generateFromList(byte[] primary_address, IxiNumber total_amount_with_fee, List<byte[]> skip_addresses)
        {
            lock (myAddresses)
            {
                Dictionary<byte[], IxiNumber> tmp_from_list = new Dictionary<byte[], IxiNumber>(new ByteArrayComparer());
                foreach (var entry in myAddresses)
                {
                    if (!entry.Value.keyPair.addressBytes.SequenceEqual(primary_address))
                    {
                        continue;
                    }

                    if (skip_addresses.Contains(entry.Value.keyPair.addressBytes, new ByteArrayComparer()))
                    {
                        continue;
                    }

                    DLT.Wallet wallet = Node.walletState.getWallet(entry.Key);
                    if (wallet.type != WalletType.Normal)
                    {
                        continue;
                    }

                    IxiNumber amount = wallet.balance;
                    if (amount == 0)
                    {
                        continue;
                    }

                    tmp_from_list.Add(entry.Value.nonce, amount);
                }

                var tmp_from_list_ordered = tmp_from_list.OrderBy(x => x.Value.getAmount());

                SortedDictionary<byte[], IxiNumber> from_list = new SortedDictionary<byte[], IxiNumber>(new ByteArrayComparer());

                IxiNumber tmp_total_amount = 0;
                foreach (var entry in tmp_from_list_ordered)
                {
                    if (tmp_total_amount + entry.Value >= total_amount_with_fee)
                    {
                        IxiNumber tmp_amount = total_amount_with_fee - tmp_total_amount;
                        from_list.Add(entry.Key, tmp_amount);
                        tmp_total_amount += tmp_amount;
                        break;
                    }
                    from_list.Add(entry.Key, entry.Value);
                    tmp_total_amount += entry.Value;
                }

                if (from_list.Count > 0 && tmp_total_amount == total_amount_with_fee)
                {
                    return from_list;
                }
                return null;
            }
        }
    }
}
