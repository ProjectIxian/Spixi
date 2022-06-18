// Copyright (C) 2017-2022 Ixian OU
// This file is part of Spixi - www.github.com/ProjectIxian/Spixi
//
// Ixian Core is free software: you can redistribute it and/or modify
// it under the terms of the MIT License as published
// by the Open Source Initiative.
//
// Ixian Core is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//

using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Engines;
using System.Security.Cryptography;
using Java.Security;
using Java.Security.Spec;
using System.Text;
using Javax.Crypto;
using IXICore;
using IXICore.Meta;

namespace CryptoLibs
{
    class BouncyCastleAndroid : ICryptoLib
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        // Private variables used for AES key expansion
        private int PBKDF2_iterations = 10000;
        private string AES_algorithm = "AES/CBC/PKCS7Padding";
        private string AES_GCM_algorithm = "AES/GCM/NoPadding";

        // Private variables used for Chacha
        private readonly int chacha_rounds = 20;

        // Private variables used for SHA-3
        [ThreadStatic]
        static Org.BouncyCastle.Crypto.Digests.Sha3Digest sha3Algorithm512 = null;

        public BouncyCastleAndroid()
        {
        }

        private Java.Math.BigInteger bigEndianToLittleEndian(byte[] input)
        {
            return new Java.Math.BigInteger(input.Prepend((byte)0).ToArray());
        }

        private byte[] littleEndianToBigEndian(Java.Math.BigInteger bigInt)
        {
            byte[] input = bigInt.ToByteArray();
            if (input[0] == 0x00)
            {
                return input.Skip(1).ToArray();
            }

            return input;
        }

        private byte[] rsaKeyToBytes(KeyPair rsaKey, bool includePrivateParameters, int version)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add((byte)version); // add version
            bytes.AddRange(BitConverter.GetBytes((int)0)); // prepend pub key version

            KeyFactory kf = KeyFactory.GetInstance("RSA");
            // the ToByteArray() function returns big-endian bytes, we need little-endian
            if (includePrivateParameters)
            {
                RSAPrivateCrtKeySpec rsaParams = (RSAPrivateCrtKeySpec)kf.GetKeySpec(rsaKey.Private, Java.Lang.Class.FromType(typeof(RSAPrivateCrtKeySpec)));
                byte[] modulus = littleEndianToBigEndian(rsaParams.Modulus);
                bytes.AddRange(BitConverter.GetBytes(modulus.Length));
                bytes.AddRange(modulus);

                byte[] publicExponent = littleEndianToBigEndian(rsaParams.PublicExponent);
                bytes.AddRange(BitConverter.GetBytes(publicExponent.Length));
                bytes.AddRange(publicExponent);


                byte[] primeP = littleEndianToBigEndian(rsaParams.PrimeP);
                bytes.AddRange(BitConverter.GetBytes(primeP.Length));
                bytes.AddRange(primeP);

                byte[] primeQ = littleEndianToBigEndian(rsaParams.PrimeQ);
                bytes.AddRange(BitConverter.GetBytes(primeQ.Length));
                bytes.AddRange(primeQ);

                byte[] primeExponentP = littleEndianToBigEndian(rsaParams.PrimeExponentP);
                bytes.AddRange(BitConverter.GetBytes(primeExponentP.Length));
                bytes.AddRange(primeExponentP);

                byte[] primeExponentQ = littleEndianToBigEndian(rsaParams.PrimeExponentQ);
                bytes.AddRange(BitConverter.GetBytes(primeExponentQ.Length));
                bytes.AddRange(primeExponentQ);

                byte[] crtCoefficient = littleEndianToBigEndian(rsaParams.CrtCoefficient);
                bytes.AddRange(BitConverter.GetBytes(crtCoefficient.Length));
                bytes.AddRange(crtCoefficient);

                byte[] privateExponent = littleEndianToBigEndian(rsaParams.PrivateExponent);
                bytes.AddRange(BitConverter.GetBytes(privateExponent.Length));
                bytes.AddRange(privateExponent);
            }
            else
            {
                RSAPublicKeySpec rsaPubParams = (RSAPublicKeySpec)kf.GetKeySpec(rsaKey.Public, Java.Lang.Class.FromType(typeof(RSAPublicKeySpec)));
                byte[] modulus = littleEndianToBigEndian(rsaPubParams.Modulus);
                bytes.AddRange(BitConverter.GetBytes(modulus.Length));
                bytes.AddRange(modulus);

                byte[] publicExponent = littleEndianToBigEndian(rsaPubParams.PublicExponent);
                bytes.AddRange(BitConverter.GetBytes(publicExponent.Length));
                bytes.AddRange(publicExponent);
            }

