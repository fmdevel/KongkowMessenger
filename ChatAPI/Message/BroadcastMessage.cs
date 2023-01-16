using System;
using System.IO;
using System.Collections.Generic;

namespace ChatAPI
{
    internal class BroadcastMessage : IProgress
    {
        public readonly DateTime Time;
        public readonly List<int> MessageIds;
        private NSerializer m_respBuf;

        public BroadcastMessage(string message, Attachment attachment, List<Contact> target)
        {
            var s = new Serializer();
            message = (string.IsNullOrEmpty(message) ? "🔊 Pesan Broadcast" : string.Concat("🔊 Pesan Broadcast", "\r\n\r\n", message));
            s.Add(message);
            if (attachment == null || attachment.FileName.Length == 0 || !File.Exists(attachment.FileName))
            {
                s.Add(string.Empty);
                s.Add(Util.EmptyBytes);
            }
            else
            {
                s.Add(Path.GetExtension(attachment.FileName));
                s.Add(File.ReadAllBytes(attachment.FileName));
            }

            MessageIds = new List<int>(target.Count);
            foreach (var contact in target)
            {
                if (contact != null && !contact.IsServerPOS && contact.IsActive && contact.ID != Core.Owner.ID)
                {
                    var messageId = contact.GetConversation().AddChat(message, attachment).MessageId;
                    MessageIds.Add(messageId);
                    s.Add(contact.ID);
                    s.Add(messageId);
                    contact.CloseConversation();
                }
            }
            if (MessageIds.Count > 0)
            {
                m_respBuf = new NSerializer(TypeHeader.BROADCAST);
                m_respBuf.SecureAdd(s, Core.CryptoKey); // Encrypt data!!
                Time = DateTime.Now;
            }
        }

        public void Transmit()
        {
            if (MessageIds.Count > 0)
                Network.Enqueue(m_respBuf, ChatMessage.TIMEOUT, this);
        }

        void IProgress.SetProgress(bool success, int current, int max)
        {
            if (success)
                foreach (int messageId in MessageIds)
                    Network.NotifyIncomingChatStatus(Notification.CHAT_SENT, messageId, false); // Set to false, because BroadcastMessage is not saved on PendingChat
            else
            {
                if (Time.Ticks + (10000L * ChatMessage.TIMEOUT) > DateTime.Now.Ticks)
                {
                    this.Transmit();
                    return;
                }
                this.TimedOut();
            }
        }

        public void TimedOut()
        {
            foreach (int messageId in MessageIds)
                Network.NotifyIncomingChatStatus(Notification.CHAT_SEND_FAIL, messageId, false); // Set to false, because BroadcastMessage is not saved on PendingChat
        }
    }
}