namespace ChatAPI
{
    public static partial class Core
    {
        private static int m_lastSyncContact;
        public static void SyncAllContact()
        {
            if (Owner == null || !Network.IsLoggedIn)
                return;
           
            var thisTime = System.Environment.TickCount;
            if (m_lastSyncContact != 0 && thisTime - m_lastSyncContact < 2 * 60 * 1000) // 2 minutes
                return;

            m_lastSyncContact = thisTime;

            var respBuf = new NSerializer(TypeHeader.CONTACT_SYNC);
            SyncContact(Owner, respBuf);
            var list = m_allContacts;
            for (int i = 0; i < list.Count; i++)
            {
                var contact = list[i];
                if (!contact.IsBlocked && contact.ID.Length >= 6)
                    SyncContact(contact, respBuf);
            }
            Network.Send(respBuf);

#if BUILD_PARTNER
            SyncBanner();
#endif
        }

        public static void SyncContact(Contact contact)
        {
            if (!Network.IsLoggedIn || contact.IsBlocked || contact.ID.Length < 6)
                return;

            var respBuf = new NSerializer(TypeHeader.CONTACT_SYNC);
            SyncContact(contact, respBuf);
            Network.Send(respBuf); // Direct Send, DO NOT buffer it
        }

        private static void SyncContact(Contact contact, NSerializer buf)
        {
            buf.Add(contact.ID);
            buf.Add(contact.Status);
            buf.Add(contact.DP == null ? (int)0 : contact.DP.Hash);
            var name = contact.Name;
            buf.Add(name == contact.ID ? string.Empty : name);
            buf.Add(contact.Username);
            buf.Add(contact.AccType);
            buf.Add(contact.JoinDate); // Sync join date at utc format
        }

        private static void SyncContactDelete(Contact contact)
        {
            var respBuf = new NSerializer(TypeHeader.CONTACT_DELETE);
            respBuf.Add(contact.ID);
            Network.Enqueue(respBuf); // Important, must be buffered
        }
    }
}