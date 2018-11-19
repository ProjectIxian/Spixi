using DLT.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Engines;
using System.Security.Cryptography;
using Java.Security;
using Java.Security.Spec;

namespace CryptoLibs
{
    class BouncyCastleAndroid : DLT.ICryptoLib
    {
        byte[] publicKeyBytes;
        byte[] privateKeyBytes;

        // Private variables used for AES key expansion
        private int PBKDF2_iterations = 10000;
        private string AES_algorithm = "AES/CBC/PKCS7Padding";

        // Private variables used for Chacha
        private readonly int chacha_rounds = 20;


        public BouncyCastleAndroid()
        {
            publicKeyBytes = null;
            privateKeyBytes = null;
        }

        private byte[] rsaKeyToBytes(KeyPair rsaKey, bool includePrivateParameters)
        {
            List<byte> bytes = new List<byte>();

            KeyFactory kf = KeyFactory.GetInstance("RSA");
            RSAPrivateCrtKeySpec rsaParams = null;
            RSAPublicKeySpec rsaPubParams = null;
            if (includePrivateParameters)
            {
                rsaParams = (RSAPrivateCrtKeySpec)kf.GetKeySpec(rsaKey.Private, Java.Lang.Class.FromType(typeof(RSAPrivateCrtKeySpec)));
                bytes.AddRange(BitConverter.GetBytes(rsaParams.Modulus.ToByteArray().Length));
                bytes.AddRange(rsaParams.Modulus.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.PublicExponent.ToByteArray().Length));
                bytes.AddRange(rsaParams.PublicExponent.ToByteArray());

                bytes.AddRange(BitConverter.GetBytes(rsaParams.PrimeP.ToByteArray().Length));
                bytes.AddRange(rsaParams.PrimeP.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.PrimeQ.ToByteArray().Length));
                bytes.AddRange(rsaParams.PrimeQ.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.PrimeExponentP.ToByteArray().Length));
                bytes.AddRange(rsaParams.PrimeExponentP.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.PrimeExponentQ.ToByteArray().Length));
                bytes.AddRange(rsaParams.PrimeExponentQ.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.CrtCoefficient.ToByteArray().Length));
                bytes.AddRange(rsaParams.CrtCoefficient.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaParams.PrivateExponent.ToByteArray().Length));
                bytes.AddRange(rsaParams.PrivateExponent.ToByteArray());
            }
            else
            {
                rsaPubParams = (RSAPublicKeySpec)kf.GetKeySpec(rsaKey.Public, Java.Lang.Class.FromType(typeof(RSAPublicKeySpec)));
                bytes.AddRange(BitConverter.GetBytes(rsaPubParams.Modulus.ToByteArray().Length));
                bytes.AddRange(rsaPubParams.Modulus.ToByteArray());
                bytes.AddRange(BitConverter.GetBytes(rsaPubParams.PublicExponent.ToByteArray().Length));
                bytes.AddRange(rsaPubParams.PublicExponent.ToByteArray());
            }

            return bytes.ToArray();
        }

        private RSACryptoServiceProvider rsaKeyFromBytes(byte [] keyBytes)
        {
            try
            {
                RSAParameters rsaParams = new RSAParameters();

                int offset = 0;
                int dataLen = 0;

                dataLen = BitConverter.ToInt32(keyBytes, offset);
                offset += 4;
                rsaParams.Modulus = keyBytes.Skip(offset).Take(dataLen).ToArray();
                offset += dataLen;

                dataLen = BitConverter.ToInt32(keyBytes, offset);
                offset += 4;
                rsaParams.Exponent = keyBytes.Skip(offset).Take(dataLen).ToArray();
                offset += dataLen;

                if (keyBytes.Length > offset)
                {
                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.P = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.Q = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.DP = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.DQ = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.InverseQ = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;

                    dataLen = BitConverter.ToInt32(keyBytes, offset);
                    offset += 4;
                    rsaParams.D = keyBytes.Skip(offset).Take(dataLen).ToArray();
                    offset += dataLen;
                }

                RSACryptoServiceProvider rcsp = new RSACryptoServiceProvider();
                rcsp.ImportParameters(rsaParams);
                return rcsp;
            }catch(Exception)
            {
                Logging.warn("An exception occured while trying to reconstruct PKI from bytes");
            }
            return null;
        }

        // Generates keys for RSA signing
        public bool generateKeys(int keySize)
        {
            KeyPair kp = null;
            try
            {
                KeyPairGenerator kpg = KeyPairGenerator.GetInstance("RSA");
                kpg.Initialize(keySize);
                kp = kpg.GenKeyPair();
                privateKeyBytes = rsaKeyToBytes(kp, true);
                publicKeyBytes = rsaKeyToBytes(kp, false);
            }
            catch (Exception e)
            {
                Logging.warn(string.Format("Exception while generating signature keys: {0}", e.ToString()));
                return false;
            }
            return true;
        }

        public byte[] getPublicKey()
        {
            return publicKeyBytes;
        }

        public byte[] getPrivateKey()
        {
            return privateKeyBytes;
        }
        
        public byte[] getSignature(byte[] input_data, byte[] privateKey)
        {
            try
            {
                RSACryptoServiceProvider rsa = rsaKeyFromBytes(privateKey);

                byte[] signature = rsa.SignData(input_data, CryptoConfig.MapNameToOID("SHA512"));
                return signature;
            }
            catch (Exception e)
            {
                Logging.warn(string.Format("Cannot generate signature: {0}", e.Message));
            }
            return null;
        }

        public bool verifySignature(byte[] input_data, byte[] publicKey, byte[] signature)
        {
            try
            {

                RSACryptoServiceProvider rsa = rsaKeyFromBytes(publicKey);

                byte[] signature_bytes = signature;
                return rsa.VerifyData(input_data, CryptoConfig.MapNameToOID("SHA512"), signature_bytes);
            }
            catch (Exception e)
            {
                Logging.warn(string.Format("Invalid public key {0}:{1}", publicKey, e.Message));
            }
            return false;
        }

        // Encrypt data using RSA
        public byte[] encryptWithRSA(byte[] input, byte[] publicKey)
        {
            RSACryptoServiceProvider rsa = rsaKeyFromBytes(publicKey);
            return rsa.Encrypt(input, false);
        }


        // Decrypt data using RSA
        public byte[] decryptWithRSA(byte[] input, byte[] privateKey)
        {
            RSACryptoServiceProvider rsa = rsaKeyFromBytes(privateKey);
            return rsa.Decrypt(input, false);
        }

        // Encrypt data using AES
        public byte[] encryptDataAES(byte[] input, byte[] key)
        {
            IBufferedCipher outCipher = CipherUtilities.GetCipher(AES_algorithm);

            int blockSize = outCipher.GetBlockSize();
            // Perform key expansion
            byte[] salt = new byte[blockSize];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with a random value.
                rngCsp.GetBytes(salt);
            }

            ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);
            try
            {
                outCipher.Init(true, withIV);
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error initializing encryption. {0}", e.ToString()));
                return null;
            }

            List<byte> bytes = new List<byte>();
            bytes.AddRange(salt);
            bytes.AddRange(outCipher.DoFinal(input));

            return bytes.ToArray();
        }

        // Decrypt data using AES
        public byte[] decryptDataAES(byte[] input, byte [] key, int inOffset = 0)
        {

            IBufferedCipher inCipher = CipherUtilities.GetCipher(AES_algorithm);

            int blockSize = inCipher.GetBlockSize();
            // Perform key expansion
            byte[] salt = new byte[blockSize];

            for (int i = 0; i < blockSize; i++)
            {
                salt[i] = input[inOffset + i];
            }

            ParametersWithIV withIV = new ParametersWithIV(new KeyParameter(key), salt);

            try
            {
                inCipher.Init(false, withIV);
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Error initializing decryption. {0}", e.ToString()));
            }

            byte[] bytes = inCipher.DoFinal(input, inOffset + blockSize, input.Length - inOffset - blockSize);

            return bytes;
        }

        private static byte[] getPbkdf2BytesFromPassphrase(string password, byte[] salt, int iterations, int byteCount)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
            pbkdf2.IterationCount = iterations;
            return pbkdf2.GetBytes(byteCount);
        }

        // Encrypt using password
        public byte[] encryptWithPassword(byte[] data, string password)
        {
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with a random value.
                rngCsp.GetBytes(salt);
            }
            byte[] key = getPbkdf2BytesFromPassphrase(password, salt, PBKDF2_iterations, 16);
            byte[] ret_data = encryptDataAES(data, key);

            List<byte> tmpList = new List<byte>();
            tmpList.AddRange(salt);
            tmpList.AddRange(ret_data);

            return tmpList.ToArray();
        }

        // Decrypt using password
        public byte[] decryptWithPassword(byte[] data, string password)
        {
            byte[] salt = new byte[16];
            for(int i = 0; i < 16; i++)
            {
                salt[i] = data[i];
            }
            byte[] key = getPbkdf2BytesFromPassphrase(password, salt, PBKDF2_iterations, 16);
            return decryptDataAES(data, key, 16);
        }

        // Encrypt data using Chacha engine
        public byte[] encryptWithChacha(byte[] input, byte[] key)
        {
            // Create a buffer that will contain the encrypted output and an 8 byte nonce
            byte[] outData = new byte[input.Length + 8];

            // Generate the 8 byte nonce
            Random rnd = new Random();
            byte[] nonce = new byte[8];
            rnd.NextBytes(nonce);

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

        public byte[] generateChildKey(byte[] parentKey, int seed = 0)
        {
            RSACryptoServiceProvider origRsa = rsaKeyFromBytes(parentKey);
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
            //return rsaKeyToBytes(newRsa, true);
            return null;
        }

    }
}