            return bytes.ToArray();
        }

        private KeyPair rsaKeyFromBytes(byte[] keyBytes)
        {
            try
            {

                int offset = 0;
                int dataLen = 0;
                int version = 0;

                if (keyBytes.Length != 523 && keyBytes.Length != 2339)
                {
                    offset += 1; // skip address version
                    version = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                }

                dataLen = BitConverter.ToInt32(keyBytes, offset);
                offset += 4;
                Java.Math.BigInteger modulus = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                offset += dataLen;

                dataLen = BitConverter.ToInt32(keyBytes, offset);
                offset += 4;
                Java.Math.BigInteger exponent = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                offset += dataLen;

                RSAPrivateCrtKeySpec privKeySpec = null;

                if (keyBytes.Length > offset)
                {
                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger P = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger Q = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger DP = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger DQ = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger InverseQ = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    Java.Math.BigInteger D = bigEndianToLittleEndian(keyBytes.Skip(offset).Take(dataLen).ToArray());
                    offset += dataLen;
                    privKeySpec = new RSAPrivateCrtKeySpec(modulus, exponent, D, P, Q, DP, DQ, InverseQ);
                }

                RSAPublicKeySpec pubKeySpec = new RSAPublicKeySpec(modulus, exponent);

                KeyFactory keyFactory = KeyFactory.GetInstance("RSA");
                IPublicKey pubKey = keyFactory.GeneratePublic(pubKeySpec);
                IPrivateKey privKey = null;
                if (privKeySpec != null)
                {
                    privKey = keyFactory.GeneratePrivate(privKeySpec);
                }

                return new KeyPair(pubKey, privKey);
            }
            catch (Exception)
            {
                Logging.warn("An exception occurred while trying to reconstruct PKI from bytes");
            }
            return null;
        }

        public bool testKeys(byte[] plain, IxianKeyPair key_pair)
        {
            Logging.info("Testing generated keys.");
            // Try if RSACryptoServiceProvider considers them a valid key
            if (rsaKeyFromBytes(key_pair.privateKeyBytes) == null)
            {
                Logging.warn("RSA key is considered invalid by RSACryptoServiceProvider!");
                return false;
            }

            byte[] encrypted = encryptWithRSA(plain, key_pair.publicKeyBytes);
            byte[] signature = getSignature(plain, key_pair.privateKeyBytes);

            if (!decryptWithRSA(encrypted, key_pair.privateKeyBytes).SequenceEqual(plain))
            {
                Logging.warn("Error decrypting data while testing keys.");
                return false;
            }

            if (!verifySignature(plain, key_pair.publicKeyBytes, signature))
            {
                Logging.warn("Error verifying signature while testing keys.");
                return false;
            }


            return true;
        }

        // Generates keys for RSA signing
        public IxianKeyPair generateKeys(int keySize, int version)
        {
            KeyPair kp = null;
            try
            {
                KeyPairGenerator kpg = KeyPairGenerator.GetInstance("RSA");
                kpg.Initialize(keySize);
                kp = kpg.GenKeyPair();
                IxianKeyPair ixi_kp = new IxianKeyPair();
                ixi_kp.privateKeyBytes = rsaKeyToBytes(kp, true, version);
                ixi_kp.publicKeyBytes = rsaKeyToBytes(kp, false, version);

                byte[] plain = Encoding.UTF8.GetBytes("Plain text string");
                if (!testKeys(plain, ixi_kp))
                {
                    return null;
                }
                return ixi_kp;
            }
            catch (Exception e)
            {
                Logging.warn("Exception while generating signature keys: {0}", e.ToString());
                return null;
            }
        }

        public byte[] getSignature(byte[] input_data, byte[] privateKey)
        {
            try
            {
                KeyPair kp = rsaKeyFromBytes(privateKey);

                Signature sig = Signature.GetInstance("SHA512withRSA");
                sig.InitSign(kp.Private);
                sig.Update(input_data);
                byte[] signature = sig.Sign();
                return signature;
            }
            catch (Exception e)
            {
                Logging.warn("Cannot generate signature: {0}", e.Message);
            }
            return null;
        }

        public bool verifySignature(byte[] input_data, byte[] publicKey, byte[] signature)
        {
            try
            {

                KeyPair kp = rsaKeyFromBytes(publicKey);

                if (kp == null)
                {
                    Logging.warn("Error occurred while verifying signature {0}, invalid public key {1}", Crypto.hashToString(signature), Crypto.hashToString(publicKey));
                    return false;
                }

                Signature sig = Signature.GetInstance("SHA512withRSA");
                sig.InitVerify(kp.Public);
                sig.Update(input_data);
                return sig.Verify(signature);
            }
            catch (Exception e)
            {
                Logging.warn("Error occurred while verifying signature {0} with public key {1}: {2}", Crypto.hashToString(signature), Crypto.hashToString(publicKey), e.Message);
            }
            return false;
        }

        // Encrypt data using RSA
        public byte[] encryptWithRSA(byte[] input, byte[] publicKey)
        {
            KeyPair kp = rsaKeyFromBytes(publicKey);
            Cipher cipher = Cipher.GetInstance("RSA/NONE/OAEPWithSHA1AndMGF1Padding");
            cipher.Init(Javax.Crypto.CipherMode.EncryptMode, kp.Public);
            return cipher.DoFinal(input);
        }


        // Decrypt data using RSA
        public byte[] decryptWithRSA(byte[] input, byte[] privateKey)
        {
            KeyPair kp = rsaKeyFromBytes(privateKey);
            Cipher cipher = Cipher.GetInstance("RSA/NONE/OAEPWithSHA1AndMGF1Padding");
            cipher.Init(Javax.Crypto.CipherMode.DecryptMode, kp.Private);
            return cipher.DoFinal(input);
        }

        // Encrypt data using AES
        public byte[] encryptWithAES(byte[] input, byte[] key, bool use_GCM)
        {
            string algo = AES_algorithm;
            if (use_GCM)
            {
                algo = AES_GCM_algorithm;
            }

            IBufferedCipher outCipher = CipherUtilities.GetCipher(algo);


            int salt_size = outCipher.GetBlockSize();
            if (use_GCM)
            {
                salt_size = 12;
            }
            byte[] salt = getSecureRandomBytes(salt_size);

            byte[] bytes = null;

            ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);
            try
            {
                outCipher.Init(true, withIV);
                byte[] encrypted_data = outCipher.DoFinal(input);

                bytes = new byte[salt.Length + encrypted_data.Length];
                Array.Copy(salt, bytes, salt.Length);
                Array.Copy(encrypted_data, 0, bytes, salt.Length, encrypted_data.Length);
            }
            catch (Exception e)
            {
                Logging.error("Error initializing encryption. {0}", e.ToString());
                return null;
            }

            return bytes;
        }

        // Decrypt data using AES
        public byte[] decryptWithAES(byte[] input, byte[] key, bool use_GCM, int inOffset = 0)
        {
            string algo = AES_algorithm;
            if (use_GCM)
            {
                algo = AES_GCM_algorithm;
            }

            IBufferedCipher inCipher = CipherUtilities.GetCipher(algo);

            byte[] bytes = null;
            try
            {
                try
                {
                    if (use_GCM)
                    {
                        // GCM mode requires 12 bytes salt
                        int salt_size = 12;
                        byte[] salt = new byte[salt_size];

                        Array.Copy(input, inOffset, salt, 0, salt.Length);

                        ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);
                        inCipher.Init(false, withIV);
                        bytes = inCipher.DoFinal(input, inOffset + salt_size, input.Length - inOffset - salt_size);
                    }
                    else
                    {
                        int block_size = inCipher.GetBlockSize();
                        byte[] salt = new byte[block_size];

                        Array.Copy(input, inOffset, salt, 0, salt.Length);

                        ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);
                        inCipher.Init(false, withIV);
                        bytes = inCipher.DoFinal(input, inOffset + block_size, input.Length - inOffset - block_size);
                    }
                }
                catch (Exception)
                {
                    // try again using normal salt - backwards compatibility, TODO TODO can be removed later
                    if (use_GCM)
                    {
                        int block_size = inCipher.GetBlockSize();
                        byte[] salt = new byte[block_size];

                        Array.Copy(input, inOffset, salt, 0, salt.Length);

                        ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);
                        inCipher.Init(false, withIV);
                        bytes = inCipher.DoFinal(input, inOffset + block_size, input.Length - inOffset - block_size);
                    }
                    else
                    {
                        bytes = null;
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                bytes = null;
                Logging.error("Error initializing decryption. {0}", e.ToString());
            }

            return bytes;
        }

        private static byte[] getPbkdf2BytesFromPassphrase(string password, byte[] salt, int iterations, int byteCount)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
            pbkdf2.IterationCount = iterations;
            return pbkdf2.GetBytes(byteCount);
        }

        // Encrypt using password
        public byte[] encryptWithPassword(byte[] data, string password, bool use_GCM)
        {
            byte[] salt = getSecureRandomBytes(16);
            byte[] key = getPbkdf2BytesFromPassphrase(password, salt, PBKDF2_iterations, 16);
            byte[] ret_data = encryptWithAES(data, key, use_GCM);

            List<byte> tmpList = new List<byte>();
            tmpList.AddRange(salt);
            tmpList.AddRange(ret_data);

            return tmpList.ToArray();
        }

        // Decrypt using password
        public byte[] decryptWithPassword(byte[] data, string password, bool use_GCM)
        {
            byte[] salt = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                salt[i] = data[i];
            }
            byte[] key = getPbkdf2BytesFromPassphrase(password, salt, PBKDF2_iterations, 16);
            return decryptWithAES(data, key, use_GCM, 16);
        }

        // Encrypt data using Chacha engine
        public byte[] encryptWithChacha(byte[] input, byte[] key)
        {
            // Create a buffer that will contain the encrypted output and an 8 byte nonce
            byte[] outData = new byte[input.Length + 8];

            // Generate the 8 byte nonce
            byte[] nonce = getSecureRandomBytes(8);

            // Prevent leading 0 to avoid edge cases
            if (nonce[0] == 0)
                nonce[0] = 1;

            // Generate the Chacha engine
            var parms = new ParametersWithIV(new KeyParameter(key), nonce);
            var chacha = new ChaChaEngine(chacha_rounds);
            chacha.Init(true, parms);

            // Encrypt the input data while maintaing an 8 byte offset at the start
            chacha.ProcessBytes(input, 0, input.Length, outData, 8);

            // Copy the 8 byte nonce to the start of outData buffer
            Buffer.BlockCopy(nonce, 0, outData, 0, 8);

            // Return the encrypted data buffer
            return outData;
        }

        // Decrypt data using Chacha engine
        public byte[] decryptWithChacha(byte[] input, byte[] key)
        {
            // Extract the nonce from the input
            byte[] nonce = input.Take(8).ToArray();

            // Generate the Chacha engine
            var parms = new ParametersWithIV(new KeyParameter(key), nonce);
            var chacha = new ChaChaEngine(chacha_rounds);
            chacha.Init(false, parms);

            // Create a buffer that will contain the decrypted output
            byte[] outData = new byte[input.Length - 8];

            // Decrypt the input data
            chacha.ProcessBytes(input, 8, input.Length - 8, outData, 0);

            // Return the decrypted data buffer
            return outData;
        }

        public byte[] generateChildKey(byte[] parentKey, int version, int seed = 0)
        {
            /*RSACryptoServiceProvider origRsa = rsaKeyFromBytes(parentKey);
            if(origRsa.PublicOnly)
            {
                Logging.error("Child key cannot be generated from a public key! Private key is also required.");
                return null;
            }
            RSAParameters origKey = origRsa.ExportParameters(true);
            RsaKeyPairGenerator kpGenerator = new RsaKeyPairGenerator();
            int seed_len = origKey.P.Length + origKey.Q.Length;
            if (seed != 0)
            {
                seed_len += 4;
            }
            byte[] child_seed = new byte[seed_len];
            Array.Copy(origKey.P, 0, child_seed, 0, origKey.P.Length);
            Array.Copy(origKey.Q, 0, child_seed, origKey.P.Length, origKey.Q.Length);
            if(seed != 0)
            {
                Array.Copy(BitConverter.GetBytes(seed), 0, child_seed, origKey.P.Length + origKey.Q.Length, 4);
            }

            Org.BouncyCastle.Crypto.Digests.Sha512Digest key_digest = new Org.BouncyCastle.Crypto.Digests.Sha512Digest();
            Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator digest_rng = new Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator(key_digest);
            digest_rng.AddSeedMaterial(child_seed);
            // TODO: Check if certainty of 80 is good enough for us
            RsaKeyGenerationParameters keyParams = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new Org.BouncyCastle.Security.SecureRandom(digest_rng), 4096, 80);
            RsaKeyPairGenerator keyGen = new RsaKeyPairGenerator();
            keyGen.Init(keyParams);
            AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();
            //
            RSACryptoServiceProvider newRsa = (RSACryptoServiceProvider)DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);
            return rsaKeyToBytes(newRsa, true, version);*/
            return null;
        }

        public byte[] getSecureRandomBytes(int length)
        {
            byte[] random_data = new byte[length];
            rngCsp.GetBytes(random_data);
            return random_data;
        }

        /// <summary>
        ///  Computes a SHA3-256 value of the given data. It is possible to calculate the hash for a subset of the input data by
        ///  using the `offset` and `count` parameters.
        /// </summary>
        /// <param name="data">Source data for hashing.</param>
        /// <param name="offset">Byte offset into the data. Default = 0</param>
        /// <param name="count">Number of bytes to use in the calculation. Default, 0, means use all available bytes.</param>
        /// <returns>SHA3-256 hash of the input data.</returns>
        public byte[] sha3_256(byte[] input, int offset = 0, int count = 0)
        {
            if (count == 0)
            {
                count = input.Length - offset;
            }

            var hashAlgorithm = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256);

            hashAlgorithm.BlockUpdate(input, offset, count);

            byte[] result = new byte[32]; // 256 / 8 = 32
            hashAlgorithm.DoFinal(result, 0);
            return result;
        }

        /// <summary>
        ///  Computes a SHA3-512 value of the given data. It is possible to calculate the hash for a subset of the input data by
        ///  using the `offset` and `count` parameters.
        /// </summary>
        /// <param name="data">Source data for hashing.</param>
        /// <param name="offset">Byte offset into the data. Default = 0</param>
        /// <param name="count">Number of bytes to use in the calculation. Default, 0, means use all available bytes.</param>
        /// <returns>SHA3-512 hash of the input data.</returns>
        public byte[] sha3_512(byte[] input, int offset = 0, int count = 0)
        {
            if (sha3Algorithm512 == null)
            {
                sha3Algorithm512 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);
            }
            if (count == 0)
            {
                count = input.Length - offset;
            }

            sha3Algorithm512.BlockUpdate(input, offset, count);

            byte[] result = new byte[64]; // 512 / 8 = 64
            sha3Algorithm512.DoFinal(result, 0);
            return result;
        }

        /// <summary>
        ///  Computes a (SHA3-512)^2 value of the given data. It is possible to calculate the hash for a subset of the input data by
        ///  using the `offset` and `count` parameters.
        /// </summary>
        /// <remarks>
        ///  The term (SHA3-512)^2 in this case means hashing the value twice - e.g. using SHA3-512 again on the computed hash value.
        /// </remarks>
        /// <param name="input">Source data for hashing.</param>
        /// <param name="offset">Byte offset into the data. Default = 0</param>
        /// <param name="count">Number of bytes to use in the calculation. Default, 0, means use all available bytes.</param>
        /// <returns>SHA3-512 squared hash of the input data.</returns>
        public byte[] sha3_512sq(byte[] input, int offset = 0, int count = 0)
        {
            if (sha3Algorithm512 == null)
            {
                sha3Algorithm512 = new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);
            }
            if (count == 0)
            {
                count = input.Length - offset;
            }

            sha3Algorithm512.BlockUpdate(input, offset, count);

            byte[] result = new byte[64]; // 512 / 8 = 64
            sha3Algorithm512.DoFinal(result, 0);
            sha3Algorithm512.BlockUpdate(result, 0, result.Length);
            sha3Algorithm512.DoFinal(result, 0);
            return result;
        }

        /// <summary>
        ///  Computes a trunc(N, (SHA3-512)^2) value of the given data. It is possible to calculate the hash for a subset of the input data by
        ///  using the `offset` and `count` parameters.
        /// </summary>
        /// <remarks>
        ///  The term (SHA3-512)^2 in this case means hashing the value twice - e.g. using SHA3-512 again on the computed hash value.
        ///  The trunc(N, X) function represents taking only the first `N` bytes of the byte-field `X`.
        /// </remarks>
        /// <param name="input">Source data for hashing.</param>
        /// <param name="offset">Byte offset into the data. Default = 0</param>
        /// <param name="count">Number of bytes to use in the calculation. Default, 0, means use all available bytes.</param>
        /// <param name="hash_length">Number of bytes to keep from the truncated hash.</param>
        /// <returns>SHA3-512 squared and truncated hash of the input data.</returns>
        public byte[] sha3_512sqTrunc(byte[] data, int offset = 0, int count = 0, int hashLength = 44)
        {
            byte[] shaTrunc = new byte[hashLength];
            Array.Copy(sha3_512sq(data, offset, count), shaTrunc, hashLength);
            return shaTrunc;
        }
    }
}
