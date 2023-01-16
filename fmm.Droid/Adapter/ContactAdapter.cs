using System;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public class ContactAdapter : IListAdapter<Contact>
    {
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.Kongkow_ListItemContact, null);

            var contact = this.Items[position];
            convertView.FindViewById(Resource.Id.OnlineStatus).Visibility = contact.IsOnline ? ViewStates.Visible : ViewStates.Gone;
            convertView.FindViewById<TextView>(Resource.Id.ContactName).Text = contact.PublicName;
            var r = UIUtil.GetBadgeResId(contact.AccType);
            var badge = convertView.FindViewById<ImageView>(Resource.Id.badge);
            if (r == -1) badge.SetImageDrawable(null); else badge.SetImageResource(r);

            convertView.FindViewById<TextView>(Resource.Id.ContactStatus).Text = contact.Status;

            var dp = convertView.FindViewById<CircleImageView>(Resource.Id.ContactDP);
            UIUtil.SetDefaultDP(dp, contact);
            dp.BorderColor = Android.Graphics.Color.White;
            dp.Tag = position;
            dp.Click -= Dp_Click;
            dp.Click += Dp_Click;

            return convertView;
        }

        private void Dp_Click(object sender, EventArgs e)
        {
            var view = (View)sender;
            Activity.CurrentContact = this.Items[(int)view.Tag];
            ViewProfileActivity.Start(view);
        }
    }
}