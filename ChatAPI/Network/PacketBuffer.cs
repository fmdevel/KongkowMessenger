using System;
using System.Collections.Generic;
using System.Text;

namespace ChatAPI
{
    public partial class Client
    {
        private class PacketBuffer
        {
            internal byte[] data;
            internal int count;

            public PacketBuffer(int capacity)
            {
                data = new byte[capacity];
            }

            public void AddRange(byte[] array, int length)
            {
                if (length > 0)
                {
                    if (count + length > data.Length)
                        Resize(ref data, count, Math.Max(data.Length * 2, count + length));

                    Array.Copy(array, 0, data, count, length);
                    count += length;
                }
            }

            public void RemoveRange(int length)
            {
                if (length < count)
                    Array.Copy(data, length, data, 0, count - length);

                count -= length;
            }


            public static void Resize(ref byte[] array, int preservedSize, int newSize)
            {
                var dest = new byte[newSize];
                if (preservedSize > 0)
                    Array.Copy(array, 0, dest, 0, preservedSize);
                array = dest;
            }
        }
    }
}