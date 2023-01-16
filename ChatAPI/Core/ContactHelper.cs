using System.IO;
using System.Collections.Generic;

namespace ChatAPI
{
    public static partial class Core
    {
        private static List<Contact> m_allContacts;
        private static StringDB m_allContactsId;

        internal static string GetContactDir(string id)
        {
            return Path.Combine(m_rootContactDB, id);
        }

        private static void PrepareContactDirectory(Contact contact)
        {
            var contactDir = GetContactDir(contact.ID);
            if (!Directory.Exists(contactDir))
                Directory.CreateDirectory(contactDir);
        }

        private static void LoadAllContactsInfo()
        {
            m_allContacts = new List<Contact>(); // create new contacts list

            if (m_allContactsId == null)
                m_allContactsId = new StringDB(Path.Combine(m_rootDB, "contacts.fdt"), StringEncoder.UTF8);

            //if (!Directory.Exists(m_rootContactDB))
            //    Directory.CreateDirectory(m_rootContactDB);

            int i = 0;
            while (i < m_allContactsId.Count)
            {
                var contact = LoadContactInfo(m_allContactsId[i]);
                if (contact == null)
                    m_allContactsId.RemoveAt(i); // remove junk
                else
                {
                    m_allContacts.Add(contact);
                    i++;
                }
            }
        }

        private static Contact LoadContactInfo(string id)
        {
            var contactDir = GetContactDir(id);
            if (!Directory.Exists(contactDir))
                return null;

            string name = null;
            string status = null;
            DP dp = null;
            string extraInfo = null;
            string username = null;
            uint accType = 0;
            long joinDate = 0;

            var infoFile = Path.Combine(contactDir, "info"); // New info file
            if (File.Exists(infoFile))
            {
                var s = new Deserializer(File.ReadAllBytes(infoFile));
                int dpHash = 0;
                if (s.Extract(ref name) && s.Extract(ref status) && s.Extract(ref dpHash))
                {
                    if (dpHash != 0)
                    {
                        var dpFile = GetContactDP(id);
                        if (File.Exists(dpFile))
                            dp = new DP(dpFile, dpHash);
                    }
                    if (s.Extract(ref extraInfo) && s.Extract(ref username) && s.Extract(ref accType)) s.Extract(ref joinDate);
                }
            }
            var contact = Contact.Create(id, name, status, dp, extraInfo, username, accType, joinDate);
            contact.IsBlocked = BlockedContacts.Contains(id);
            return contact;
        }

        private static string GetContactDP(string id)
        {
            return Path.Combine(GetContactDir(id), "dp");
        }

        internal static DP CreateDP(string id, byte[] imageData)
        {
            var dpFile = GetContactDP(id);
            var dpFileThumb = dpFile + "t";
            if (imageData == null)
            {
                if (File.Exists(dpFile))
                    File.Delete(dpFile);
                if (File.Exists(dpFileThumb))
                    File.Delete(dpFileThumb);
                return null;
            }
            else
            {
                File.WriteAllBytes(dpFile, imageData);
#if __ANDROID__
                var b1 = Android.Graphics.BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                var b2 = UIUtil.ResizeImage(b1, 128);
                if (b1 != b2) // b1 can be same ref as b2 when b1 size <= 128
                    b1.Recycle();
                File.WriteAllBytes(dpFileThumb, UIUtil.Compress(b2, 85));
                b2.Recycle();
#else
                var b = UIUtil.FromArray(imageData);
                File.WriteAllBytes(dpFileThumb, UIUtil.Compress(UIUtil.ResizeImage(b, 48), 90));
#endif
                return new DP(dpFile, Crypto.hashV2(imageData));
            }
        }

        public static void SaveContactInfo(Contact contact)
        {
            if (string.IsNullOrEmpty(m_rootContactDB))
                return; // contact not previously loaded

            var contactDir = GetContactDir(contact.ID);
            if (!Directory.Exists(contactDir))
                return; // contact not previously loaded

            var s = new Serializer();
            s.Add(contact.Name); // add name
            s.Add(contact.Status); // add status
            s.Add(contact.DP == null ? 0 : contact.DP.Hash); // add dpHash
            s.Add(contact.ExtraInfo);
            s.Add(contact.Username);
            s.Add(contact.AccType);
            s.Add(contact.JoinDate);
            using (var file = new FileStream(Path.Combine(contactDir, "info"), FileMode.Create))
                s.SaveTo(file);
        }

        public static Contact LoadServer(Contact server)
        {
            AddContact(server);
            var contact = LoadContactInfo(server.ID);
            return contact;
        }

        public static bool AddContact(Contact contact)
        {
            if ((Owner.ID == contact.ID) || m_allContactsId.Contains(contact.ID))
                return false;

            PrepareContactDirectory(contact);
            m_allContactsId.Add(contact.ID);
            m_allContacts.Add(contact);
            return true;
        }

        public static Contact AddContactSupport(string id, string name)
        {
            var contact = AddContactAuto(id, name, null);
            contact.IsSupport = true;
            return contact;
        }
        public static Contact AddContactAuto(string id)
        {
            return AddContactAuto(id, null, null);
        }
        public static Contact AddContactAuto(string id, string name, string username)
        {
            var contact = FindContact(id);
            if (contact == null)
            {
                contact = Contact.Create(id, name, username);
                AddContact(contact);
                SaveContactInfo(contact);
                SyncContact(contact);
            }
            return contact;
        }

