using System;

namespace ChatAPI
{
    public class Serializer // Fast binary serializarion with fast encryption support
    {
        protected byte[] m_data;
        protected int m_offset;
        protected int m_length;

        public Serializer() // For Serialize
        {
            m_data = Util.EmptyBytes;
        }
        
        private void EnlargeBlock(int length)
        {
            int requiredLength = m_offset + 4 + length; // current position + 4bytes for length + data length in bytes
            if (m_length < requiredLength) // is buffer enough?
            {
                // To be efficient, minimum requiredLength is 32 or twice as current buffer length
                m_length = Math.Max(Math.Max(requiredLength, 32), m_length * 2);
                Array.Resize(ref m_data, m_length);
            }
        }

        protected unsafe void AddLength(int length)
        {
            EnlargeBlock(length);
            fixed (byte* pdata = &m_data[m_offset])
                *(int*)(pdata) = length; // Write length

            m_offset += 4; // Seek offset
        }

        public void Add(string value)
        {
            int length = value.Length * 2; // Because UTF16 takes 2 bytes per char
            AddLength(length);
            if (length > 0)
                StringEncoder.UTF16.Copy(value, 0, m_data, m_offset, value.Length);

            m_offset += length; // Seek offset
        }

        public void Add(byte value)
        {
            AddLength(1); // 1byte
            m_data[m_offset++] = value; // Set value and seek offset +1byte
        }

        public unsafe void Add(int value)
        {
            AddLength(4); // 4bytes for int
            fixed (byte* pdata = &m_data[m_offset])
                *(int*)(pdata) = value; // Set value

            m_offset += 4; // Seek offset +4bytes
        }

        public unsafe void Add(uint value)
        {
            AddLength(4); // 4bytes for int
            fixed (byte* pdata = &m_data[m_offset])
                *(uint*)(pdata) = value; // Set value

            m_offset += 4; // Seek offset +4bytes
        }

        public unsafe void Add(long value)
        {
            AddLength(8); // 8bytes for long
            fixed (byte* pdata = &m_data[m_offset])
                *(long*)(pdata) = value; // Set value

            m_offset += 8; // Seek offset +8bytes
        }

        public void Add(DateTime value)
        {
            Add(value.Ticks);
        }

        public void Add(byte[] value)
        {
            AddLength(value.Length);
            Array.Copy(value, 0, m_data, m_offset, value.Length);
            m_offset += value.Length; // Seek offset
        }

        public void SecureAdd(byte[] value, byte[] cryptoKey) // Add and Encrypt
        {
            AddLength(value.Length + 4); // length +4bytes crypto header
            Crypto.Encrypt(cryptoKey, value, 0, value.Length, this.m_data, this.m_offset);
            this.m_offset += (value.Length + 4); // length +4bytes crypto header
        }

        public void SecureAdd(Serializer value, byte[] cryptoKey) // Add and Encrypt
        {
            AddLength(value.m_offset + 4); // length +4bytes crypto header
            Crypto.Encrypt(cryptoKey, value.m_data, 0, value.m_offset, this.m_data, this.m_offset);
            this.m_offset += (value.m_offset + 4); // length +4bytes crypto header
        }

        public void SaveTo(System.IO.Stream stream)
        {
            stream.Write(m_data, 0, m_offset);
        }

        public byte[] ToArray()
        {
            var result = new byte[m_offset];
            Array.Copy(m_data, 0, result, 0, m_offset);
            return result;
        }
    }
}