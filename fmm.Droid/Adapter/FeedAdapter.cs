using System;
using Android.App;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public class FeedAdapter : IListAdapter<Feed>
    {
        private BaseQueue<ImageView> m_viewCache = new BaseQueue<ImageView>();

        private ImageView CreateView(ViewGroup parent)
        {
            var context = parent.Context;
            ImageView view = null;
            if (!m_viewCache.Dequeue(ref view))
                view = new ImageView(context);

            int w = (context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? UIUtil.DisplayMetrics.WidthPixelsLandscape : UIUtil.DisplayMetrics.WidthPixelsPotrait) - UIUtil.DpToPx(22);
            parent.AddView(view, w, w);
            parent.Tag = view;
            return view;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var feed = this.Items[position];
            var view = convertView;
            if (view == null)
                view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.Kongkow_ListItemFeed, null);

            view.FindViewById(Resource.Id.OnlineStatus).Visibility = feed.Contact.IsOnline ? ViewStates.Visible : ViewStates.Gone;
            var dp = view.FindViewById<CircleImageView>(Resource.Id.ContactDP);
            UIUtil.SetDefaultDP(dp, feed.Contact);
            dp.BorderColor = Android.Graphics.Color.White;
            dp.Tag = position;
            dp.Click -= Dp_Click;
            dp.Click += Dp_Click;

            view.FindViewById<TextView>(Resource.Id.ContactName).Text = (feed.Contact.ID == Core.Owner.ID) ? (Core.Setting.Language == Language.EN ? "You" : "Anda") : feed.Contact.PublicName;
            var r = UIUtil.GetBadgeResId(feed.Contact.AccType);
            var badge = view.FindViewById<ImageView>(Resource.Id.badge);
            if (r == -1) badge.SetImageDrawable(null); else badge.SetImageResource(r);

            view.FindViewById<TextView>(Resource.Id.ContactUpdateTime).Text = Util.LocalFormatDate(feed.Time);
            string updateKind = null;

            var content = view.FindViewById<LinearLayout>(Resource.Id.ContactUpdateContent);
            var contentImage = content.Tag as ImageView;
            if (!string.IsNullOrEmpty(feed.DpFile) && System.IO.File.Exists(feed.DpFile))
            {
                if (contentImage == null)
                    contentImage = CreateView(content);

                contentImage.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(feed.DpFile)));
                updateKind = parent.Context.Resources.GetString(Resource.String.UpdatePhotoProfile);
            }
            else if (contentImage != null)
            {
                contentImage.SetImageDrawable(null); // Clear image content
                content.RemoveView(contentImage);
                content.Tag = null;
                m_viewCache.Enqueue(contentImage); // Push to recycler
            }

            var contactStatus = view.FindViewById<TextView>(Resource.Id.ContactUpdateStatus);
            if (!string.IsNullOrEmpty(feed.Status))
            {
                contactStatus.Text = feed.Status;
                contactStatus.Visibility = ViewStates.Visible;
                updateKind = "Update Status";
            }
            else
            {
                contactStatus.Text = null;
                contactStatus.Visibility = ViewStates.Gone;
            }

            view.FindViewById<TextView>(Resource.Id.ContactUpdate).Text = updateKind;
            return view;
        }

        private void Dp_Click(object sender, EventArgs e)
        {
            var view = (View)sender;
            Activity.CurrentContact = this.Items[(int)view.Tag].Contact;
            ViewProfileActivity.Start(view);
        }
    }
}