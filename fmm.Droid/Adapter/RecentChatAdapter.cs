using System;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using ChatAPI;

namespace fmm
{
    public class RecentChatAdapter : IListAdapter<Contact>
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

            var dp = convertView.FindViewById<CircleImageView>(Resource.Id.ContactDP);
            UIUtil.SetDefaultDP(dp, contact);
            dp.BorderColor = Color.White;
            dp.Tag = position;
            dp.Click -= Dp_Click;
            dp.Click += Dp_Click;

            var chat = Core.GetRecentChat(contact);
            if (chat != null)
            {
                var status = convertView.FindViewById<TextView>(Resource.Id.ContactStatus);
                status.Typeface = (chat.Direction == ChatMessage.TypeDirection.IN && chat.Status != Notification.CHAT_READ) ? Typeface.DefaultBold : Typeface.Default;
                var message = chat.MessageLocal;
                if (!string.IsNullOrEmpty(message))
                    message += " | ";
                else if (chat.Attachment != null && !string.IsNullOrEmpty(chat.Attachment.FileName))
                    message = "Attachment | ";
                status.Text = message + Util.LocalFormatDate(chat.Time);
            }
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