using System;

namespace ChatAPI
{
    public class ChatStatusDB : DynamicBlockDB<ChatStatus>
    {
        public ChatStatusDB(string fileName)
            : base(fileName)
        {
            base.LoadBlocks();
        }
        protected override byte[] TransformBlock(ChatStatus value)
        {
            var s = new Serializer();
            s.Add((byte)value.Status);
            s.Add(value.MessageId);
            return s.ToArray();
        }

        protected override ChatStatus TransformBlock(byte[] block, int index, int count)
        {
            //var data = new byte[count];
            //Array.Copy(block, index, data, 0, count);
            var stat = new Deserializer(block, index, count);

            byte status = 0;
            if (!stat.Extract(ref status) || !Enums<Notification>.IsDefined(status))
                return null;

            int messageId = 0;
            if (!stat.Extract(ref messageId))
                return null;

            return new ChatStatus((Notification)status, messageId);
        }
    }
}
