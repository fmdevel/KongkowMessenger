using System;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public partial class MainActivity
    {
        private FeedAdapter m_feedAdapter;
        private void Initialize_Feed()
        {
            CreateAdapter_Feed();
            var v = m_tabView.GetContent(1).FindViewById<ListView>(Resource.Id.ListViewer);
            v.ItemClick += ContactClick_Feed;
            v.SetBackgroundColor(Android.Graphics.Color.WhiteSmoke);
        }

        private void CreateAdapter_Feed()
        {
            m_tabView.GetContent(1).FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_feedAdapter = new FeedAdapter();
        }
        internal void TrimMemory_Feed()
        {
            var content = m_tabView.GetContent(1);
            if (content != null && m_feedAdapter != null)
                content.FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_feedAdapter = null;
        }

        internal void Update_Feed()
        {
            m_tabIcons[1].State = TabState.Active;
            AnyUnprocessedFeed = false;
            m_tabIcons[0].State = AnyUnprocessedChat ? TabState.InactiveNotif : TabState.Inactive;
            m_tabIcons[2].State = TabState.Inactive;
#if BUILD_PARTNER
            m_tabIcons[3].State = AnyUnprocessedPosUpdate ? TabState.InactiveNotif : TabState.Inactive;
#endif
            RefreshList_Feed();
        }

        internal void Update_Feed(Contact contact)
        {
            var content = m_tabView.GetContent(1);
            var adapter = m_feedAdapter;
            if (content == null || adapter == null || adapter.Items.Count == 0)
                return;

            var list = content.FindViewById<ListView>(Resource.Id.ListViewer);
            int start = list.FirstVisiblePosition;
            for (int i = start, j = list.LastVisiblePosition; i <= j; i++)
                if (contact.ID == adapter.Items[i].Contact.ID)
                {
                    var view = list.GetChildAt(i - start);
                    adapter.GetView(i, view, list);
                    // break; // Do not break, because same Contact might appears multiple times
                }
        }

        internal void RefreshList_Feed()
        {
            if (m_feedAdapter == null)
                CreateAdapter_Feed();
            m_feedAdapter.Items = Core.GetSortedFeed();
            m_feedAdapter.NotifyDataSetChanged();
        }

        private void ContactClick_Feed(object sender, AdapterView.ItemClickEventArgs e)
        {
            var contact = m_feedAdapter.Items[e.Position].Contact;
            if (contact.ID == Core.Owner.ID)
            {
                CurrentContact = contact;
                ViewProfileActivity.Start((View)sender);
            }
            else
                StartChat(contact);
        }
    }
}