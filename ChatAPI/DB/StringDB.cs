using System;

namespace ChatAPI
{
    public class StringDB : DynamicBlockDB<string>
    {

        private StringEncoder m_encoder;

        public StringDB(string fileName, StringEncoder encoder)
            : base(fileName)
        {
            m_encoder = encoder;
            base.LoadBlocks();
        }

        protected override string TransformBlock(byte[] block, int index, int count)
        {
            return m_encoder.GetString(block, index, count);
        }

        protected override byte[] TransformBlock(string value)
        {
            return m_encoder.GetBytes(value);
        }
    }
}