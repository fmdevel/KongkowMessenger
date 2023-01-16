using System;
using System.IO;

namespace ChatAPI
{
    public class LoginInfo
    {
        public LoginStatus Status;
    }

    public class BannedInfo : LoginInfo
    {
        public long Time;
        public string By;
        public string Reason;

        public string ToLocalTimeString()
        {
            return new DateTime(Time).ToLocalTime().ToString("d MMMM yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static partial class Core
    {
        internal static bool Login()
        {
            if (Owner == null || CryptoKey == null)
                return false;

            var respBuf = new NSerializer(TypeHeader.LOGIN);
            respBuf.Add(Owner.ID);
            respBuf.Add(Network.PROTOCOL_MAJOR_VERSION);
            respBuf.Add(Network.PROTOCOL_MINOR_VERSION);
            respBuf.SecureAdd(StringEncoder.UTF8.GetBytes(Owner.ID), CryptoKey);
            //*;
#if !(__IOS__ || __ANDROID__)
            respBuf.Add((int)2); // Desktop
#else
            respBuf.Add((int)0); // Mobile
#endif

#if BUILD_PARTNER
            respBuf.Add(Util.APP_FMUSER);
#else
            respBuf.Add(string.Empty);
#endif
            Network.Send(respBuf);
            return true;
        }

        public static void Logout()
        {
            Setting.ActivationState = ActivationState.NEED_ACTIVATION;
            ClearAppData();
        }

        internal static void SetLoginResult(LoginStatus status, NDeserializer buf)
        {
            if (status != LoginStatus.BLANK_PASSWORD && status != LoginStatus.SUCCESS && status != LoginStatus.SUCCESS_BUT_OBSOLETE && status != LoginStatus.FAIL_UNSUPPORTED_PROTOCOL)
                Setting.ActivationState = ActivationState.NEED_ACTIVATION;

            if (status == LoginStatus.FAIL_SESSION_EXPIRED)
                ClearAppData();

            LoginInfo info;
            if (status == LoginStatus.FAIL_ACCOUNT_HAS_BEEN_BANNED)
            {
                ClearAppData();
                var banned = new BannedInfo();
                info = banned;
                if (buf.Extract(ref banned.Time) && buf.Extract(ref banned.By))
                    buf.Extract(ref banned.Reason);
            }
            else
                info = new LoginInfo();

            info.Status = status;
            if (OnLogin != null)
                OnLogin.Invoke(info);
        }

        internal static void OnLoginSuccess()
        {
            ResendPendingChat();
            SyncAllContact();
        }

        internal static void ClearAppData()
        {
            Network.Shutdown();
            lock (PendingChat)
            {
                if (Owner != null)
                {
                    Util.DeleteFile(Path.Combine(m_rootDB, "owner"));
                    Util.DeleteFile(Path.Combine(m_rootDB, "crypto"));
                    Util.DeleteFile(Path.Combine(m_rootDB, "pw"));

                    try
                    {
                        BackupAndRemoveContacts(Owner.ID);
                    }
                    catch { }
                    finally
                    {
                        Owner = null; // reset owner
                        CryptoKey = null;
                        CryptoKeyPassword = null;
                        LoginToken = 0;
                    }
                }
            }
        }
    }
}