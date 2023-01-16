using System;

namespace ChatAPI
{
    public static class Enums<E> // UltraFast version of Enum.IsDefined
    {
        private static byte[] m_hash = (byte[])Enum.GetValues(typeof(E));
        public static bool IsDefined(byte value)
        {
            return Array.IndexOf(m_hash, value) >= 0;
        }
    }

    public enum TypeHeader : byte
    {
        INVALID = 0,
        ACTIVATION = 3,
        LOGIN = 4,
        PING = 7,
        NOTIFICATION = 8,
        LAST_ACTIVITY = 10,
        CHAT = 11,
        BROADCAST = 12,
        CONTACT_SYNC = 14,
        CONTACT_SYNC_DEVICE = 17,
        CONTACT_DELETE = 19,
        LAST_SEEN = 22,
        POS = 230,
        BANNER_SYNC = 234,
        UPDATE_FMI = 235, // DESKTOP
        // STREAM = 254,
        // EXTENDED_TYPE = 255
    }

    public enum ActivationType : byte
    {
        CheckUsernameAvailability = 19,
        SignUp = 20,
        UseExistingAccount = 21,
        Ping = 22,
        SetPassword = 23,
        Recovery = 24,
        GetSecurityQuestion = 25,
        AnswerSecurityQuestion = 26,
        UpdateSecurityQuestion = 27
    }

    public enum LoginStatus : byte
    {
        SUCCESS = 0,
        FAIL_SESSION_EXPIRED = 1,
        SUCCESS_BUT_OBSOLETE = 3,
        FAIL_UNSUPPORTED_PROTOCOL = 4,
        BLANK_PASSWORD = 5,
        FAIL_ACCOUNT_HAS_BEEN_BANNED = 254,
    }

    public enum ActivationState : byte
    {
        NEED_ACTIVATION = 0,
        ACTIVATED = 1,
        ANSWER_SECURITY_QUESTION = 2,
        NEED_SET_PASSWORD = 3,
        USER_SIGNUP = 4
    }

    public enum Notification : byte
    {
        USER_UPDATE_STATUS = 3,
        USER_UPDATE_DP = 4,
        USER_LIKE = 5, // User give like to DP or Status
        USER_UPDATE_EXTRAINFO = 6,
        USER_TYPING = 9,
        USER_ONLINESTATE = 8,
        USER_LAST_SEEN = 10,

        CHAT_PENDING = 11,
        CHAT_SENT = 12,
        CHAT_DELIVERED = 13,
        CHAT_READ = 14,
        CHAT_CANCEL = 15,
        CHAT_SEND_FAIL = 16,

        //CALL_VOICE = 31,
        //CALL_VIDEO = 32,
        //CALL_ACCEPT = 33,
        //CALL_END = 34,
        //CALL_BUSY = 35,
        //CALL_MISSED = 36 // Missed call
        USER_UPDATE_PINTRX = 249,
        USER_UPDATE_BANNER = 250,
        USER_UPDATE_NAME = 251,
        USER_UPDATE_USERNAME = 252
    }

    public enum Language : byte
    {
        ID = 0,
        EN = 1
    }

    public enum AccountType : uint
    {
        Regular = 0,
        ServerPOS = 1,
        Banned = 4,
        Verified_Account = 16,
        VIP = 17,
        Verified_Partner = 18,
        Billing = 32,
        Customer_Service = 33,
        Volunteer = 34,
        Global_Moderator = 48,
        Verified_Seller = 49,
        Verified_Local_Business = 50,
        Moderator = 64,
        Verified_Buyer = 65,
        Official_Store = 80,
        Verified_Business = 81,
        Staff = 96,
        Developer = 97,
        Community_Manager = 112,
        FMSupport = 113,
        SysAdmin = uint.MaxValue
    }

    //public enum StreamType
    //{
    //    INVALID = 0,
    //    FILE = 17,
    //    AUDIO = 18,
    //    VIDEO = 19
    //}

    //public enum FontSize : byte
    //{
    //    SMALL = 0,
    //    MEDIUM = 1,
    //    LARGE = 2,
    //    EXTRA_LARGE = 3
    //}
}