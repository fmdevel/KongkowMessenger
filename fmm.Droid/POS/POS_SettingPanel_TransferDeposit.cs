using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace fmm
{
    [Activity]
    public class POS_SettingPanel_TransferDeposit : POS_SettingPanel
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize("Transfer Deposit", Resource.Drawable.ic_transfer, Resource.Layout.POS_SettingPanel_TransferDeposit);
        }

        private void OnPIN(string password, object[] args)
        {
            var resultIntent = new Intent();
            resultIntent.PutExtra("target", (string)args[0]);
            resultIntent.PutExtra("value", (string)args[1]);
            resultIntent.PutExtra("pin", password);
            SetResult(Result.Ok, resultIntent);
            Finish();
        }

        protected override void Process()
        {
            var sal = ChatAPI.Util.GuardValue(FindViewById<EditText>(Resource.Id.tbSaldo).Text).Trim();
            if (string.IsNullOrEmpty(sal))
            {
                PopupError("Jumlah Deposit harus di isi");
                return;
            }
            if (Convert.ToInt64(sal) <= 0)
            {
                PopupError("Jumlah Deposit harus lebih besar dari 0");
                return;
            }
            var target = ChatAPI.Util.GuardValue(FindViewById<EditText>(Resource.Id.tbHP).Text).Trim();
            if (target.Length == 0)
            {
                PopupError("Kode Agen / No HH belum diisi");
                return;
            }
            POS_Dialog.PIN(OnPIN, target, sal);
        }
    }
}