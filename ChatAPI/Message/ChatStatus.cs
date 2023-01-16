using System;
namespace ChatAPI
{
    public class ChatStatus
    {
        public readonly Notification Status;
        public readonly int MessageId;

        public ChatStatus(Notification status, int messageId)
        {
            Status = status;
            MessageId = messageId;
        }

        public void TransmitTo(Contact contact)
        {
            var respBuf = new NSerializer(TypeHeader.NOTIFICATION);
            respBuf.Add((byte)this.Status);
            respBuf.Add(contact.ID);
            respBuf.Add(this.MessageId);
            Network.Enqueue(respBuf);
        }
    }
}