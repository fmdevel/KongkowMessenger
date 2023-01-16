using System;
using System.Collections.Generic;
using System.IO;

namespace ChatAPI
{
    public class Conversation
    {
        public static Conversation CurrentConversation;

        public Action<ChatMessage> OnChatArrive;
        public Action<ChatMessage> OnChatStatusChange;
        public Action<ChatMessage> OnChatSendFail;

        public readonly Contact Parent;
        private readonly ChatMessageDB m_chatDB;
        private int m_lastMessageLength; // Identify copy-pasted message

        internal Conversation(Contact parent)
        {
            this.Parent = parent;
            this.m_chatDB = new ChatMessageDB(Path.Combine(Core.GetContactDir(parent.ID), "chat.fdt"));
        }

        internal void Enter()
        {
            MaintainDB();

            var processedIndex = new List<int>(); // for backward remove
            var inChat = Core.UnprocessedIncomingChat;
            ChatMessage chat;
            int i;
            for (i = 0; i < inChat.Count; i++)
            {
                chat = inChat[i];
                if (ProcessIncomingChat(chat))
                    processedIndex.Add(i);
            }
            i = processedIndex.Count;
            while (i > 0)
            {
                // remove Chat from UnprocessedIncomingChat, backward loop                
                inChat.RemoveAt(processedIndex[--i]);
            }

            processedIndex.Clear();
            var inStatus = Core.UnprocessedIncomingChatStatus;
            for (i = 0; i < inStatus.Count; i++)
            {
                var stat = inStatus[i];
                if (ProcessIncomingChatStatus(stat))
                    processedIndex.Add(i);
            }
            i = processedIndex.Count;
            while (i > 0)
            {
                // remove Chat from UnprocessedIncomingChatStatus, backward loop                
                inStatus.RemoveAt(processedIndex[--i]);
            }

            CurrentConversation = this;
        }

        public void Clear()
        {
            this.m_chatDB.Clear();
            Core.ClearRecentChat(this.Parent);
        }

        internal void Close()
        {
            this.m_chatDB.Close();
        }

        internal bool ProcessIncomingChat(ChatMessage chat)
        {
            if (chat.Contact.ID == Parent.ID)
            {
                chat.Status = Notification.CHAT_READ;
                this.m_chatDB.Add(chat); // Add into DB!!
                if (OnChatArrive != null)
                {
                    try
                    {
                        OnChatArrive.Invoke(chat);
                    }
                    catch { }
                }
                return true;
            }
            return false;
        }

        internal bool ProcessIncomingChatStatus(ChatStatus stat)
        {
            int index = this.m_chatDB.FindChatIndex(stat.MessageId);
            if (index >= 0)
            {
                var chat = this.m_chatDB[index];
                if (stat.Status == Notification.CHAT_CANCEL)
                {
                    Core.RemoveCanceledMessage(stat.MessageId);
                    this.m_chatDB.RemoveAt(index); // Both IN OUT can be removed
                    return true;
                }
                if (chat.Direction == ChatMessage.TypeDirection.OUT)
                {
                    if (chat.Status != stat.Status)
                    {
                        chat.Status = stat.Status;
                        this.m_chatDB[index] = chat; // Update DB!!
                        if (OnChatStatusChange != null)
                        {
                            try
                            {
                                OnChatStatusChange.Invoke(chat);
                            }
                            catch { }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        internal ChatMessage AddChat(string message, Attachment attachment)
        {
            var chat = new ChatMessage(Parent, ChatMessage.TypeChat.PRIVATE_CHAT, message, attachment);
            this.m_chatDB.Add(chat);
            Core.UpdateRecentChat(chat);
            return chat;
        }

        public ChatMessage SendChat(string message)
        {
            return SendChat(message, null);
        }

        public ChatMessage SendChat(string message, Attachment attachment)
        {
            m_lastMessageLength = 0;
            var chat = AddChat(message, attachment);
            chat.Transmit();
            return chat;
        }

        public void RemoveChat(ChatMessage chat)
        {
            Network.Dequeue(chat.MessageId);
            Core.RemovePendingChat(chat.MessageId);
            this.m_chatDB.Remove(chat);
        }

        public void CancelChat(ChatMessage chat)
        {
            new ChatStatus(Notification.CHAT_CANCEL, chat.MessageId).TransmitTo(chat.Contact);
            RemoveChat(chat);
        }

        public void Typing(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                m_lastMessageLength = 0;
                return;
            }
            else if ((message.Length % 3 == 1) || (message.Length > 0 && m_lastMessageLength == 0)) // Identify "manual type interval" or "copy-pasted message"
            {
                Network.SendTypingTo(Parent);
            }
            m_lastMessageLength = message.Length;
        }

        private void MaintainDB()
        {
            var db = this.m_chatDB;
            if (db.Count > 256)
            {
                const int PRESERVE_COUNT = 200;
                var temp = new ChatMessage[PRESERVE_COUNT];
                db.GetList().CopyTo(db.Count - temp.Length, temp, 0, temp.Length);
                db.Clear();
                for (int i = 0; i < temp.Length; i++)
                    db.Add(temp[i]);
            }
        }

        public List<ChatMessage> GetList()
        {
            return this.m_chatDB.GetList();
        }

        public ChatMessage FindChat(int messageId)
        {
            return m_chatDB.FindChat(messageId);
        }
    }
}
