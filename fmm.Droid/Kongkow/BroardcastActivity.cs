using System;
using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

using ChatAPI;

namespace fmm
{
    [Activity]
    public class BroardcastActivity : SupportAttachActivity
    {
        private SimpleSelectedContactAdapter m_selectedContactAdapter;
        private byte[] m_attachment;
        private string m_type;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_Broadcast);
            ThemedResId = new[] { Resource.Id.ActivityHeader };

            m_selectedContactAdapter = new SimpleSelectedContactAdapter();
            FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_selectedContactAdapter;
            FindViewById<ImageButton>(Resource.Id.btnSend).Click += SendBroadcast;
            FindViewById<ImageView>(Resource.Id.AddContact).Click += AddContact;
            SetAttachView(Resource.Id.btnAttach, true);
            if (savedInstanceState != null)
            {
                m_attachment = savedInstanceState.GetByteArray("attachment");
                if (m_attachment != null)
                    ShowAttachment();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            if (m_attachment != null)
                outState.PutByteArray("attachment", m_attachment);
            base.OnSaveInstanceState(outState);
        }

        private void AddContact(object sender, EventArgs e)
        {
            SearchableContactActivity.CreateItems();
            StartActivityForResult(typeof(SearchableContactActivity), 55); // DO NOT USE StartActivity, use StartActivityForResult
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (SearchableContactActivity.Items == null)
                return;

            var list = new List<SelectableContact>();
            foreach (var item in SearchableContactActivity.Items)
                if (item.Selected)
                    list.Add(item);

            m_selectedContactAdapter.Items = list;
            m_selectedContactAdapter.NotifyDataSetChanged();
        }

        public override void OnBackPressed()
        {
            SearchableContactActivity.Items = null; // Clear static content
            base.OnBackPressed();
        }

        private void SendBroadcast(object sender, EventArgs e)
        {
            var message = FindViewById<EditText>(Resource.Id.tbChatMsg).Text.Trim();
            if (message.Length > 0 && m_selectedContactAdapter.Items.Count > 0)
            {
                var list = new List<Contact>(m_selectedContactAdapter.Items.Count);
                foreach (var item in m_selectedContactAdapter.Items)
                    list.Add(item.Contact);

                Core.Broadcast(message, m_attachment == null ? null : new Attachment(m_type, m_attachment), list);
                SearchableContactActivity.Items = null; // Clear static content
                Finish();
            }
        }

        private void ShowAttachment()
        {
            var att = FindViewById(Resource.Id.attachment);
            var del = att.FindViewById(Resource.Id.Delete);
            del.Click -= Del_Click;
            del.Click += Del_Click;
            att.FindViewById<TextView>(Resource.Id.Text).Text = "Attachment " + m_attachment.Length.ToString() + " bytes";
            att.Visibility = ViewStates.Visible;
        }

        protected override void OnAttach(byte[] raw, string fileType, string desc)
        {          
            m_attachment = raw;
            m_type = fileType;
            ShowAttachment();
        }

        private void Del_Click(object sender, EventArgs e)
        {
            m_attachment = null;
            ((View)((View)sender).Parent).Visibility = ViewStates.Gone;
        }
    }
}