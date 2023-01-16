using System;
using Android.App;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public partial class MainActivity
    {
        private ContactAdapter m_contactAdapter;

        private void Initialize_Contact()
        {
            CreateAdapter_Contact();
            var v = m_tabView.GetContent(2).FindViewById<ListView>(Resource.Id.ListViewer);
            v.ItemClick += ContactClick_Contact;
            v.Divider = new Android.Graphics.Drawables.ColorDrawable(new Android.Graphics.Color(150, 150, 150));
            v.DividerHeight = 1;
            v.SetBackgroundColor(Android.Graphics.Color.White);
            RegisterForContextMenu(v);
        }

        private void CreateAdapter_Contact()
        {
            m_tabView.GetContent(2).FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_contactAdapter = new ContactAdapter();
        }
        internal void TrimMemory_Contact()
        {
            var content = m_tabView.GetContent(2);
            if (content != null && m_contactAdapter != null)
                content.FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_contactAdapter = null;
        }

        internal void Update_Contact()
        {
            m_tabIcons[2].State = TabState.Active;
            m_tabIcons[0].State = AnyUnprocessedChat ? TabState.InactiveNotif : TabState.Inactive;
            m_tabIcons[1].State = AnyUnprocessedFeed ? TabState.InactiveNotif : TabState.Inactive;
#if BUILD_PARTNER
            m_tabIcons[3].State = AnyUnprocessedPosUpdate ? TabState.InactiveNotif : TabState.Inactive;
#endif
            RefreshList_Contact();
        }

        internal void Update_Contact(Contact contact)
        {
            var content = m_tabView.GetContent(2);
            var adapter = m_contactAdapter;
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

        internal void RefreshList_Contact()
        {
            RefreshList_Contact(Core.GetSortedContacts());
        }

        internal void RefreshList_Contact(Contact[] contacts)
        {
            if (m_contactAdapter == null)
                CreateAdapter_Contact();
            m_contactAdapter.Items = contacts;
            m_contactAdapter.NotifyDataSetChanged();
        }

        public void OnCreateContextMenu_Contact(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v.Id == Resource.Id.ListViewer)
            {
                var contact = m_contactAdapter.Items[((AdapterView.AdapterContextMenuInfo)menuInfo).Position];
                menu.Add(Menu.None, 0, 0, GetString(Resource.String.ViewProfile) + " " + contact.Name);
                menu.Add(Menu.None, 1, 1, GetString(Resource.String.DeleteContact) + " " + contact.Name);
            }
        }

        public bool OnContextItemSelected_Contact(IMenuItem item)
        {
            var contact = m_contactAdapter.Items[((AdapterView.AdapterContextMenuInfo)item.MenuInfo).Position];
            if (item.ItemId == 0)
            {
                Activity.CurrentContact = contact;
                StartActivity(typeof(ViewProfileActivity));
            }
            else if (item.ItemId == 1)
            {
                Core.RemoveContact(contact);
                RefreshList_Contact();
            }
            return true;
        }

        private void ContactClick_Contact(object sender, AdapterView.ItemClickEventArgs e)
        {
            StartChat(m_contactAdapter.Items[e.Position]);
        }
    }
}