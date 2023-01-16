using System;
using System.IO;

namespace ChatAPI
{
    public class Feed
    {
        public readonly Contact Contact;
        public readonly DateTime Time;
        public readonly Notification Type;
        public readonly string Status;
        public string DpFile;

        public Feed(Contact contact, DateTime time, Notification type, string status)
        {
            this.Contact = contact;
            this.Time = time;
            this.Type = type;
            this.Status = status;
        }

        public void SetDP(byte[] imageData)
        {
            if (imageData != null && imageData.Length > 0)
            {
                DpFile = Path.Combine(Core.TempDir, Contact.ID + "_" + Time.Ticks.ToString());
                File.WriteAllBytes(DpFile, imageData);
            }
        }

        public static int CompareTimeReverse(Feed a, Feed b)
        {
            return -DateTime.Compare(a.Time, b.Time);
        }
    }
}
