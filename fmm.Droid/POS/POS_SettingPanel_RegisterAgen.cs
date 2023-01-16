using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class POS_SettingPanel_RegisterAgen : POS_SettingPanel
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize("Daftar Agen", Resource.Drawable.ic_register, Resource.Layout.POS_SettingPanel_RegisterAgen);
        }

        private void OnPIN(string password, object[] args)
        {
            var resultIntent = new Intent();
            resultIntent.PutExtra("name", (string)args[0]);
            resultIntent.PutExtra("hp", (string)args[1]);
            resultIntent.PutExtra("addr", (string)args[2]);
            resultIntent.PutExtra("postCode", (string)args[3]);
            resultIntent.PutExtra("pin", password);
            SetResult(Result.Ok, resultIntent);
            Finish();
        }

        protected override void Process()
        {
            var name = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbName).Text).Trim();
            if (name.Length <= 2)
            {
                PopupError("Nama belum lengkap");
                return;
            }
            var addr = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbAddress).Text).Trim();
            if (addr.Length < 10)
            {
                PopupError("Alamat belum lengkap");
                return;
            }

            long tmp;
            var postCode = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbPostCode).Text).Trim();
            if (postCode.Length < 5 || postCode.Length > 6 || !long.TryParse(postCode, out tmp))
            {
                PopupError("Kode Pos tidak valid");
                return;
            }

            var hp = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbHP).Text).Trim();
            var hps = hp.Split(',');
            for (int i = 0; i < hps.Length; i++)
            {
                var h = hps[i];
                if (h.Length >= 10)
                {
                    if (h.StartsWith("+62"))
                        h = "0" + h.Substring(3);
                    else if (h.StartsWith("62"))
                        h = "0" + h.Substring(2);
                    if (h[0] == '0' && h.Length >= 10 && h.Length <= 13 && long.TryParse(h, out tmp))
                    {
                        hps[i] = h;
                        goto HpIsValid;
                    }
                }
                PopupError("No Hp tidak valid");
                return;

            HpIsValid:
                ;
            }

            hp = string.Join(",", hps);
            POS_Dialog.PIN(OnPIN, name, hp, addr, postCode);
        }
    }
}