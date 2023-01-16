using System;
using System.IO;

namespace ChatAPI
{
    public static partial class Core
    {
        private static string m_rootDB; // root database folder
        private static string m_rootContactDB; // root contact database folder
        private static string m_publicDataDir;
        private static string m_tempDir;

        internal static byte[] CryptoKey; // Owner key for message encrypt
        internal static byte[] CryptoKeyPassword; // Owner key password
        internal static int LoginToken;

        public static Action<LoginInfo> OnLogin;
        public static Action<ChatMessage> OnUnprocessedIncomingChat;
        //public static Action<Contact> OnLastActivity;
        public static Action<Feed> OnFeed;
        public static SettingDB Setting;
        public static StringDB BlockedContacts;

        public static void Initialize(string privateDataDir, string publicDataDir)
        {
            m_rootDB = privateDataDir;
            if (!Directory.Exists(privateDataDir))
                Directory.CreateDirectory(privateDataDir);

            m_publicDataDir = publicDataDir;
            if (!Directory.Exists(publicDataDir))
                Directory.CreateDirectory(publicDataDir);
                      
            m_rootContactDB = Path.Combine(privateDataDir, "contacts");
            EnsurePathAccessible(m_rootContactDB);
            Setting = new SettingDB(Path.Combine(privateDataDir, "setting"));
            m_tempDir = Path.Combine(privateDataDir, "temp");
            EnsurePathAccessible(m_tempDir);

            BlockedContacts = new StringDB(Path.Combine(privateDataDir, "blockedContacts"), StringEncoder.UTF8);
            if (!LoadOwner())
                Setting.ActivationState = ActivationState.NEED_ACTIVATION;

            LoadAllContactsInfo();
#if BUILD_PARTNER
            var currentPosId = Setting.Read("SID");
            if (!string.IsNullOrEmpty(currentPosId))
                ContactPOS.Current = FindContact(currentPosId) as ContactPOS;
            LoadBanner();
#endif
            InitializeChat();
        }

        public static string PublicDataDir
        {
            get { return m_publicDataDir; }
        }

        public static string TempDir
        {
            get { return m_tempDir; }
        }

        private static void EnsurePathAccessible(string path)
        {
#if __ANDROID__
            if (!Directory.Exists(path))
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    ClearAppData();
                }
#else
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
#endif
        }

        public static int UnreadChatCount
        {
            get { return UnprocessedIncomingChat.Count; }
        }

        public static void Broadcast(string message, Attachment attachment)
        {
            Broadcast(message, attachment, m_allContacts);
        }

        public static void Broadcast(string message, Attachment attachment, System.Collections.Generic.List<Contact> target)
        {
            if (target.Count == 0)
                return;

            var bc = new BroadcastMessage(message, attachment, target);
            bc.Transmit();
        }

        public static void CloseAllConversation(Contact exept)
        {
            string exeptId = exept == null ? null : exept.ID;
            foreach (var contact in m_allContacts)
            {
                if (contact.ID != Owner.ID && contact.ID != exeptId)
                    contact.CloseConversation();
            }
        }
    }
}