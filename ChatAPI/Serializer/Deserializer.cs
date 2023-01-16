using System;
namespace ChatAPI
{
    public class Deserializer // Fast binary deserializarion with corrupted data check and fast decryption
    {
        protected byte[] m_data;
        protected int m_offset;
        protected int m_length;

        public Deserializer(byte[] data)
        {
            m_data = data;
            m_length = data.Length;
        }
        public Deserializer(byte[] data, int offset)
        {
            m_data = data;
            m_offset = offset; // Seek offset
            m_length = data.Length;
        }
        public Deserializer(byte[] data, int offset, int count)
        {
            m_data = data;
            m_offset = offset; // Seek offset
            m_length = offset + count;
        }
        protected unsafe int ExtractLength()
        {
            int offset = m_offset;
            if (m_length >= offset + 4)
            {
                int length;
                fixed (byte* pdata = &m_data[offset])
                    length = *(int*)(pdata); // Read length

                if (length >= 0)
                {
                    offset += 4;
                    if (m_length >= offset + length) // Make sure length info is correct
                    {
                        m_offset = offset; // Seek offset
                        return length;
                    }
                }
            }
            return -1;
        }

        public bool Extract(ref string result)
        {
            int length = ExtractLength();
            if (length < 0 || (length & 1) != 0) // UTF16 lengh must be divisible by 2
                return false; // Invalid or corrupted data

            result = StringEncoder.UTF16.GetString(m_data, m_offset, length);
            m_offset += length; // Seek offset
            return true;
        }

        public bool Extract(ref byte result)
        {
            int length = ExtractLength();
            if (length != 1) // Length must be 1 for byte
                return false; // Invalid or corrupted data

            result = m_data[m_offset++]; // Set result and seek offset +1byte
            return true;
        }

        public unsafe bool Extract(ref int result)
        {
            int length = ExtractLength();
            if (length != 4) // Length must be 4bytes for int
                return false; // Invalid or corrupted data

            fixed (byte* pdata = &m_data[m_offset])
                result = *(int*)(pdata); // Set result

            m_offset += 4; // Seek offset +4bytes
            return true;
        }

        public unsafe bool Extract(ref uint result)
        {
            int length = ExtractLength();
            if (length != 4) // Length must be 4bytes for int
                return false; // Invalid or corrupted data

            fixed (byte* pdata = &m_data[m_offset])
                result = *(uint*)(pdata); // Set result

            m_offset += 4; // Seek offset +4bytes
            return true;
        }

        public unsafe bool Extract(ref long result)
        {
            int length = ExtractLength();
            if (length != 8) // Length must be 8bytes for long
                return false; // Invalid or corrupted data

            fixed (byte* pdata = &m_data[m_offset])
                result = *(long*)(pdata); // Set result

            m_offset += 8; // Seek offset +8bytes
            return true;
        }

        public bool Extract(ref DateTime result)
        {
            long outData = 0;
            if (!Extract(ref outData))
                return false;

            result = new DateTime(outData);
            return true;
        }

        public unsafe bool Extract(ref byte[] result)
        {
            int length = ExtractLength();
            if (length < 0)
                return false; // Invalid or corrupted data

            if (length == 0)
                result = Util.EmptyBytes;
            else
            {
                result = new byte[length];
                Array.Copy(m_data, m_offset, result, 0, length);
                m_offset += length; // Seek offset
            }
            return true;
        }

        public bool SecureExtract(ref byte[] result, byte[] cryptoKey) // Decrypt and Extract
        {
            int length = ExtractLength();
            if (length < 0)
                return false; // Invalid or corrupted data

            var outData = new byte[length - 4]; // length -4bytes crypto header
            if (!Crypto.Decrypt(cryptoKey, this.m_data, this.m_offset, length, outData, 0))
                return false; // Invalid or corrupted crypto

            result = outData;
            m_offset += length; // Seek offset
            return true;
        }

        public bool SecureExtract(ref Deserializer result, byte[] cryptoKey) // Decrypt and Extract
        {
            byte[] outData = null;
            if (!SecureExtract(ref outData, cryptoKey))
                return false; // Invalid or corrupted crypto

            result = new Deserializer(outData);
            return true;
        }
    }
}