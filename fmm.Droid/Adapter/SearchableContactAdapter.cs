using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public class SearchableContactAdapter : IListAdapter<SelectableContact>
    {
        private List<SelectableContact> m_items;

        public SearchableContactAdapter(List<SelectableContact> list)
        {
            list.Sort(SelectableContact.Comparer);
            m_items = list;
            base.Items = list;
        }

        public void Search(string key)
        {
            if ((object)key == null || key.Length < 2)
            {
                m_items.Sort(SelectableContact.Comparer);
                base.Items = m_items;
            }
            else
            {
                key = Util.FullPhoneNumber(key);
                var list = new List<SelectableContact>();
                foreach (var item in m_items)
                {
                    if (item.Contact.ID.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Contact.Username.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0 || item.Contact.Name.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        list.Add(item);
                }
                list.Sort(SelectableContact.Comparer);
                base.Items = list;
            }
            this.NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.Kongkow_ListItemContact, null);
                convertView.Click += Item_Click;
            }

            var item = this.Items[position];
            convertView.Tag = item;

            var contact = item.Contact;
            convertView.FindViewById(Resource.Id.OnlineStatus).Visibility = contact.IsOnline ? ViewStates.Visible : ViewStates.Gone;
            convertView.FindViewById<TextView>(Resource.Id.ContactStatus).Visibility = ViewStates.Invisible;
            var cBox = convertView.FindViewById<CheckBox>(Resource.Id.cBoxContact);
            cBox.Visibility = ViewStates.Visible;
            cBox.Checked = item.Selected;
            convertView.FindViewById<TextView>(Resource.Id.ContactName).Text = contact.PublicName;
            var r = UIUtil.GetBadgeResId(contact.AccType);
            var badge = convertView.FindViewById<ImageView>(Resource.Id.badge);
            if (r == -1) badge.SetImageDrawable(null); else badge.SetImageResource(r);

            var dp = convertView.FindViewById<CircleImageView>(Resource.Id.ContactDP);
            UIUtil.SetDefaultDP(dp, contact);
            dp.BorderColor = new Android.Graphics.Color(0xF0, 0xF0, 0xF0);

            return convertView;
        }

        private void Item_Click(object sender, EventArgs e)
        {
            var view = (View)sender;
            var item = (SelectableContact)view.Tag;
            item.Selected = !item.Selected;
            view.FindViewById<CheckBox>(Resource.Id.cBoxContact).Checked = item.Selected;
        }
    }

    public class SelectableContact : Java.Lang.Object
    {
        public Contact Contact;
        public bool Selected;

        public SelectableContact(Contact contact)
        {
            this.Contact = contact;
        }

        public static int Comparer(SelectableContact a, SelectableContact b)
        {
            if (a.Selected)
            {
                if (!b.Selected) return -1;
            }
            else if (b.Selected)
                return 1;

            return Contact.Comparer(a.Contact, b.Contact);
        }
    }
}