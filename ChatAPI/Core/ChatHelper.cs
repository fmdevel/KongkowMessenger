using System.IO;

namespace ChatAPI
{
    public static partial class Core
    {
        internal static ChatMessageDB UnprocessedIncomingChat;
        internal static ChatStatusDB UnprocessedIncomingChatStatus;
        internal static ChatMessageDB PendingChat;
        private static ChatMessageDB m_recentChat;

        private static void InitializeChat()
        {
            InitRecentChat();
            UnprocessedIncomingChat = new ChatMessageDB(Path.Combine(m_rootDB, "unchat.fdt"));
            UnprocessedIncomingChatStatus = new ChatStatusDB(Path.Combine(m_rootDB, "unchats.fdt"));
            PendingChat = new ChatMessageDB(Path.Combine(m_rootDB, "pending.fdt"));
        }

        private static void InitRecentChat()
        {
            m_recentChat = new ChatMessageDB(Path.Combine(m_rootDB, "recent.fdt"));
        }

        public static void UpdateRecentChat(ChatMessage chat)
        {
            var id = chat.Contact.ID;
            var r = m_recentChat;
            for (int i = 0; i < r.Count; i++)
                if (string.Equals(r[i].Contact.ID, id))
                {
                    r[i] = chat; // Update DB!!
                    return;
                }

            r.Add(chat);
        }

        public static ChatMessage GetRecentChat(Contact contact)
        {
            var r = m_recentChat;
            for (int i = 0; i < r.Count; i++)
                if (r[i].Contact.ID == contact.ID)
                    return r[i];

            return null;
        }

        public static void ClearRecentChat(Contact contact)
        {
            var r = GetRecentChat(contact);
            if (r != null)
                m_recentChat.Remove(r);
        }

        public static ChatMessage[] GetSortedRecentChats()
        {
            var r = m_recentChat;
            var count = r.Count;
            while (--count >= 0)
            {
                var item = r[count];
                if (item == null || m_allContactsId.IndexOf(item.Contact.ID) < 0 || item.Contact.IsBlocked)
                    r.RemoveAt(count); // remove non-existance or blocked
            }
            var sorted = r.GetList().ToArray();
            System.Array.Sort(sorted, ChatMessage.CompareTimeReverse);
            return sorted;
        }

        internal static void RemoveCanceledMessage(int messageId)
        {
            var i = UnprocessedIncomingChat.FindChatIndex(messageId);
            if (i < 0)
                return;

            var chat = UnprocessedIncomingChat[i];
            UnprocessedIncomingChat.RemoveAt(i);
            var recentChat = GetRecentChat(chat.Contact);
            if (recentChat != null && recentChat.MessageId == messageId)
            {
                ClearRecentChat(chat.Contact);
                if (OnUnprocessedIncomingChat != null)
                    OnUnprocessedIncomingChat.Invoke(null);
            }
        }

        internal static void AddUnprocessedIncomingChat(ChatMessage chat)
        {
            UnprocessedIncomingChat.Add(chat);
            if (OnUnprocessedIncomingChat != null)
                OnUnprocessedIncomingChat.Invoke(chat);
        }

        internal static void AddPendingChat(ChatMessage chat)
        {
            lock (PendingChat)
            {
                if (PendingChat.FindChat(chat.MessageId) != null)
                    return;

                PendingChat.Add(chat);
            }
        }

        internal static void RemovePendingChat(int messageId)
        {
            lock (PendingChat)
            {
                int index = PendingChat.FindChatIndex(messageId);
                if (index >= 0)
                    PendingChat.RemoveAt(index);
            }
        }

        internal static void ResendPendingChat()
        {
            lock (PendingChat)
            {
                var i = PendingChat.Count;
                if (i == 0)
                    return;

                var timeNow = System.DateTime.Now.Ticks;
                do
                {
                    i--;
                    var chat = PendingChat[i];
                    if (chat.IsTimedOut(timeNow))
                    {
                        PendingChat.RemoveAt(i); // Remove it
                        Network.NotifyIncomingChatStatus(Notification.CHAT_SEND_FAIL, chat.MessageId, false); // Set to false, because it already removed
                    }
                } while (i != 0);

                if (PendingChat.Count > 0)
                    Network.Transmit(PendingChat.GetList());
            }
        }
    }
}