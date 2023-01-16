using System;

namespace ChatAPI
{
    public partial class Contact
    {
        public delegate void OnUpdateDelegate(Contact contact, Notification typeUpdate);
        public static OnUpdateDelegate OnUpdate;
        // public static Action<Contact> OnTyping;

        public readonly string ID;
        public string Username;
        public long JoinDate;
        public uint AccType;
        private string m_name;
        private string m_status;
        private DP m_DP;
        private string m_extraInfo;
        public bool IsBlocked;
        private Conversation m_conversation;
        public DateTime LastActivity;
        public DateTime LastSeen;
        private bool m_online;
        public bool IsSupport;

        public static Contact Create(string id, string name, string username)
        {
            return Create(id, name, null, null, null, username, 0, 0);
        }
        public static Contact Create(string id, string name, string status, DP dp, string extraInfo, string username, uint accType, long joinDate)
        {
            if (id.IndexOf('_') > 0)
                return new ContactPOS(id, name, status, dp, extraInfo, joinDate);

            return new Contact(id, name, status, dp, extraInfo, username, accType, joinDate);
        }

        protected Contact(string id, string name, string status, DP dp, string extraInfo, string username, uint accType, long joinDate)
        {
            this.ID = id.ToLower();
            this.Username = string.IsNullOrEmpty(username) ? this.ID : username;
            if (string.IsNullOrEmpty(name))
            {
                if (this.IsServerPOS)
                {
                    var names = id.ToUpper().Split('_');
                    name = string.Join(" ", names, 0, names.Length - 1);
                }
                else
                    name = id;
            }
            this.m_name = name;
            this.m_status = status;
            this.m_DP = dp;
            this.m_extraInfo = extraInfo;
            this.AccType = accType;
            this.JoinDate = joinDate;
        }

        //public static bool operator ==(Contact a, Contact b)
        //{
        //    return (object)a == (object)b || ((object)a != null && (object)b != null && string.Equals(a.ID, b.ID));
        //}
        //public static bool operator !=(Contact a, Contact b)
        //{
        //    return !(a == b);
        //}
        public override bool Equals(object obj)
        {
            var b = obj as Contact;
            return (object)b != null && string.Equals(this.ID, b.ID);
        }

        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }

        public bool IsServerPOS
        {
            get { return this is ContactPOS; }
        }

        public bool IsActive
        {
            get { return !IsBlocked && (m_DP != null || !string.IsNullOrEmpty(m_status)); }
        }

        public bool IsOnline
        {
            get { return this == Core.Owner ? Network.IsLoggedIn : m_online; }
        }

        //public void InvalidateOnlineState()
        //{
        //    InvalidateOnlineState(DateTime.Now.Ticks);
        //}

        public bool InvalidateOnlineState(long timeNow)
        {
            var minutesSpan = (timeNow - this.LastActivity.Ticks) / 600000000;
            var online = (minutesSpan >= 0 && minutesSpan < 2);
            if (online != m_online)
            {
                m_online = online;
                return true;
            }
            return false;
        }

        public static int Comparer(Contact a, Contact b)
        {
            if (!a.IsActive)
            {
                if (b.IsActive) return 1;
            }
            else if (!b.IsActive)
                return -1;

            if (!a.IsServerPOS)
            {
                if (b.IsServerPOS) return 1;
            }
            else if (!b.IsServerPOS)
                return -1;

            return string.Compare(a.m_name, b.m_name, true);
        }

        public string PublicName // Give username (if available) for public viewer instead of private name
        {
            get
            {
                return ID == Username ? Name : Username; // If (ID == Username) that means Username not available
            }
        }

        public string Name
        {
            get
            {
                return Util.GuardValue(this.m_name);
            }
            set
            {
                this.m_name = Util.GuardValue(value);
            }
        }

        public string Status
        {
            get
            {
                var stat = Util.GuardValue(this.m_status);
                if (this.IsBlocked)
                    return "TERBLOKIR";
                else if (stat == "TERBLOKIR")
                    this.m_status = stat = string.Empty;

                return stat;
            }
            set
            {
                this.m_status = Util.GuardValue(value);
            }
        }

        public string ChatStatus
        {
            get
            {
                var status = this.Status;
                var last = Util.LocalFormatDateStatus(this.LastSeen);
                //if (string.IsNullOrEmpty(last))
                //{
                //    var lastChat = this.GetRecentChatIn();
                //    if (lastChat != null)
                //        last = Util.LocalFormatDateStatus(lastChat.Time);
                //}
                if (string.IsNullOrEmpty(status))
                    status = last;
                else if (!string.IsNullOrEmpty(last))
                    status = status + " | " + last;

                return status;
            }
        }

        public DP DP
        {
            get
            {
                return this.IsBlocked ? null : this.m_DP;
            }
        }

        public void SetDP(byte[] imageData)
        {
#if __ANDROID__
            int update = this.m_DP == null ? 0 : this.m_DP.Update;
#endif
            this.m_DP = Core.CreateDP(this.ID, imageData);
#if __ANDROID__
            this.m_DP.Update = update + 1;
#endif
        }

        //public string Address
        //{
        //    get
        //    {
        //        return Util.GuardValue(this.m_address);
        //    }
        //    set
        //    {
        //        this.m_address = value;
        //    }
        //}

        //public string PostalCode
        //{
        //    get
        //    {
        //        return Util.GuardValue(this.m_postalCode);
        //    }
        //    set
        //    {
        //        this.m_postalCode = value;
        //    }
        //}

        public string ExtraInfo
        {
            get
            {
                return Util.GuardValue(this.m_extraInfo);
            }
            set
            {
                this.m_extraInfo = value;
#if BUILD_PARTNER
                if (!this.Equals(ContactPOS.Current))
                    return;

                var s = value;
                if (s.Length > 0)
                {
                    var o = Util.CommonSplit(s);
                    if (o.Length >= 4)
                    {
                        s = o[3].Replace(" ", null);
                        if (s.Length > 0)
                        {
                            o = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var id in o)
                            {
                                if (id.Length > 0)
                                    Core.AddContactSupport(id, this.Name);
                            }
                        }
                    }
                }
#endif
            }
        }

        public Conversation GetConversation()
        {
            if (m_conversation == null)
                m_conversation = new Conversation(this);

            return m_conversation;
        }

        public void EnterConversation()
        {
            if (m_conversation != null)
                m_conversation.Enter();
        }

        public void LeaveConversation()
        {
            if (m_conversation != null && (Conversation.CurrentConversation != null) && Conversation.CurrentConversation.Parent == this)
            {
                Conversation.CurrentConversation = null;
                // BUG FIX RECENT CHAT
                var list = m_conversation.GetList();
                if (list.Count > 0)
                {
                    var chat = list[list.Count - 1];
                    if (chat.Direction == ChatMessage.TypeDirection.IN)
                    {
                        //chat.Status = Notification.CHAT_READ;
                        Core.UpdateRecentChat(chat);
                    }
                }
            }
        }

        //[System.Obsolete()]
        //private ChatMessage GetRecentChatIn()
        //{
        //    if (m_conversation != null)
        //    {
        //        var list = m_conversation.GetList();
        //        var count = list.Count;
        //        while (count > 0)
        //        {
        //            count--;
        //            var chat = list[count];
        //            if (chat.Direction == ChatMessage.TypeDirection.IN)
        //                return chat;
        //        }
        //    }
        //    return null;
        //}

        public void CloseConversation()
        {
            if (m_conversation != null)
            {
                LeaveConversation();
                m_conversation.Close();
            }
            m_conversation = null;
        }
    }
}
