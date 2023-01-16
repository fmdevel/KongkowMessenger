using System.IO;

namespace ChatAPI
{
    public static partial class Core
    {
        private static FeedDB m_feed;

        private static FeedDB GetFeedDB()
        {
            if (m_feed == null)
                m_feed = new FeedDB(Path.Combine(m_rootDB, "feeds.fdt"));

            return m_feed;
        }
        private static FeedDB GetFeed(out Feed oldestItem)
        {
            var feed = GetFeedDB();
            oldestItem = null;
            var count = feed.Count;
            while (--count >= 0)
            {
                var item = feed[count];
                if (item == null || (item.Contact.ID != Owner.ID && m_allContactsId.IndexOf(item.Contact.ID) < 0))
                    feed.RemoveAt(count); // remove non-existance contact
                else if (oldestItem == null || item.Time < oldestItem.Time)
                    oldestItem = item;
            }

            return feed;
        }

        public static void UpdateFeed(Feed item)
        {
            Feed oldestItem;
            var feed = GetFeed(out oldestItem);
            if (feed.Count >= 50 && oldestItem != null)
                feed[feed.IndexOf(oldestItem)] = item;
            else
                feed.Add(item);

            if (OnFeed != null)
                OnFeed.Invoke(item);
        }

        public static Feed[] GetSortedFeed()
        {
            Feed oldestItem;
            var sorted = GetFeed(out oldestItem).GetList().ToArray();
            System.Array.Sort(sorted, Feed.CompareTimeReverse);
            return sorted;
        }
    }
}