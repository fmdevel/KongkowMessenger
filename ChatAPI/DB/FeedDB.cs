using System;
using System.IO;

namespace ChatAPI
{

    //public class OldFeedDB : DynamicBlockDB<Feed>
    //{
    //    public OldFeedDB(string fileName)
    //        : base(fileName)
    //    {
    //        base.LoadBlocks();
    //    }

    //    protected override byte[] TransformBlock(Feed value)
    //    {
    //        return null;
    //    }

    //    protected override Feed TransformBlock(byte[] block, int index, int count)
    //    {
    //        var u = new Deserializer(block, index, count);

    //        string id = null;
    //        if (!u.Extract(ref id))
    //            return null;

    //        var contact = Core.FindContact(id);
    //        if (contact == null)
    //            return null;

    //        DateTime time = default(DateTime);
    //        if (!u.Extract(ref time))
    //            return null;

    //        byte type = 0;
    //        if (!u.Extract(ref type) || !Enums<Notification>.IsDefined(type))
    //            return null;

    //        string status = null;
    //        byte[] dp = null;
    //        var notif = (Notification)type;
    //        if (notif == Notification.USER_UPDATE_STATUS)
    //        {
    //            if (!u.Extract(ref status))
    //                return null;
    //        }
    //        else if (notif == Notification.USER_UPDATE_DP)
    //            if (!u.Extract(ref dp))
    //                return null;

    //        var feed = new Feed(contact, time, notif, status);
    //        feed.SetDP(dp);
    //        return feed;
    //    }
    //}

    public class FeedDB : DynamicBlockDB<Feed>
    {
        public FeedDB(string fileName)
            : base(fileName)
        {
            base.LoadBlocks();
        }

        protected override byte[] TransformBlock(Feed value)
        {
            var s = new Serializer();
            s.Add(value.Contact.ID);
            s.Add(value.Time);
            s.Add((byte)value.Type);
            if (value.Type == Notification.USER_UPDATE_STATUS)
                s.Add(value.Status);
            else if (value.Type == Notification.USER_UPDATE_DP)
                s.Add(value.DpFile);

            return s.ToArray();
        }

        protected override Feed TransformBlock(byte[] block, int index, int count)
        {
            var u = new Deserializer(block, index, count);

            string id = null;
            if (!u.Extract(ref id))
                return null;

            var contact = Core.FindContact(id);
            if (contact == null)
            {
                if (id == Core.Owner.ID)
                    contact = Core.Owner;
                else
                    return null;
            }

            DateTime time = default(DateTime);
            if (!u.Extract(ref time))
                return null;

            byte type = 0;
            if (!u.Extract(ref type) || !Enums<Notification>.IsDefined(type))
                return null;

            string status = null;
            string dpFile = null;
            var notif = (Notification)type;
            if (notif == Notification.USER_UPDATE_STATUS)
            {
                if (!u.Extract(ref status))
                    return null;
            }
            else if (notif == Notification.USER_UPDATE_DP)
                if (!u.Extract(ref dpFile))
                    return null;

            var feed = new Feed(contact, time, notif, status);
            feed.DpFile = dpFile;
            return feed;
        }

        public override void RemoveAt(int index)
        {
            DeleteDpFile(index);
            base.RemoveAt(index);
        }

        private void DeleteDpFile(int index)
        {
            var dpFile = base[index].DpFile;
            if (!string.IsNullOrEmpty(dpFile) && File.Exists(dpFile))
                File.Delete(dpFile);
        }

        public override Feed this[int index]
        {
            get
            {
                return base[index];
            }
            set
            {
                DeleteDpFile(index);
                base[index] = value;
            }
        }

        public override void Clear()
        {
            int count = base.Count;
            if (count > 0)
            {
                do
                {
                    DeleteDpFile(--count);
                } while (count > 0);
                base.Clear();
            }
        }
    }
}
