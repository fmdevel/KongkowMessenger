using System;

namespace ChatAPI
{
    public class ChatMessageDB : DynamicBlockDB<ChatMessage>
    {
        public ChatMessageDB(string fileName)
            : base(fileName)
        {
            base.LoadBlocks();
        }

        protected override byte[] TransformBlock(ChatMessage value)
        {
            var s = new Serializer();
            s.Add((value.Contact == null) ? string.Empty : value.Contact.ID);
            s.Add((byte)value.Type);
            s.Add(value.MessageId);
            s.Add(value.Message);
            //((value.Attachment == null) ? Attachment.Empty : value.Attachment).SerializeTo(s);
            s.Add(value.Attachment == null ? string.Empty : value.Attachment.FileName);
            s.Add(Util.EmptyBytes); // Content Attachment is just empty, already cached

            s.Add(value.Time);
            s.Add((byte)value.Status);
            s.Add((byte)value.Direction);
            return s.ToArray();
        }

        protected override ChatMessage TransformBlock(byte[] block, int index, int count)
        {
            //var data = new byte[count];
            //Array.Copy(block, index, data, 0, count);
            var chat = new Deserializer(block, index, count);

            string id = null;
            if (!chat.Extract(ref id))
                return null;

            byte type = 0;
            if (!chat.Extract(ref type) || !Enums<ChatMessage.TypeChat>.IsDefined(type))
                return null;

            int messageId = 0;
            if (!chat.Extract(ref messageId))
                return null;

            string message = null;
            if (!chat.Extract(ref message))
                return null;

            var attachment = Attachment.Deserialize(chat);

            DateTime time = default(DateTime);
            if (!chat.Extract(ref time))
                return null;

            byte status = 0;
            if (!chat.Extract(ref status) || !Enums<Notification>.IsDefined(status))
                return null;

            byte direction = 0;
            if (!chat.Extract(ref direction) || !Enums<ChatMessage.TypeDirection>.IsDefined(direction))
                return null;

            var contact = Core.FindContact(id);
            if (contact == null)
                contact = Contact.Create(id, null, null);

            return new ChatMessage(contact, (ChatMessage.TypeChat)type, messageId, message, attachment, time, (Notification)status, (ChatMessage.TypeDirection)direction);
        }

        public int FindChatIndex(int messageId)
        {
            var blocks = base.m_blocks;
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i].MessageId == messageId)
                    return i;

            return -1;
        }

        public ChatMessage FindChat(int messageId)
        {
            foreach (var chat in base.m_blocks)
                if (chat.MessageId == messageId)
                    return chat;

            return null;
        }

    }
}
