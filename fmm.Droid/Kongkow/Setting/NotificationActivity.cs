using System;

using Android.App;
using Android.OS;
using Android.Widget;
using Android.Media;
using ChatAPI;

namespace fmm
{
    [Activity()]
    public class NotificationActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_Notification);
            ThemedResId = new[] { Resource.Id.ActivityHeader };

            FindViewById<Button>(Resource.Id.btnDoneNotif).Click += btnDoneNotif_Click;
            FindViewById<Button>(Resource.Id.btnCancelNotif).Click += btnCancelNotif_Click;
            var radioGroupNotif = FindViewById<RadioGroup>(Resource.Id.rGroupNotif);
            var strUri = Core.Setting.Read("NotificationURI");
            var manager = new RingtoneManager(this);
            manager.SetType(RingtoneType.Notification);
            var cursor = manager.Cursor;
            if (cursor.Count > 0 && cursor.MoveToFirst())
            {
                do
                {
                    var rb = new RadioButton(this);
                    rb.Text = cursor.GetString(cursor.GetColumnIndex("title"));
                    var uri = manager.GetRingtoneUri(cursor.Position);
                    rb.Tag = uri;
                    rb.Click += rb_Click;
                    radioGroupNotif.AddView(rb);
                    if (strUri == uri.ToString())
                        rb.Checked = true;
                } while (cursor.MoveToNext());
            }
            cursor.Close();
        }

        private void rb_Click(object sender, EventArgs e)
        {
            var rb = (RadioButton)sender;
            FindViewById<RadioGroup>(Resource.Id.rGroupNotif).Tag = rb;
            RingtoneManager.GetRingtone(this, (Android.Net.Uri)rb.Tag).Play();
        }

        private void btnDoneNotif_Click(object sender, EventArgs e)
        {
            var rb = (RadioButton)FindViewById<RadioGroup>(Resource.Id.rGroupNotif).Tag;
            if (rb != null)
            {
                Core.Setting.Write("NotificationURI", ((Android.Net.Uri)rb.Tag).ToString());
                Core.Setting.Write("NotificationName", rb.Text);
                Finish();
            }
        }

        private void btnCancelNotif_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }
}