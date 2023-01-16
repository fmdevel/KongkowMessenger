using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class PrivateChatActivity : SupportAttachActivity
    {
        private ConversationAdapter m_convAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_PrivateChat);
            ThemedResId = new[] { Resource.Id.ActivityHeader };

            var conversation = CurrentContact.GetConversation();
            conversation.OnChatArrive = ChatArrive;
            conversation.OnChatStatusChange = ChatStatusChange;

            m_convAdapter = new ConversationAdapter();
            m_convAdapter.Items = conversation.GetList();
            var v = FindViewById<ListView>(Resource.Id.ConversationViewer);
            v.Adapter = m_convAdapter;
            RegisterForContextMenu(v);

            FindViewById<ImageButton>(Resource.Id.btnSend).Click += SendChat;
            FindViewById<CircleImageView>(Resource.Id.ChatContactDP).Click += SeeProfile;
            FindViewById<TextView>(Resource.Id.ChatContactName).Click += SeeProfile;
            var tbChatMsg = FindViewById<EditText>(Resource.Id.tbChatMsg);
            tbChatMsg.TextChanged += tbChatMsg_TextChanged;
            var sz = Core.Setting.FontSize;
            if (sz >= 2) // XL or XXL
                tbChatMsg.SetTextSize(Android.Util.ComplexUnitType.Dip, sz * 4 + 12);

            SetAttachView(Resource.Id.btnAttach, false);

            string strUri = Core.Setting.ChatWallpaper;
            if (!string.IsNullOrEmpty(strUri))
            {
                try
                {
                    var w = FindViewById<ImageView>(Resource.Id.Wallpaper);
                    w.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(strUri)));
                    w.Visibility = ViewStates.Visible;
                }
                catch { }
            }
            else
                FindViewById(Resource.Id.ChatBody).SetBackgroundColor(Core.Setting.ColorWallpaper);
        }

        protected override void OnResume()
        {
            base.OnResume();
            CurrentContact.EnterConversation();
            RefreshHeader();
            LocalNotification.ClearChatArriveNotification();
        }

        protected override void OnPause()
        {
            ClearHandler();
            CurrentContact.LeaveConversation();
            base.OnPause();
        }

        private void SeeProfile(object sender, EventArgs e)
        {
            ViewProfileActivity.Start((View)sender);
        }

        private void SendChat(object sender, EventArgs e)
        {
            var tbMsg = FindViewById<EditText>(Resource.Id.tbChatMsg);
            var message = tbMsg.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            Conversation.CurrentConversation.SendChat(message);
            m_convAdapter.NotifyDataSetChanged();
            tbMsg.Text = null;
        }

        private void tbChatMsg_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            CurrentContact.GetConversation().Typing(((EditText)sender).Text);
        }

        private void ChatArrive(ChatMessage chat)
        {
            RunOnUiThread(() =>
            {
                StopTyping();
                m_convAdapter.NotifyDataSetChanged();
            });
            if (!chat.Contact.IsServerPOS)
                new ChatStatus(ChatAPI.Notification.CHAT_READ, chat.MessageId).TransmitTo(chat.Contact);

            if (chat.Attachment != null && !string.IsNullOrEmpty(chat.Attachment.FileName) && chat.Attachment.FileName.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase))
            {
                Util.PublishFileToGallery(chat.Attachment.FileName);
            }
        }

        private void ChatStatusChange(ChatMessage chat)
        {
            RunOnUiThread(() => m_convAdapter.NotifyDataSetChanged());
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v.Id == Resource.Id.attachment)
            {
                var handler = v.Tag as ConversationAdapter.AttachHandler;
                if (handler != null)
                    menu.Add(handler.MessageId, 2, 2, "Share");
            }
            else if (v.Id == Resource.Id.ConversationViewer)
            {
                var chat = Conversation.CurrentConversation.GetList()[((AdapterView.AdapterContextMenuInfo)menuInfo).Position];
                menu.Add(Menu.None, 0, 0, Resources.GetString(Resource.String.CopyMessage));
                menu.Add(Menu.None, 1, 1, Resources.GetString(Resource.String.ForwardMessage));
                if (chat.Direction == ChatMessage.TypeDirection.OUT && chat.Status != ChatAPI.Notification.CHAT_READ)
                    menu.Add(Menu.None, 3, 3, Resources.GetString(Resource.String.RetrackMessage));
                menu.Add(Menu.None, 4, 4, Resources.GetString(Resource.String.DeleteMessage));
                menu.Add(Menu.None, 5, 5, Resources.GetString(Resource.String.DeleteAllMessages));
            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            var conversation = Conversation.CurrentConversation;
            if (item.ItemId == 2)
            {
                Util.ShareToAnotherApp(conversation.FindChat(item.GroupId).Attachment.FileName);
                return true;
            }

            var chat = conversation.GetList()[((AdapterView.AdapterContextMenuInfo)item.MenuInfo).Position];
            if (item.ItemId == 0)
            {
                ((Android.Text.ClipboardManager)Application.Context.GetSystemService(ClipboardService)).Text = chat.MessageTrim;
            }
            else if (item.ItemId == 1)
            {
                SearchableContactActivity.Items = null; // Reset
                SearchableContactActivity.CreateItems();
                var intent = new Intent(string.Empty, null, this, typeof(SearchableContactActivity));
                intent.PutExtra("select_all_visible", false);
                intent.PutExtra("id", chat.MessageId);
                StartActivityForResult(intent, 55);
            }
            else if (item.ItemId == 3)
            {
                conversation.CancelChat(chat);
                m_convAdapter.NotifyDataSetChanged();
            }
            else if (item.ItemId == 4)
            {
                ShowPopup(null, Resources.GetString(Resource.String.DeleteMessage) + "?", Resources.GetString(Resource.String.Delete), () =>
                 {
                     conversation.RemoveChat(chat);
                     m_convAdapter.NotifyDataSetChanged();
                     Toast.MakeText(this, Resources.GetString(Resource.String.MessageDeleted), ToastLength.Short).Show();
                 }, Resources.GetString(Resource.String.Cancel), null);
            }
            else if (item.ItemId == 5)
            {
                conversation.Clear();
                m_convAdapter.NotifyDataSetChanged();
            }
            return true;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 55)
            {
                var items = SearchableContactActivity.Items;
                SearchableContactActivity.Items = null; // Clear static content
                if (data == null)
                    return;
                var messageId = data.GetIntExtra("id", 0);
                if (messageId == 0)
                    return;
                var chat = CurrentContact.GetConversation().FindChat(messageId);
                if (chat == null)
                    return;

                if (resultCode == Result.Ok)
                {
                    var list = new System.Collections.Generic.List<Contact>();
                    foreach (var item in items)
                        if (item.Selected)
                            list.Add(item.Contact);

                    chat.ForwardTo(list);
                }
            }
        }

        internal void OnUpdateInternal(Contact contact, ChatAPI.Notification typeUpdate)
        {
            if (contact == CurrentContact)
            {
                RunOnUiThread(() =>
                {
                    if (typeUpdate == ChatAPI.Notification.USER_TYPING)
                    {
                        FindViewById(Resource.Id.lbTyping).Visibility = ViewStates.Visible;
                        ClearHandler();
                        PostDelayed(StopTyping, 3000);
                    }
                    else if (typeUpdate == ChatAPI.Notification.USER_UPDATE_NAME)
                        FindViewById<TextView>(Resource.Id.ChatContactName).Text = contact.Name;
                    else if (typeUpdate == ChatAPI.Notification.USER_UPDATE_DP)
                    {
                        var dp = FindViewById<CircleImageView>(Resource.Id.ChatContactDP);
                        UIUtil.SetDefaultDP(dp, contact);
                        dp.BorderColor = Core.Setting.Themes;
                    }
                    else
                    {
                        FindViewById(Resource.Id.OnlineStatus).Visibility = contact.IsOnline ? ViewStates.Visible : ViewStates.Gone;
                        FindViewById<TextView>(Resource.Id.ChatContactStatus).Text = contact.ChatStatus;
                    }
                });
            }
        }

        private void RefreshHeader()
        {
            var contact = CurrentContact;
            FindViewById(Resource.Id.OnlineStatus).Visibility = contact.IsOnline ? ViewStates.Visible : ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.ChatContactName).Text = contact.Name;
            var dp = FindViewById<CircleImageView>(Resource.Id.ChatContactDP);
            UIUtil.SetDefaultDP(dp, contact);
            dp.BorderColor = Core.Setting.Themes;
            FindViewById<TextView>(Resource.Id.ChatContactStatus).Text = contact.ChatStatus;
        }

        private void StopTyping()
        {
            FindViewById(Resource.Id.lbTyping).Visibility = ViewStates.Gone;
        }

        protected override void OnAttach(byte[] raw, string fileType, string desc)
        {
            CurrentContact.GetConversation().SendChat(desc, new Attachment(fileType, raw));
            m_convAdapter.NotifyDataSetChanged();
        }
    }
}