using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class POS_SettingPanel_ChangePIN : POS_SettingPanel
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize("Ganti PIN", Resource.Drawable.ic_key, Resource.Layout.POS_SettingPanel_ChangePIN);
        }

        protected override void Process()
        {
            var old = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbOldPin).Text).Trim();
            if (old.Length == 0)
            {
                PopupError("PIN Lama belum disi");
                return;
            }
            var @new = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbNewPin).Text).Trim();
            if (@new.Length == 0)
            {
                PopupError("PIN Baru belum disi");
                return;
            }
            if (Util.GuardValue(FindViewById<EditText>(Resource.Id.tbConfPin).Text).Trim() != @new)
            {
                PopupError("Konfirmasi PIN tidak sama");
                return;
            }
            var resultIntent = new Intent();
            resultIntent.PutExtra("new", @new);
            resultIntent.PutExtra("pin", old);
            SetResult(Result.Ok, resultIntent);
            Finish();
        }
    }
}