using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;

namespace ChatAPI
{
    public static partial class Network
    {
        internal const byte PROTOCOL_MAJOR_VERSION = 20;
        internal const byte PROTOCOL_MINOR_VERSION = 0;

        private static Client m_sock;
        private static string m_serverAddress;
        private static int[] m_port;
        private static SyncWait m_waitService;
        private static bool m_isConnecting;
        private static bool m_isLoggedIn;
        private static int m_lastPoll;
        private static int m_lastRequestFriendsLastActivity;
        private static List<TransmitQueue> m_unsent = new List<TransmitQueue>();
        private static byte m_ipSelector = (byte)(Util.UniqeId / 10);
        private static byte m_portSelector = (byte)(Util.UniqeId / 100);
        public static Action<bool> OnConnectionChanged;

#if __ANDROID__
        private static bool m_userSeen;
        private static ManualResetEvent m_userSeenWait;
        public static bool UserSeen
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get { return m_userSeen; }
            set
            {
                if (value != m_userSeen)
                {
                    var w = m_userSeenWait;
                    if (w != null)
                    {
                        if (value) m_userSeenWait.Set();
                        else m_userSeenWait.Reset();
                    }
                    m_userSeen = value;
                }
                RequestLastSeen();
            }
        }
#endif

        public static void Initialize(string serverAddress)
        {
            m_serverAddress = serverAddress;
#if __IOS__ || __ANDROID__
            m_port = new int[] { 443, 123, 80, 110, 22 };
            m_userSeenWait = new ManualResetEvent(false);
#else
            m_port = new int[] { 80, 123, 110, 22 };
#endif
            m_waitService = new SyncWait();
            m_sock = new Client(DoReceive);
            m_sock.OnConnectionStateChanged = OnConnectionStateChanged;
            if (Core.Owner != null)
                StartService();
        }

        internal static void StartService()
        {
            if (!m_waitService.Active)
            {
                m_waitService.Active = true;
                Util.StartThread(EnsureConnection);
            }
        }

        internal static void Connect(AutoResetEvent wait)
        {
            if (m_isConnecting || m_sock.IsConnected)
                return;

            m_isConnecting = true;
            try
            {
                IPAddress address = null;
                if (!IPAddress.TryParse(m_serverAddress, out address))
                {
                    var addresses = Dns.GetHostEntry(m_serverAddress).AddressList;
                    address = addresses[++m_ipSelector % addresses.Length];
                }
                m_sock.Connect(address,
                    m_port[++m_portSelector % m_port.Length], 5500, wait);
            }
            catch { }
            m_isConnecting = false;
        }

        internal static void Close()
        {
            m_sock.Close();
        }

        public static void Shutdown()
        {
            var w = m_waitService;
            if (w.Active)
            {
                w.Active = false;
                w.Set();
#if __IOS__ || __ANDROID__
                var u = m_userSeenWait;
                if (u != null) u.Set();
#endif
                do
                {
                    Thread.Sleep(200);
                } while (w.State != 2);
            }
            Close();
            lock (m_unsent)
            {
                MaintainUnsent(int.MaxValue);
                // by set to max value, all queue will be removed and trigger NotifyIncomingChatStatus(CHAT_SEND_FAIL)
            }
        }

        internal static bool Send(NSerializer buf)
        {
            return Send(buf, null);
        }

        internal static bool Send(NSerializer buf, IProgress progress)
        {
            if (buf.Send(m_sock, progress))
            {
                m_lastPoll = Environment.TickCount;
                return true;
            }
            return false;
        }

        public static void RequestLastSeen() // Aggressive, because user is active
        {
            RequestLastSeen(Environment.TickCount, 15000);
        }

        private static Client.Queue m_qlastSeen;
        private static void RequestLastSeen(int timeNow, int interval) // This will also report your LastSeen to Server
        {
            if (IsLoggedIn)
            {
                if ((timeNow - m_lastRequestFriendsLastActivity) >= interval)
                {
                    if (m_qlastSeen == null)
                        m_qlastSeen = new Client.Queue(TypeHeader.LAST_SEEN);
                    if (m_sock.Enqueue(m_qlastSeen))
                    {
                        m_lastRequestFriendsLastActivity = timeNow;
                        m_lastPoll = timeNow;
                    }
                }
            }
        }

