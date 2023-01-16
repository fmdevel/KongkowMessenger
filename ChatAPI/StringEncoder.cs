using System;

namespace ChatAPI
{
    public unsafe abstract class StringEncoder
    {
        public static readonly StringEncoder UTF8 = new UTF8Encoder();
        public static readonly StringEncoder UTF16 = new UTF16Encoder();

        public readonly int BytesPerChar;
        protected abstract void Copy(byte* source, char* destination, int charsCount);
        protected abstract void Copy(char* source, byte* destination, int charsCount);
        protected StringEncoder(int bytesPerChar)
        {
            BytesPerChar = bytesPerChar;
        }

        public string GetString(byte[] value)
        {
            return GetString(value, 0, value.Length);
        }
        public string GetString(byte[] value, int startIndex, int length)
        {
            if (value == null || (startIndex | length) < 0 || startIndex + length > value.Length)
                throw new ArgumentException();

            if (length == 0)
                return string.Empty;

            length /= BytesPerChar;
            var result = Util.FastAllocateString(length);
            Copy(value, startIndex, result, 0, length);
            return result;
        }

        public byte[] GetBytes(string value)
        {
            return GetBytes(value, 0, value.Length);
        }
        public byte[] GetBytes(string value, int startIndex, int length)
        {
            if (value == null || (startIndex | length) < 0 || startIndex + length > value.Length)
                throw new ArgumentException();

            if (length == 0)
                return Util.EmptyBytes;

            var result = new byte[length * BytesPerChar];
            Copy(value, startIndex, result, 0, length);
            return result;
        }

        //public byte[] GetBytes(char[] value)
        //{
        //    return GetBytes(value, 0, value.Length);
        //}
        //public byte[] GetBytes(char[] value, int startIndex, int length)
        //{
        //    if (value == null || (startIndex | length) < 0 || startIndex + length > value.Length)
        //        throw new ArgumentException();

        //    if (length == 0)
        //        return Util.EmptyBytes;

        //    var result = new byte[length * BytesPerChar];
        //    Copy(value, startIndex, result, 0, length);
        //    return result;
        //}

        //public char[] GetChars(byte[] value)
        //{
        //    return GetChars(value, 0, value.Length);
        //}
        //public char[] GetChars(byte[] value, int startIndex, int length)
        //{
        //    if (value == null || (startIndex | length) < 0 || startIndex + length > value.Length)
        //        throw new ArgumentException();

        //    if (length == 0)
        //        return Util.EmptyChars;

        //    length /= BytesPerChar;
        //    var result = new char[length];
        //    Copy(value, startIndex, result, 0, length);
        //    return result;
        //}

        internal void Copy(string source, int sourceIndex, byte[] destination, int destinationIndex, int charsCount)
        {
            fixed (char* src = source)
            fixed (byte* dest = &destination[destinationIndex])
                Copy(src + sourceIndex, dest, charsCount);
        }
        internal void Copy(byte[] source, int sourceIndex, string destination, int destinationIndex, int charsCount)
        {
            fixed (byte* src = &source[sourceIndex])
            fixed (char* dest = destination)
                Copy(src, dest + destinationIndex, charsCount);
        }
        internal void Copy(char[] source, int sourceIndex, byte[] destination, int destinationIndex, int charsCount)
        {
            fixed (char* src = &source[sourceIndex])
            fixed (byte* dest = &destination[destinationIndex])
                Copy(src, dest, charsCount);
        }
        internal void Copy(byte[] source, int sourceIndex, char[] destination, int destinationIndex, int charsCount)
        {
            fixed (byte* src = &source[sourceIndex])
            fixed (char* dest = &destination[destinationIndex])
                Copy(src, dest, charsCount);
        }


        private class UTF8Encoder : StringEncoder
        {
            public UTF8Encoder() : base(1) { }

            protected override unsafe void Copy(byte* source, char* destination, int charsCount)
            {
                do
                {
                    *destination = (char)(*source);
                    source++;
                    destination++;
                } while (--charsCount != 0);
            }
            protected override unsafe void Copy(char* source, byte* destination, int charsCount)
            {
                do
                {
                    *destination = (byte)(*source);
                    source++;
                    destination++;
                } while (--charsCount != 0);
            }
        }

        private class UTF16Encoder : StringEncoder
        {
            public UTF16Encoder() : base(2) { }

            protected override unsafe void Copy(byte* source, char* destination, int charsCount)
            {
                do
                {
                    *destination = *(char*)source;
                    source += 2;
                    destination++;
                } while (--charsCount != 0);
            }
            protected override unsafe void Copy(char* source, byte* destination, int charsCount)
            {
                do
                {
                    *(char*)destination = *source;
                    source++;
                    destination += 2;
                } while (--charsCount != 0);
            }
        }
    }
}