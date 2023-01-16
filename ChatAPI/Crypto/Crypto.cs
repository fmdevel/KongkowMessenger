using System;
using System.Security.Cryptography;

namespace ChatAPI
{
    internal static class Crypto // UltaFast Cryptography
    {
        public static int hash(byte[] value)
        {
            return hash(value, 0, value.Length);
        }
        public static int hash(byte[] value, int index, int length)
        {
            int h = 0;
            while (length-- > 0)
                h = (h << 3) | (h >> 29) ^ value[index++];
            return h;
        }

        public static int hashV2(byte[] value)
        {
            return hashV2(value, 0, value.Length);
        }
        public static int hashV2(byte[] value, int index, int length)
        {
            int h = 0;
            while (length-- > 0)
                h = (h << 5) + h ^ value[index++];
            return h;
        }
        public static int hashV2(string value)
        {
            return hashV2(value, 0, value.Length);
        }
        public static int hashV2(string value, int index, int length)
        {
            int h = 0;
            while (length-- > 0)
                h = (h << 5) + h ^ value[index++];
            return h;
        }

        public unsafe static bool Encrypt(byte[] key, byte[] inData, int inIndex, int length, byte[] outData, int outIndex)
        {
            if (key == null || key.Length != 32 + 4 || length == 0 // key.Length must be 32 (+4hash), length must > 0
                || inData == null || (inIndex | length) < 0 || inIndex + length > inData.Length
                || outData == null || (outIndex | length) < 0 || outIndex + length + 4 > outData.Length)
                return false; // invalid key, input or output

            Array.Clear(outData, outIndex + 4, length); // clear output

            int i, iteration = length;
            if (iteration < 32) iteration = 32; // key.Length must be 32 (+4hash)

            for (i = 0; i < iteration; i++)
                outData[(i % length) + outIndex + 4] ^= key[i & 31]; // spread key into output

            for (i = 0; i < length; i++)
                outData[outIndex + i + 4] ^= (byte)(i ^ inData[inIndex + i]);

            int finalHash = hash(outData, outIndex + 4, length);
            fixed (byte* pKey = &key[32]) // get keyHash pointer at the end of key
                finalHash ^= *(int*)(pKey); // xor finalHash with keyHash

            fixed (byte* pOut = &outData[outIndex])
                *(int*)(pOut) = finalHash; // set finalHash as output header

            return true;
        }

        public unsafe static bool Decrypt(byte[] key, byte[] inData, int inIndex, int length, byte[] outData, int outIndex)
        {
            if (key == null || key.Length != 32 + 4 || length == 0 // key.Length must be 32 (+4hash), length must > 0
                || inData == null || (inIndex | length) < 0 || inIndex + length > inData.Length
                || outData == null || (outIndex | length) < 0 || outIndex + length - 4 > outData.Length)
                return false; // invalid key, input or output

            length -= 4; // substract length with header
            int finalHash = hash(inData, inIndex + 4, length);
            fixed (byte* pKey = &key[32]) // get keyHash pointer at the end of key
                finalHash ^= *(int*)(pKey); // xor finalHash with keyHash

            fixed (byte* pIn = &inData[inIndex])
                if (finalHash != *(int*)(pIn))
                return false; // finalHash does not match with given input hash

            Array.Clear(outData, outIndex, length); // clear output

            int i, iteration = length;
            if (iteration < 32) iteration = 32; // key.Length must be 32 (+4hash)

            for (i = 0; i < iteration; i++)
                outData[outIndex + (i % length)] ^= key[i & 31]; // spread key into output

            for (i = 0; i < length; i++)
                outData[outIndex + i] ^= (byte)(i ^ inData[inIndex + i + 4]);

            return true;
        }

        public unsafe static byte[] EncryptPassword(string value)
        {
            using (var crypto = SHA256.Create())
            {
                byte[] key = crypto.ComputeHash(StringEncoder.UTF8.GetBytes(value));
                var keyHash = hashV2(key, 0, 32);
                Array.Resize(ref key, 32 + 4);
                fixed (byte* pKey = &key[32])
                    *(int*)(pKey) = keyHash; // write hash at the end of key
                return key;
            }
        }

        public unsafe static bool VerifyPassword(string value)
        {
            var key = Core.CryptoKeyPassword;
            if (key == null || key.Length != 32 + 4)
                return false;

            var encryptedPassword = EncryptPassword(value);
            for (int i = 0; i < key.Length; i++) if (encryptedPassword[i] != key[i]) return false;

            return true;
        }
    }
}