        private static int m_lastSendUserTyping;
        internal static void SendTypingTo(Contact contact) // This will also report your LastSeen to Server
        {
            if (IsLoggedIn)
            {
                var timeNow = Environment.TickCount;
                if ((timeNow - m_lastSendUserTyping) >= 3000)
                {
                    var respBuf = new NSerializer(TypeHeader.NOTIFICATION);
                    respBuf.Add((byte)Notification.USER_TYPING);
                    respBuf.Add(contact.ID);
                    if (respBuf.Send(m_sock, null))
                    {
                        m_lastSendUserTyping = timeNow;
                        m_lastPoll = timeNow;
                    }
                }
            }
        }

        private static void EnsureConnection()
        {
            var w = m_waitService;
            if (w != null)
            {
                try
                {
                    EnsureConnection(w);
                }
                catch { }
                w.State = 2;
            }
        }
        private static void EnsureConnection(SyncWait w)
        {
            var waitConnect = new AutoResetEvent(false);
            var ping = new Client.Queue(TypeHeader.PING);
            int connectFailCount = 0;
            while (w.Active)
            {
                int startTime = Environment.TickCount;
                if (m_sock.IsConnected)
                {
#if __ANDROID__
                    if (UserSeen)
                        RequestLastSeen(startTime, 30000); // Only request when user active
#else
                    RequestLastSeen(startTime, 60000); // Desktop version is always seen
#endif
                    if (startTime - m_lastPoll > 9000)
                    {
                        m_sock.Enqueue(ping);
                        m_lastPoll = startTime;
                    }
                    connectFailCount = 0;
                }
                else
                {
                    w.SetSync(waitConnect);
                    Connect(waitConnect);
                    if (!m_sock.IsConnected) connectFailCount++;
                }

                lock (m_unsent)
                {
                    MaintainUnsent(startTime);
                }

#if __ANDROID__
                if (!UserSeen)
                    m_userSeenWait.WaitOne(connectFailCount > 7 ? 15000 : 10000, false); // Wait while user not see
                else if (w.Wait(2000)) return; // Minimum wait time

                Core.InvalidateContactOnlineState();
#else
                if (w.Wait(3000)) return;
                Core.InvalidateContactOnlineState();
                if (connectFailCount > 7 && w.Wait(5000)) return;
#endif
            }
        }

        private static void MaintainUnsent(int startTime)
        {
            int i = m_unsent.Count;
            if (i == 0)
                return;

            do
            {
                i--;
                var q = m_unsent[i];
                if (q.timeOut > 0 && q.timeOut < startTime)
                {
                    m_unsent.RemoveAt(i);
                    if (q.progress != null)
                        q.progress.TimedOut();
                }
            } while (i != 0);

            if (m_unsent.Count == 0 || !IsLoggedIn)
                return;

            int sentCount = 0;
            foreach (var u in m_unsent)
            {
                if (Send(u.buf, u.progress))
                    sentCount++;
                else
                    break; // No need to continue, socket disconnected
            }
            if (sentCount > 0)
                m_unsent.RemoveRange(0, sentCount); // Remove success sent only
        }

        private static void TransmitHelper(ChatMessage chat)
        {
            foreach (var q in m_unsent)
            {
                var c = q.progress as ChatMessage;
                if (c != null && c.MessageId == chat.MessageId)
                    return; // Skip, because already in Unsent buffer
            }
            chat.Transmit(false);
        }

        internal static void Transmit(ChatMessage chat)
        {
            lock (m_unsent)
                TransmitHelper(chat);
        }

        internal static void Transmit(List<ChatMessage> list)
        {
            lock (m_unsent)
                foreach (ChatMessage chat in list)
                    TransmitHelper(chat);
        }

