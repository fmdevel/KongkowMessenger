using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public partial class MainActivity
    {
        private RecentChatAdapter m_chatAdapter;

        private void Initialize_RecentChat()
        {
            CreateAdapter_RecentChat();
            var v = m_tabView.GetContent(0).FindViewById<ListView>(Resource.Id.ListViewer);
            v.ItemClick += ContactClick_RecentChat;
            v.Divider = new Android.Graphics.Drawables.ColorDrawable(new Android.Graphics.Color(150, 150, 150));
            v.DividerHeight = 1;
            v.SetBackgroundColor(Android.Graphics.Color.White);
            RegisterForContextMenu(v);
            Update_RecentChat();
        }

        private void CreateAdapter_RecentChat()
        {
            m_tabView.GetContent(0).FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_chatAdapter = new RecentChatAdapter();
        }

        internal void TrimMemory_RecentChat()
        {
            var content = m_tabView.GetContent(0);
            if (content != null && m_chatAdapter != null)
                content.FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_chatAdapter = null;
        }

        internal void Update_RecentChat()
        {
            m_tabIcons[0].State = TabState.Active;
            AnyUnprocessedChat = false;
            m_tabIcons[1].State = AnyUnprocessedFeed ? TabState.InactiveNotif : TabState.Inactive;
            m_tabIcons[2].State = TabState.Inactive;
#if BUILD_PARTNER
            m_tabIcons[3].State = AnyUnprocessedPosUpdate ? TabState.InactiveNotif : TabState.Inactive;
#endif
            RefreshList_RecentChat();
        }

        internal void Update_RecentChat(Contact contact)
        {
            var content = m_tabView.GetContent(0);
            var adapter = m_chatAdapter;
            if (content == null || adapter == null || adapter.Items.Count == 0)
                return;

            var list = content.FindViewById<ListView>(Resource.Id.ListViewer);
            int start = list.FirstVisiblePosition;
            for (int i = start, j = list.LastVisiblePosition; i <= j; i++)
                if (contact.ID == adapter.Items[i].ID)
                {
                    var view = list.GetChildAt(i - start);
                    adapter.GetView(i, view, list);
                    break;
                }
        }

        internal void RefreshList_RecentChat()
        {
            var sortedChats = Core.GetSortedRecentChats();
            var sortedContacts = Core.GetContactSupport();
            foreach(var c in sortedChats)
            {
                if (!sortedContacts.Contains(c.Contact))
                    sortedContacts.Add(c.Contact);
            }

            if (m_chatAdapter == null)
                CreateAdapter_RecentChat();
            m_chatAdapter.Items = sortedContacts;
            m_chatAdapter.NotifyDataSetChanged();
        }

        public void OnCreateContextMenu_RecentChat(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v.Id == Resource.Id.ListViewer)
            {
                var contact = m_chatAdapter.Items[((AdapterView.AdapterContextMenuInfo)menuInfo).Position];
                menu.Add(Menu.None, 0, 0, Resources.GetString(Resource.String.DeleteAllMessages) + " " + contact.Name);
                menu.Add(Menu.None, 1, 1, Resources.GetString(Resource.String.ViewProfile) + " " + contact.Name);
            }
        }

        public bool OnContextItemSelected_RecentChat(IMenuItem item)
        {
            var contact = m_chatAdapter.Items[((AdapterView.AdapterContextMenuInfo)item.MenuInfo).Position];
            if (item.ItemId == 0)
            {
                contact.GetConversation().Clear();
                RefreshList_RecentChat();
            }
            else if (item.ItemId == 1)
            {
                Activity.CurrentContact = contact;
                StartActivity(typeof(ViewProfileActivity));
            }
            return true;
        }

        private void ContactClick_RecentChat(object sender, AdapterView.ItemClickEventArgs e)
        {
            StartChat(m_chatAdapter.Items[e.Position]);
        }
    }
}