        public static void RemoveContact(Contact contact)
        {
            contact.CloseConversation();
            var contactDir = GetContactDir(contact.ID);
            if (Directory.Exists(contactDir))
                Directory.Delete(contactDir, true);

            int index = m_allContactsId.IndexOf(contact.ID);
            if (index >= 0)
            {
                m_allContactsId.RemoveAt(index);
                index = FindContactIndex(contact.ID);
                if (index >= 0) m_allContacts.RemoveAt(index);
            }
            SyncContactDelete(contact);
        }

        private static void BackupAndRemoveContacts(string ownerID)
        {
            var arcDir = Path.Combine(m_rootDB, "arc_" + ownerID);
            if (Directory.Exists(arcDir))
                Directory.Delete(arcDir, true);
            Directory.CreateDirectory(arcDir);

            var all = m_allContacts;
            int count = all.Count;
            while (--count >= 0)
            {
                var contact = all[count];
                contact.CloseConversation();
                var contactDir = GetContactDir(contact.ID);
                if (Directory.Exists(contactDir))
                {
                    if (contact.IsBlocked)
                        try
                        {
                            Directory.Delete(contactDir, true);
                        }
                        catch { }
                }
            }

            UnprocessedIncomingChat.Clear();
            UnprocessedIncomingChatStatus.Clear();
            PendingChat.Clear();
            BlockedContacts.Clear();
            GetFeedDB().Clear();
            all.Clear();
            m_allContactsId.Clear();

            var recentFile = m_recentChat.Name;
            m_recentChat.Close();
            try
            {
                Directory.Move(m_rootContactDB, Path.Combine(arcDir, "contacts"));
                File.Move(recentFile, Path.Combine(arcDir, "recent.fdt"));
            }
            catch { }
            m_recentChat = new ChatMessageDB(recentFile);
            EnsurePathAccessible(m_rootContactDB);
        }

        private static bool RestoreContacts(string ownerID)
        {
            var arcDir = Path.Combine(m_rootDB, "arc_" + ownerID);
            if (!Directory.Exists(arcDir)) 
                return false;

            var recentFile = m_recentChat.Name;
            m_recentChat.Close();
            try
            {
                Util.DeleteFile(recentFile);
                File.Move(Path.Combine(arcDir, "recent.fdt"), recentFile);

                Directory.Delete(m_rootContactDB, true);
                Directory.Move(Path.Combine(arcDir, "contacts"), m_rootContactDB);
                var all = m_allContactsId;
                all.Clear();
                foreach (string folder in Directory.GetDirectories(m_rootContactDB))
                {
                    var id = Path.GetFileName(folder);
                    if (id != ownerID) all.Add(id);
                }
                return true;
            }
            catch
            {
                EnsurePathAccessible(m_rootContactDB);
            }
            return false;
        }

        public static Contact FindContact(string id)
        {
            int i = FindContactIndex(id);
            if (i >= 0) return m_allContacts[i];

            return null; // not found
        }

        public static Contact FindContactByUsername(string username)
        {
            foreach (var c in m_allContacts)
            {
                if (string.Equals(c.Username, username, System.StringComparison.InvariantCultureIgnoreCase))
                    return c;
            }
            return null;
        }

        private static int FindContactIndex(string id)
        {
            var all = m_allContacts;
            int count = all.Count;
            for (int i = 0; i < count; i++)
                if (string.Equals(all[i].ID, id))
                    return i;

            return -1; // not found
        }

        public static Contact[] GetSortedContacts()
        {
            var sorted = m_allContacts.ToArray();
            System.Array.Sort(sorted, Contact.Comparer);
            return sorted;
        }

        internal static List<Contact> GetContactSupport()
        {
            var list = new List<Contact>();
            if (Owner != null)
            {
                foreach (var c in m_allContacts)
                {
                    if (c.IsSupport)
                        list.Add(c);
                }
            }
            return list;
        }

        public static Contact[] FindContacts(string criteria)
        {
            var list = new List<Contact>();
            if (criteria.Length >= 2)
            {
                FindContactsByName(list, criteria);
                FindContactsByUsername(list, criteria);
            }
            return list.ToArray();
        }

        private static void FindContactsByUsername(List<Contact> list, string username)
        {
            foreach (var c in m_allContacts)
            {
                if (c.Username.IndexOf(username) >= 0 && list.IndexOf(c) < 0)
                    list.Add(c);
            }
        }

        private static void FindContactsByName(List<Contact> list, string name)
        {
            foreach (var c in m_allContacts)
            {
                if (!string.IsNullOrEmpty(c.Name) && c.Name.IndexOf(name, System.StringComparison.CurrentCultureIgnoreCase) >= 0 && list.IndexOf(c) < 0)
                    list.Add(c);
            }
        }

        public static void BlockContact(Contact contact)
        {
            if (!BlockedContacts.Contains(contact.ID))
                BlockedContacts.Add(contact.ID);

            contact.CloseConversation();
            contact.IsBlocked = true;
            SyncContactDelete(contact);
        }

        public static void UnblockCotact(Contact contact)
        {
            var index = BlockedContacts.IndexOf(contact.ID);
            if (index >= 0)
            {
                BlockedContacts.RemoveAt(index);
                SyncContact(contact);
                contact.IsBlocked = false;
            }
        }

        internal static void InvalidateContactOnlineState()
        {
            if (Owner == null)
                return;

            var timeNow = System.DateTime.Now.Ticks;
            foreach (var c in m_allContacts)
            {
                if(c.InvalidateOnlineState(timeNow) && Contact.OnUpdate != null)
                    Contact.OnUpdate.Invoke(c, Notification.USER_ONLINESTATE);
            }
        }
    }
}