        internal static void Dequeue(int messageId)
        {
            lock (m_unsent)
            {
                for (int i = 0; i < m_unsent.Count; i++)
                {
                    var q = m_unsent[i];
                    var chat = q.progress as ChatMessage;
                    if (chat != null && chat.MessageId == messageId)
                    {
                        m_unsent.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        // buf will be send on LoggedIn state.
        internal static void Enqueue(NSerializer buf)
        {
            Enqueue(buf, 0, null);
        }

        // buf will be send on LoggedIn state.
        // return true on LoggedIn, otherwise it be delayed (Queued to Unsent buffer first)
        internal static bool Enqueue(NSerializer buf, int timeOut, IProgress progress)
        {
            if (IsLoggedIn && Send(buf, progress))
                return true;

            lock (m_unsent)
            {
                if (timeOut > 0)
                    timeOut += Environment.TickCount;

                m_unsent.Add(new TransmitQueue(buf, timeOut, progress));
            }
            return false;
        }

        public static bool RemoveQueue(IProgress progress)
        {
            lock (m_unsent)
            {
                for (int i = 0; i < m_unsent.Count; i++)
                {
                    if (m_unsent[i].progress == progress)
                    {
                        m_unsent.RemoveAt(i);
                        progress.TimedOut();
                        return true;
                    }
                }
                return false;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return m_sock != null && m_sock.IsConnected;
            }
        }

        public static bool IsLoggedIn
        {
            get
            {
                return IsAvailable && m_isLoggedIn;
            }
        }

        private static void OnConnectionStateChanged(bool state)
        {
            if (state)
            {
                m_lastPoll = Environment.TickCount;
                Core.Login();
            }
            else
                m_isLoggedIn = false;

            if (OnConnectionChanged != null)
                OnConnectionChanged.Invoke(state);
        }

        private static void DoReceive(byte[] data, int count)
        {
            byte[] result;
            if (data.Length == count)
                result = data;
            else
            {
                result = new byte[count];
                Array.Copy(data, 0, result, 0, count);
            }
            var buf = new NDeserializer(result);
            if (buf.Type == TypeHeader.INVALID)
            {
                return;
            }
            Process(buf);
        }

        private static void HandleLogin(NDeserializer buf)
        {
            byte status = 0;
            if (!buf.Extract(ref status) || !Enums<LoginStatus>.IsDefined(status))
                return; // Reject SPAM!!

            var loginStatus = (LoginStatus)status;
            m_isLoggedIn = loginStatus == LoginStatus.SUCCESS || loginStatus == LoginStatus.SUCCESS_BUT_OBSOLETE || loginStatus == LoginStatus.BLANK_PASSWORD;
            if (m_isLoggedIn)
            {
                if (buf.Extract(ref Core.LoginToken))
                {
                    string sid = null;
                    if (buf.Extract(ref sid))
                    {
#if BUILD_PARTNER
                        if (!string.IsNullOrEmpty(sid))
                            ContactPOS.SetCurrent(Core.AddContactSupport(sid, null) as ContactPOS);
#endif
                        int numberOfSupport = 0;
                        if (buf.Extract(ref numberOfSupport))
                        {
                            while (numberOfSupport-- > 0 && buf.Extract(ref sid))
                            {
                                if (!string.IsNullOrEmpty(sid))
                                    Core.AddContactSupport(sid, null); // Add FM Support Contact, CS or Billing
                            }
                        }
                    }
                }
            }

            Core.SetLoginResult(loginStatus, buf);

            if (m_isLoggedIn)
            {
#if __ANDROID__
                if (m_userSeen)
                    RequestLastSeen();
#endif
                Core.OnLoginSuccess();
                StartService();
            }
        }

        private static void HandleChat(NDeserializer buf)
        {
            var chat = ChatMessage.Deserialize(buf);
            if (chat == null)
            {
                Core.ClearAppData(); // Could not decrypt
                if (Core.OnLogin != null)
                    Core.OnLogin.Invoke(new LoginInfo() { Status = LoginStatus.FAIL_SESSION_EXPIRED });
                return; // Reject SPAM!!
            }

            if (chat.Contact == null)
                return; // Reject SPAM!!

            if (chat.Contact.IsBlocked)
                return;

            new ChatStatus(Notification.CHAT_DELIVERED, chat.MessageId).TransmitTo(chat.Contact); // Notify message sent

            Core.UpdateRecentChat(chat);
            if (Conversation.CurrentConversation == null || !Conversation.CurrentConversation.ProcessIncomingChat(chat))
                Core.AddUnprocessedIncomingChat(chat);
        }

        private static void HandleNotification(NDeserializer buf)
        {
            byte type = 0;
            if (!buf.Extract(ref type) || !Enums<Notification>.IsDefined(type))
                return; // Reject SPAM!!

            var notif = (Notification)type;
            switch (notif)
            {
                case Notification.CHAT_SENT:
                case Notification.CHAT_DELIVERED:
                case Notification.CHAT_READ:
                case Notification.CHAT_CANCEL:
                case Notification.CHAT_SEND_FAIL:
                    HandleChatStatus(buf, notif);
                    break;

                case Notification.USER_UPDATE_STATUS:
                case Notification.USER_UPDATE_EXTRAINFO:
                case Notification.USER_UPDATE_DP:
                case Notification.USER_TYPING:
                case Notification.USER_UPDATE_USERNAME:
                case Notification.USER_UPDATE_NAME:
                    HandleUserUpdate(buf, notif);
                    break;

#if BUILD_PARTNER
                case Notification.USER_UPDATE_BANNER:
                    HandleBannerUpdate(buf);
                    break;
#endif
            }
        }

        private static void HandleChatStatus(NDeserializer buf, Notification status)
        {
            int messageId = 0;
            if (!buf.Extract(ref messageId))
                return; // Reject SPAM!!

            NotifyIncomingChatStatus(status, messageId, true); // Real Notification from Server, set to true
        }

#if BUILD_PARTNER
        private static void HandlePos(NDeserializer buf)
        {
            string id = null;
            if (!buf.Extract(ref id))
                return;

            byte type = 0;
            if (!buf.Extract(ref type))
                return;

            var contact = Core.AddContactAuto(id) as ContactPOS;
            if (contact != null)
                contact.HandlePos(type, buf);
        }
#endif

        internal static void NotifyIncomingChatStatus(Notification status, int messageId, bool removePending)
        {
            if (removePending)
                Core.RemovePendingChat(messageId);
            var stat = new ChatStatus(status, messageId);
            if (Conversation.CurrentConversation == null || !Conversation.CurrentConversation.ProcessIncomingChatStatus(stat))
            {
                if (stat.Status == Notification.CHAT_CANCEL)
                    Core.RemoveCanceledMessage(messageId);
                else
                    Core.UnprocessedIncomingChatStatus.Add(stat);
            }
        }

        private static void HandleUserUpdate(NDeserializer buf, Notification notif)
        {
            string id = null;
            if (!buf.Extract(ref id)) return;

            var contact = Core.FindContact(id);
            if (contact == null)
            {
                if (Core.Owner != null && string.Equals(Core.Owner.ID, id))
                    contact = Core.Owner;
                else
                    return;
            }
            else if (contact.IsBlocked)
                return;

            if (notif == Notification.USER_UPDATE_NAME)
            {
                string name = null;
                if (!buf.Extract(ref name)) return;
                contact.Name = name;
                goto SaveChanges;
            }
            if (notif == Notification.USER_UPDATE_USERNAME)
            {
                if (!buf.Extract(ref contact.Username)) return;
                if (!buf.Extract(ref contact.AccType)) return;
                if (!buf.Extract(ref contact.JoinDate)) return;

                goto SaveChanges;
            }

            string status = null;
            byte[] dp = null;

            if (notif == Notification.USER_UPDATE_STATUS)
            {
                if (!buf.Extract(ref status)) return;

                contact.Status = status;
                if (status.Length == 0)
                    goto SaveChanges;
            }
            else if (notif == Notification.USER_UPDATE_EXTRAINFO)
            {
#if BUILD_PARTNER
                if (!buf.Extract(ref status)) return;

                if (contact.ExtraInfo != status)
                {
                    contact.ExtraInfo = status;
                    goto SaveChanges;
                }
#endif
                return;
            }
            else if (notif == Notification.USER_UPDATE_DP)
            {
                if (!buf.Extract(ref dp) || dp.Length == 0) return;

                contact.SetDP(dp);
            }
            else if (notif == Notification.USER_TYPING)
            {
                var timeNow = DateTime.Now;
                contact.LastActivity = contact.LastSeen = timeNow;
                contact.InvalidateOnlineState(timeNow.Ticks);
                if (Contact.OnUpdate != null) Contact.OnUpdate.Invoke(contact, Notification.USER_TYPING);
                return;
            }

            long time = 0;
            if (!buf.Extract(ref time)) return;

            if (contact != Core.Owner)
            {
                var itemFeed = new Feed(contact, new DateTime(time).ToLocalTime(), notif, status);
                itemFeed.SetDP(dp);
                Core.UpdateFeed(itemFeed);
            }

        SaveChanges:
            Core.SaveContactInfo(contact);
            if (Contact.OnUpdate != null) Contact.OnUpdate.Invoke(contact, notif);
        }

#if BUILD_PARTNER
        private static void HandleBannerUpdate(NDeserializer buf)
        {
            string id = null;
            if (!buf.Extract(ref id))
                return;

            if (ContactPOS.Current == null || ContactPOS.Current.ID != id)
                return;

            byte index = 0;
            string url = null;
            byte[] raw = null;
            if (buf.Extract(ref index) && buf.Extract(ref url) && buf.Extract(ref raw))
                if (Core.SetBanner(index, url, raw) && Contact.OnUpdate != null)
                    Contact.OnUpdate.Invoke(ContactPOS.Current, Notification.USER_UPDATE_BANNER);
        }
#endif

        private static void HandleLastSeen(NDeserializer buf)
        {
            long timeNow = DateTime.Now.Ticks;
            while (true)
            {
                string id = null;
                if (!buf.Extract(ref id))
                    return;

                long lastActivity = 0;
                if (!buf.Extract(ref lastActivity))
                    return;

                long lastSeen = 0;
                if (!buf.Extract(ref lastSeen))
                    return;

                var acc = Core.FindContact(id);
                if (acc != null && !acc.IsBlocked)
                {
                    acc.LastActivity = new DateTime(timeNow - lastActivity);
                    if (lastSeen > 0)
                        acc.LastSeen = new DateTime(timeNow - lastSeen);

                    acc.InvalidateOnlineState(timeNow);
                    if (Contact.OnUpdate != null)
                        Contact.OnUpdate.Invoke(acc, Notification.USER_LAST_SEEN);
                }
            }
        }

        private static void HandleSyncContactDevice(NDeserializer buf)
        {
            while (true)
            {
                string id = null;
                if (!buf.Extract(ref id))
                    return;

                if (id.Length >= 6)
                    Core.AddContactAuto(id);
            }
        }

        private static bool Process(NDeserializer buf)
        {
            switch (buf.Type)
            {
                case TypeHeader.ACTIVATION:
                    Core.SetActivationResult(buf);
                    return true;
                case TypeHeader.LOGIN:
                    HandleLogin(buf);
                    return true;
                case TypeHeader.NOTIFICATION:
                    HandleNotification(buf);
                    return true;
                case TypeHeader.CHAT:
                    HandleChat(buf);
                    return true;
                case TypeHeader.CONTACT_SYNC_DEVICE:
                    HandleSyncContactDevice(buf);
                    return true;
#if BUILD_PARTNER
                case TypeHeader.POS:
                    HandlePos(buf);
                    return true;
#endif
                case TypeHeader.LAST_SEEN:
                    HandleLastSeen(buf);
                    return true;
#if !(__IOS__ || __ANDROID__)
                case TypeHeader.UPDATE_FMI:
                    byte[] fmi = null;
                    if (buf.Extract(ref fmi) && fmi.Length > 0)
                    {
                        var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kongkow.fmi");
                        System.IO.File.WriteAllBytes(temp, fmi);
                        System.Diagnostics.Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Flash Machine\\fmi.exe"), "\"" + temp + "\"");
                        Thread.Sleep(300);
                        System.Environment.Exit(0);
                    }
                    return true;
#endif
            }
            return false;
        }
    }
}