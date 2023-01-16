using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Graphics;

using ChatAPI;
#if BUILD_PARTNER
using ChatAPI.Connector;
#endif

namespace fmm
{
    public static class LocalNotification
    {
        public static NotificationManager Manager
        {
            get { return Application.Context.GetSystemService(Context.NotificationService) as NotificationManager; }
        }

        private static int m_notifyId;
        private static Bitmap m_cacheDefaultIcon; // Cached for boost performance
        private static List<Bitmap> m_cacheIcons;

        private static Android.Net.Uri RingtoneUri
        {
            get
            {
                var uri = Core.Setting.Read("NotificationURI");
                return string.IsNullOrEmpty(uri) ? RingtoneManager.GetDefaultUri(RingtoneType.Notification) : Android.Net.Uri.Parse(uri);
            }
        }

        private static Bitmap LargeIcon()
        {
            var bitmap = m_cacheDefaultIcon;
            if (bitmap == null)
            {
                var originalBitmap = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.app_icon);
                if (UIUtil.DisplayMetrics.Density >= 4.0f)
                {
                    bitmap = originalBitmap; // Give original size bitmap for xxxhdpi (~640 dpi) or higher
                }
                else
                {
                    // Since Kongkow app icon size is 192x192 pixels, it's better to reduce icon size to fit screen Dpi
                    // Another side effect is memory usage gets reduced for screen lower than xxxhdpi (192x192px 4,0x)
                    // See https://stackoverflow.com/questions/25030710/gcm-push-notification-large-icon-size/25030938
                    var sz = (int)(192.0f * (UIUtil.DisplayMetrics.Density / 4.0f)); // 192 is Kongkow appdefault.png size
                    bitmap = UIUtil.ResizeImage(originalBitmap, sz, sz);
                    originalBitmap.Recycle(); // We got reduced imaged, no need originalBitmap
                }
                m_cacheDefaultIcon = bitmap;
            }
            return bitmap;
        }

        private static Intent CreateIntent(Type type)
        {
            var intent = new Intent(Application.Context, type);
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            return intent;
        }

        private static object m_channel;
        private static string m_channelId;
        private static void DoNotify(int id, Intent intent, string title, string content, Bitmap icon)
        {
            try
            {
                intent.PutExtra("NotifID", id);
                var context = Application.Context;
                var sdkInt = Build.VERSION.SdkInt;
                var pending = PendingIntent.GetActivity(context, 0, intent, sdkInt >= (BuildVersionCodes)31 ? PendingIntentFlags.Immutable | PendingIntentFlags.OneShot : PendingIntentFlags.OneShot);
                var mgr = Manager;

                if (sdkInt >= BuildVersionCodes.O)
                {
                    var chan = m_channel as NotificationChannel;
                    if (chan == null)
                    {
                        m_channelId = "fmm";
                        m_channel = chan = new NotificationChannel(m_channelId, new Java.Lang.String(m_channelId), NotificationImportance.Low);
                        chan.SetSound(null, null);
                    }
                    mgr.CreateNotificationChannel(chan);
                }

                if (icon == null) icon = LargeIcon();
                var builder = new Android.App.Notification.Builder(context)
                    .SetAutoCancel(true)
                    .SetContentIntent(pending)
                    .SetContentTitle(title)
                    .SetContentText(content)
                    .SetSound(null)
                    .SetLargeIcon(icon)
                    .SetSmallIcon(Resource.Drawable.notif_icon_small);

                if (sdkInt >= BuildVersionCodes.O)
                    builder.SetChannelId(m_channelId);

                var notif = builder.Build();
                notif.Priority = -1; //NotificationPriority.Low;

                RingtoneManager.GetRingtone(context, RingtoneUri).Play();

                notif.Defaults = Core.Setting.Vibrate ? NotificationDefaults.Lights | NotificationDefaults.Vibrate : NotificationDefaults.Lights;
                mgr.Notify(id, notif);
            }
            catch { }
        }

        public static void NotifyChatArrive(ChatMessage chat)
        {
            // NotifID = 8, 9, 10
            // Only 3 chat notifications displayed at max

            var intent = CreateIntent(typeof(MainActivity));
            intent.PutExtra("Contact", chat.Contact.ID);

            Bitmap icon = null;
            if (chat.Contact.DP != null)
            {
                var uri = Android.Net.Uri.FromFile(new Java.IO.File(chat.Contact.DP.Thumbnail));
                int maxSize = UIUtil.DisplayMetrics.Density >= 4.0f ? 192 : (int)(192.0f * (UIUtil.DisplayMetrics.Density / 4.0f)); // reduce icon size to fit screen Dpi
                icon = UIUtil.GetBitmap(Application.Context, uri, maxSize);
            }

            DoNotify(8 + (m_notifyId++ % 3), intent, Core.UnreadChatCount.ToString() + (Core.Setting.Language == Language.ID ? " Pesan baru diterima" : " New message(s)"), chat.Contact.Name + ": " + chat.Message, icon);

            if (icon != null)
            {
                if (m_cacheIcons == null)
                {
                    m_cacheIcons = new List<Bitmap>(3);
                }
                else if (m_cacheIcons.Count >= 3)
                {
                    m_cacheIcons[0].Recycle();
                    m_cacheIcons.RemoveAt(0);
                }
                m_cacheIcons.Add(icon);
            }
        }

#if BUILD_PARTNER
        public static void NotifyPosInfo(ContactPOS contact, string info)
        {
            if (Activity.CurrentActivity != null)
                Activity.CurrentActivity.RunOnUiThread(() => Activity.CurrentActivity.PopupOK(contact.Name, info));
            else
            {
                var intent = CreateIntent(typeof(POS_History));
                //intent.PutExtra("Contact", contact.ID);
                intent.PutExtra("Info", info);
                DoNotify(11 + m_notifyId++, intent, contact.Name, info, null);
            }
        }

        public static void NotifyPosStruk(ContactPOS contact, TrxData trx)
        {
            if (Activity.CurrentActivity != null)
                Activity.CurrentActivity.RunOnUiThread(() => POS_Struk.Show(trx.TrxId));
            else
            {
                var intent = CreateIntent(typeof(POS_History));
                //intent.PutExtra("Contact", contact.ID);
                intent.PutExtra("trxId", trx.TrxId);
                DoNotify(11 + m_notifyId++, intent, contact.Name, trx.Description, null);
            }
        }
#endif

        public static void ClearChatArriveNotification()
        {
            ClearNotification(8);
            ClearNotification(9);
            ClearNotification(10);
            if (m_cacheIcons != null)
            {
                foreach (Bitmap icon in m_cacheIcons)
                {
                    icon.Recycle();
                }
                m_cacheIcons = null;
            }
        }

        public static void ClearNotification(int id)
        {
            try
            {
                Manager.Cancel(id);
            }
            catch { }
        }
    }
}