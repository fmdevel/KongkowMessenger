using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class POS_Info : POS_Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize(Resource.Layout.POS_Info, "Informasi");
            FindViewById(Resource.Id.Send).Click += BtnProcess_Click;
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            if (ContactPOS.RegistrationNeeded)
                StartActivity(typeof(POS_Register));
            else
                StartActivityForResult(typeof(POS_SettingPanel_TicketDeposit), 20);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;

            if (requestCode == 20)
            {
                ContactPOS.Current.ShowProgress(null, 18000, "Hasil tidak diketahui");
                ContactPOS.Current.RequestTicket(data.GetStringExtra("value"), data.GetStringExtra("pin"));
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            ContactPOS.Current.GetAgenInfo(false);
            SetInfo();
        }

        internal void SetInfo()
        {
            SetServerInfo();
            SetAgenInfo();
        }

        private void SetServerInfo()
        {
            FindViewById<TextView>(Resource.Id.lbServerName).Text = ContactPOS.Current.Name;
            UIUtil.SetDefaultDP(FindViewById<ImageView>(Resource.Id.pbServerLogo), ContactPOS.Current);
            var extraInfo = Util.CommonSplit(ContactPOS.Current.ExtraInfo);
            string kodePos = null;
            if (extraInfo.Length >= 2)
            {
                kodePos = extraInfo[1];
                if (kodePos.Length > 0)
                    kodePos = "Kode Pos: " + kodePos;
            }

            string addr = null;
            if (extraInfo.Length >= 1)
                addr = extraInfo[0].Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ");

            FindViewById<TextView>(Resource.Id.lbServerAddress).Text = addr + " " + kodePos;

            if (extraInfo.Length >= 3)
            {
                CreateContactCS(FindViewById<LinearLayout>(Resource.Id.tbTlpCS), ParsePhone(extraInfo[2]), null, false);
            }

            if (extraInfo.Length >= 4)
            {
                CreateContactCS(FindViewById<LinearLayout>(Resource.Id.tbKongkowCS), ParsePhone(extraInfo[3]), AddContactCS, true);
            }
        }

        private static string[] ParsePhone(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var chars = new char[value.Length];
            int i = 0;
            foreach (char c in value)
            {
                if ((c >= '0' && c <= '9') || c == ',' || c == '/' || c == '&')
                    chars[i++] = c == '/' || c == '&' ? ',' : c;
            }
            if (i == 0)
                return null;
            return new string(chars, 0, i).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void SetAgenInfo()
        {
            var a = ContactPOS.Current.AgenInfo;
            if (a == null)
                return;

            //FindViewById<TextView>(Resource.Id.lbAgentCode).Text = a.Kode;
            FindViewById<TextView>(Resource.Id.lbName).Text = a.Nama;
            //var addr = a.Alamat;
            //if ((object)addr != null)
            //    addr = addr.Replace(",", ", ");

            //FindViewById<EditText>(Resource.Id.tbAddress).Text = addr;
            //FindViewById<EditText>(Resource.Id.tbID).Text = a.ID;
            FindViewById<TextView>(Resource.Id.lbSaldo).Text = Util.Rp(a.Saldo);
            FindViewById<TextView>(Resource.Id.lbStatus).Text = (a.Aktif ? "Aktif" : "Non Aktif");

            var m = a.Saldo.ToString();
            // Hack recent chat to get high position order in chat list
            m = "Saldo=" + m + " " + ContactPOS.Current.Status;
            var hackRecentChat = ContactPOS.Current.CacheRecentStatus;
            if (hackRecentChat == null)
            {
                ContactPOS.Current.CacheRecentStatus = hackRecentChat = new ChatMessage(ContactPOS.Current, ChatMessage.TypeChat.PRIVATE_CHAT, m);
                Core.UpdateRecentChat(hackRecentChat);
            }
            else if (hackRecentChat.Message != m)
            {
                //hackRecentChat.Message = m;
                //hackRecentChat.Time = DateTime.Now;
                Core.UpdateRecentChat(hackRecentChat);
            }

            var msg = a.AdditionalMessage;
            if (!string.IsNullOrEmpty(msg))
            {
                a.AdditionalMessage = null;
                PopupInfo(msg);
            }
        }

        private static void CreateContactCS(LinearLayout layout, string[] menuItems, Action<string> OnClick, bool useUsername)
        {
            layout.RemoveAllViews();
            if (menuItems == null || menuItems.Length == 0)
                return;

            for (int i = 0; i < Math.Min(menuItems.Length, 4); i++)
            {
                var item = new TextView(layout.Context);
                var par = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                par.SetMargins(0, 2, 0, 2);
                item.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
                item.SetTextColor(new Color(48, 48, 128));
                item.SetTypeface(null, TypefaceStyle.Bold);
                item.PaintFlags |= PaintFlags.UnderlineText;

                var usernameOrId = menuItems[i];
                if (useUsername && char.IsDigit(usernameOrId[0])) // first char is digit, it is using id
                {
                    var cs = Core.FindContact(Util.FullPhoneNumber(usernameOrId));
                    if (cs != null) // Contact found
                        usernameOrId = cs.Username; // Use username instead of id
                }
                item.Text = usernameOrId;
                if (OnClick != null)
                {
                    item.Clickable = true;
                    item.Click += (object sender, EventArgs e) => { OnClick.Invoke(((TextView)sender).Text); };
                }
                layout.AddView(item, par);
            }
        }

        private void AddContactCS(string usernameOrId)
        {
            if (ContactPOS.Current == null)
                return;

            Contact cs;
            if (char.IsDigit(usernameOrId[0])) // uses id?
            {
                usernameOrId = Util.FullPhoneNumber(usernameOrId);
                cs = Core.AddContactAuto(usernameOrId, "CS " + ContactPOS.Current.Name, null);
            }
            else
            {
                cs = Core.FindContactByUsername(usernameOrId);
                if (cs == null)
                    return; // WTH
            }
            StartChat(cs);
        }
    }
}