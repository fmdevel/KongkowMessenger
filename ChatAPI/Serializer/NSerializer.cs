using System;

namespace ChatAPI
{
    public class NSerializer : Serializer  // Network Serializer
    {
        public readonly TypeHeader Type;
        public NSerializer(TypeHeader type)
        {
            Type = type;
            m_offset = 4 + 1; // 4-byte for NetworkHeader, 1-byte for Type
        }

        public bool Send(Client client, IProgress progress)
        {
            if (client != null && client.IsConnected)
            {
                if (m_data.Length < m_offset) // Guard request with Type only (without Addition)
                    Array.Resize(ref m_data, m_offset);

                Client.SetHeader(m_data, m_offset - 4); // Fill the first 4-bytes with NetworkHeader. Need to -4 to get real count
                m_data[4] = (byte)Type; // Set the 5-th byte as Type
                return client.Enqueue(new Client.Queue(m_data, m_offset, progress));
            }
            return false;
        }
    }
}