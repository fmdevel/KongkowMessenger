using System;

namespace ChatAPI
{
    internal class NDeserializer : Deserializer  // Network Deserializer
    {
        public readonly TypeHeader Type;

        public NDeserializer(byte[] data)
            : base(data, 1) // Set offset to 1, because first byte is Type
        {
            this.Type = GetTypeMsg(data[0]); // Get Type from first byte
        }
        public static TypeHeader GetTypeMsg(byte type)
        {
            return Enums<TypeHeader>.IsDefined(type) ? (TypeHeader)type : TypeHeader.INVALID;
        }
    }
}