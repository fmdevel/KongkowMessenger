using System.IO;

namespace ChatAPI
{
    public static partial class Core
    {
        public static Contact Owner;

        private static bool LoadOwner()
        {
            Owner = null; // reset owner
            CryptoKey = null;
            CryptoKeyPassword = null;

            string ownerFile = Path.Combine(m_rootDB, "owner");
            if (!File.Exists(ownerFile))
                return false;
            var ownerID = StringEncoder.UTF8.GetString(File.ReadAllBytes(ownerFile));
            if (ownerID.Length < 6)
                return false;

            var cryptoKey = LoadKey(Path.Combine(m_rootDB, "crypto"));
            if (cryptoKey == null)
                return false;

            var owner = LoadContactInfo(ownerID);
            if (owner == null)
                return false;

            Owner = owner;
            CryptoKey = cryptoKey;
            CryptoKeyPassword = LoadKey(Path.Combine(m_rootDB, "pw"));
            Setting.ActivationState = ActivationState.ACTIVATED;
            return true;

        }

        private static void SaveOwner(string ownerID, string name, string username, byte[] cryptoKey, byte[] cryptoKeyPassword, uint accType, long joinDate)
        {
            m_lastSyncContact = 0;
            File.WriteAllBytes(Path.Combine(m_rootDB, "owner"), StringEncoder.UTF8.GetBytes(ownerID));
            CryptoKey = cryptoKey;
            SaveKey("crypto", cryptoKey);
            SavePassword(cryptoKeyPassword);

            bool restored = RestoreContacts(ownerID);
            if (restored) LoadAllContactsInfo();

            var owner = LoadContactInfo(ownerID);
            if (owner == null)
            {
                owner = Contact.Create(ownerID, name, null, null, null, username, accType, joinDate);
                Directory.CreateDirectory(GetContactDir(ownerID));
            }
            else
            {
                owner.Name = name;
                owner.Username = username;
                owner.AccType = accType;
                owner.JoinDate = joinDate;
            }
            SaveContactInfo(owner);
            Owner = owner;
            if (restored) InitRecentChat();
            Setting.ActivationState = ActivationState.ACTIVATED;
        }

        private unsafe static void SaveKey(string fileName, byte[] key)
        {
            var hwId = Util.HwUniqueId;
            var buf = new byte[32 + 4 + 4]; // extra 4bytes for hwId
            System.Array.Copy(key, 0, buf, 4, 32 + 4);
            fixed (byte* pKey = buf)
                *(int*)(pKey) = hwId; // write hwId to key

            File.WriteAllBytes(Path.Combine(m_rootDB, fileName), buf);
        }

        private static void SavePassword(byte[] cryptoKeyPassword)
        {
            CryptoKeyPassword = cryptoKeyPassword;
            SaveKey("pw", cryptoKeyPassword);
            Setting.LastAskPassword = System.DateTime.Now;
        }

        private unsafe static byte[] LoadKey(string fileName)
        {
            if (File.Exists(fileName))
            {
                var key = File.ReadAllBytes(fileName);
                if (key.Length == 32 + 4 + 4)
                {
                    int hwId;
                    fixed (byte* pKey = key)
                        hwId = *(int*)(pKey); // read hwId from key

                    if (hwId == Util.HwUniqueId)
                    {
                        var temp = new byte[32 + 4]; // alloc original key size
                        System.Array.Copy(key, 4, temp, 0, 32 + 4);
                        return temp;
                    }
                }
            }
            return null; // invalid key size hwId
        }

        //public static void UploadOwnerStatus()
        //{
        //    var respBuf = new NSerializer(TypeHeader.NOTIFICATION);
        //    respBuf.Add((byte)Notification.USER_UPDATE_STATUS);
        //    respBuf.Add(Owner.Status);
        //    Network.Enqueue(respBuf);
        //}

        public static void UploadOwnerDP()
        {
            var respBuf = new NSerializer(TypeHeader.NOTIFICATION);
            respBuf.Add((byte)Notification.USER_UPDATE_DP);
            respBuf.Add(Owner.DP == null ? Util.EmptyBytes : File.ReadAllBytes(Owner.DP.Full));
            Network.Enqueue(respBuf);
        }
    }
}