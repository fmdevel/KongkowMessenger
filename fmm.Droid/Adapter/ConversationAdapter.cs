using System;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public class ConversationAdapter : IListAdapter<ChatMessage>
    {
        private BaseQueue<View> m_viewIn = new BaseQueue<View>();
        private BaseQueue<View> m_viewInAttach = new BaseQueue<View>();
        private BaseQueue<View> m_viewOut = new BaseQueue<View>();
        private BaseQueue<View> m_viewOutAttach = new BaseQueue<View>();
        private BaseQueue<View> GetQueue(int layout)
        {
            switch (layout)
            {
                case Resource.Layout.Kongkow_Chat_IN:
                    return m_viewIn;
                case Resource.Layout.Kongkow_Chat_IN_Attachment:
                    return m_viewInAttach;
                case Resource.Layout.Kongkow_Chat_OUT:
                    return m_viewOut;
            }
            return m_viewOutAttach;
        }

        private View CreateView(int layout, ViewGroup parent)
        {
            View view = null;
            if (!GetQueue(layout).Dequeue(ref view))
                view = LayoutInflater.From(parent.Context).Inflate(layout, null);

            view.Tag = layout;
            var p = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            p.AddRule((layout == Resource.Layout.Kongkow_Chat_IN || layout == Resource.Layout.Kongkow_Chat_IN_Attachment) ? LayoutRules.AlignParentLeft : LayoutRules.AlignParentRight);
            var leftRight = UIUtil.DpToPx(10);
            var topBottom = UIUtil.DpToPx(5);
            p.SetMargins(leftRight, topBottom, leftRight, topBottom);
            parent.AddView(view, p);
            parent.Tag = view;
            return view;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var chat = this.Items[position];
            int layout;
            string attachFile = null;
            if (chat.Attachment != null && chat.Attachment.FileName.Length > 0 && System.IO.File.Exists(chat.Attachment.FileName))
            {
                layout = chat.Direction == ChatMessage.TypeDirection.IN ? Resource.Layout.Kongkow_Chat_IN_Attachment : Resource.Layout.Kongkow_Chat_OUT_Attachment;
                attachFile = chat.Attachment.FileName;
            }
            else
                layout = chat.Direction == ChatMessage.TypeDirection.IN ? Resource.Layout.Kongkow_Chat_IN : Resource.Layout.Kongkow_Chat_OUT;

            var view = convertView as ViewGroup;
            if (view == null)
                view = new RelativeLayout(parent.Context);

            var content = view.Tag as View;
            if (content == null)
            {
                content = CreateView(layout, view);
            }
            else if (layout != (int)content.Tag)
            {
                var imageView = content.FindViewById<ImageView>(Resource.Id.attachment);
                if (imageView != null)
                    imageView.SetImageDrawable(null); // Clear image content

                view.RemoveView(content);
                view.Tag = null;
                GetQueue((int)content.Tag).Enqueue(content); // Push to recycler
                content = CreateView(layout, view);
            }

            Update(chat, content, attachFile);
            return view;
        }

        private void HandleAttachment(ChatMessage chat, View view, string attachFile)
        {
            var imageView = view.FindViewById<ImageView>(Resource.Id.attachment);
            imageView.SetAdjustViewBounds(true);
            var handler = imageView.Tag as AttachHandler;
            if (handler == null)
                handler = new AttachHandler(imageView);

            handler.MessageId = chat.MessageId;
            handler.Uri = Android.Net.Uri.FromFile(new Java.IO.File(attachFile));
            handler.MimeType = Java.Net.URLConnection.GuessContentTypeFromName(attachFile);
            if ((object)handler.MimeType != null)
            {
                if (handler.MimeType.StartsWith("image"))
                    imageView.SetImageURI(handler.Uri);
                else if (handler.MimeType.StartsWith("audio"))
                    imageView.SetImageResource(Resource.Drawable.mime_music);
                else if (handler.MimeType.StartsWith("video"))
                    imageView.SetImageResource(Resource.Drawable.mime_video);
                else
                    imageView.SetImageResource(Resource.Drawable.mime_doc);
            }
            else
                imageView.SetImageResource(Resource.Drawable.mime_doc);
        }

        private void Update(ChatMessage chat, View view, string attachFile)
        {
            if ((object)attachFile != null)
                HandleAttachment(chat, view, attachFile);

            var sz = Core.Setting.FontSize;
            var fontSize = (float)(sz * 4 + 12);
            var lbChatMessage = view.FindViewById<TextView>(Resource.Id.lbChatMessage);
            lbChatMessage.Text = chat.MessageLocal;
            lbChatMessage.Visibility = string.IsNullOrEmpty(chat.Message) ? ViewStates.Gone : ViewStates.Visible;
            lbChatMessage.SetTextSize(Android.Util.ComplexUnitType.Dip, fontSize);
            TextView lbChatTime = view.FindViewById<TextView>(Resource.Id.lbChatTime);
            lbChatTime.Text = Util.LocalFormatDate(chat.Time);
            lbChatTime.SetTextSize(Android.Util.ComplexUnitType.Dip, fontSize * 4 / 5); // A bit smaller font

            if (chat.Direction == ChatMessage.TypeDirection.OUT)
            {
                ImageView status = view.FindViewById<ImageView>(Resource.Id.lbChatStatus);
                int resId;
                switch (chat.Status)
                {
                    case Notification.CHAT_SENT:
                        resId = Resource.Drawable.checklist;
                        break;
                    case Notification.CHAT_DELIVERED:
                        resId = Resource.Drawable.doublechecklist;
                        break;
                    case Notification.CHAT_READ:
                        resId = Resource.Drawable.doublechecklistblue;
                        break;
                    case Notification.CHAT_SEND_FAIL:
                        resId = Resource.Drawable.ic_delete;
                        break;
                    default:
                        resId = Resource.Drawable.hourglass;
                        break;
                }
                status.SetImageResource(resId);
            }
        }

        public class AttachHandler : Java.Lang.Object
        {
            public Android.Net.Uri Uri;
            public string MimeType;
            public int MessageId;

            public AttachHandler(View view)
            {
                view.Tag = this;
                view.Click += View_Click;
                Activity.CurrentActivity.RegisterForContextMenu(view);
            }

            private void View_Click(object sender, EventArgs e)
            {
                Util.OpenAssociatedFile(Uri, MimeType);
            }
        }
    }
}