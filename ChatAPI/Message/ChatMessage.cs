using System;
namespace ChatAPI
{
    public class ChatMessage : IProgress
    {
        public enum TypeChat : byte
        {
            PRIVATE_CHAT = 1,
            BROADCAST = 2,
            GROUP = 4
        }

        public enum TypeDirection : byte
        {
            IN = 0,
            OUT = 1
        }


        public readonly Contact Contact;
        public readonly TypeChat Type;
        public readonly int MessageId;
        public readonly string Message;
        public readonly Attachment Attachment;

        public Notification Status;
        public readonly DateTime Time;
        public readonly TypeDirection Direction;
        public int ProgressCurrent;
        public int ProgressMax;
        private int m_retryTimes;
        public static Action<ChatMessage> OnProgress;

        public ChatMessage(Contact contact, TypeChat type, string message)
            : this(contact, type, message, Attachment.Empty)
        {
        }

        public ChatMessage(Contact contact, TypeChat type, string message, Attachment attachment)
            : this(contact, type, Util.UniqeId, message, attachment, DateTime.Now, Notification.CHAT_PENDING, TypeDirection.OUT)
        {
        }

        public ChatMessage(Contact contact, TypeChat type, int messageId, string message, Attachment attachment, DateTime time, Notification status, TypeDirection direction)
        {
            this.Contact = contact;
            this.Type = type;
            this.MessageId = messageId;
            this.Message = message;
            this.Attachment = attachment;

            this.Time = time;
            this.Status = status;
            this.Direction = direction;
        }

        public override bool Equals(object obj)
        {
            var b = obj as ChatMessage;
            return (object)b != null && this.MessageId == b.MessageId;
        }

        public override int GetHashCode()
        {
            return this.MessageId;
        }

        public static int CompareTimeReverse(ChatMessage a, ChatMessage b)
        {
            return -DateTime.Compare(a.Time, b.Time);
        }

        public string MessageLocal
        {
            get
            {
                var message = this.Message;
                if ((object)message != null && message.StartsWith("🔊 Pesan Broadcast") && Core.Setting.Language != Language.ID)
                    return "🔊 Broadcast Message" + message.Substring("🔊 Pesan Broadcast".Length);
                return message;
            }
        }

        public string MessageTrim
        {
            get
            {
                var message = this.Message;
                if ((object)message != null && message.StartsWith("🔊 Pesan Broadcast"))
                    message = message.Substring("🔊 Pesan Broadcast".Length).Trim();
                return message;
            }
        }

        public void ForwardTo(System.Collections.Generic.List<Contact> target)
        {
            if (target.Count == 0)
                return;

            var message = this.MessageTrim;
            foreach (var contact in target)
            {
                contact.GetConversation().SendChat(message, this.Attachment);
                if (contact.ID != this.Contact.ID)
                    contact.CloseConversation();
            }
        }

        void IProgress.SetProgress(bool success, int current, int max)
        {
            this.ProgressCurrent = current;
            this.ProgressMax = max;
            if (!success)
            {
                if ((++m_retryTimes > 3 && max >= 16 * 1024) || this.IsTimedOut(DateTime.Now.Ticks)) // Possibly Mobile Data is not strong enough to send big data
                    this.TimedOut(); // Mark Failed, giving chance for another queue to be transmitted
                else
                    Network.Transmit(this);
                return;
            }
            else if (current == max)
                Network.NotifyIncomingChatStatus(Notification.CHAT_SENT, this.MessageId, false); // Temporary "Sent" status, set to false

            if (OnProgress != null)
                OnProgress.Invoke(this);
        }

        public void TimedOut()
        {
            Network.NotifyIncomingChatStatus(Notification.CHAT_SEND_FAIL, this.MessageId, true); // Set to true, must remove from PendingChat
        }

        public static ChatMessage Deserialize(Deserializer s)
        {
            Deserializer chat = null;
            if (!s.SecureExtract(ref chat, Core.CryptoKey))
                return null;

            string id = null;
            if (!chat.Extract(ref id) || id.Length < 6)
                return null;

            byte type = 0;
            if (!chat.Extract(ref type) || !Enums<TypeChat>.IsDefined(type))
                return null;

            int messageId = 0;
            if (!chat.Extract(ref messageId))
                return null;

            string message = null;
            if (!chat.Extract(ref message))
                return null;

            long time = 0;
            if (!chat.Extract(ref time))
                return null;

            var attachment = Attachment.Deserialize(chat);
            return new ChatMessage(Core.AddContactAuto(id), (TypeChat)type, messageId, message, attachment, new DateTime(DateTime.Now.Ticks - time), Notification.CHAT_DELIVERED, TypeDirection.IN);
        }

        private void Transmit(int timeOut)
        {
            ProgressCurrent = 0;
            ProgressMax = 0;
            var s = new Serializer();
            string id = this.Contact.ID; // Contact, Group, ServerPOS

            s.Add(id);
            s.Add((byte)this.Type);
            s.Add(this.MessageId);
            s.Add(Util.GuardValue(this.Message));
            if (this.Attachment == null || this.Attachment.FileName.Length == 0 || !System.IO.File.Exists(this.Attachment.FileName))
            {
                s.Add(string.Empty);
                s.Add(Util.EmptyBytes);
            }
            else
            {
                s.Add(System.IO.Path.GetExtension(this.Attachment.FileName));
                s.Add(System.IO.File.ReadAllBytes(this.Attachment.FileName));
            }
            var respBuf = new NSerializer(TypeHeader.CHAT);
            respBuf.SecureAdd(s, Core.CryptoKey); // Encrypt data!!

            Network.Enqueue(respBuf, timeOut, this);
        }

        internal const int TIMEOUT = 1000 * 60 * 120; // 120 minutes timeOut for non-Server
        internal const int TIMEOUT_POS = 1000 * 60 * 7; // 7 minutes timeOut for Server
        private int GetTimeOutMilliseconds()
        {
            return this.Contact.IsServerPOS ? TIMEOUT_POS : TIMEOUT; // 7 minutes timeOut for server, otherwise 120 minutes
        }

        public bool IsTimedOut(long timeNow)
        {
            return timeNow > this.Time.Ticks + (10000L * GetTimeOutMilliseconds());
        }

        internal void Transmit(bool save) // Internal Function, DO NOT use it
        {
            if (save)
                Core.AddPendingChat(this);

            this.Transmit(GetTimeOutMilliseconds());
        }

        public void Transmit()
        {
            Transmit(true);
        }
    }
}