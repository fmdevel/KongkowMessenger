using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;

namespace fmm
{
    [Activity]
    public class POS_SettingPanel_TicketDeposit : POS_SettingPanel
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize("Isi Deposit", Resource.Drawable.ic_ticket, Resource.Layout.POS_SettingPanel_TicketDeposit);

            HandleEvent(Resource.Id.rb100k);
            HandleEvent(Resource.Id.rb150k);
            HandleEvent(Resource.Id.rb200k);
            HandleEvent(Resource.Id.rb250k);
            FindViewById<RadioButton>(Resource.Id.rbOther).CheckedChange += rbOther_CheckedChange;
        }

        private void rbOther_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            FindViewById<EditText>(Resource.Id.tbSaldo).Visibility = e.IsChecked ? ViewStates.Visible : ViewStates.Invisible;
        }

        private void HandleEvent(int resId)
        {
            FindViewById<RadioButton>(resId).CheckedChange += SetSal;
        }

        private void SetSal(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(e.IsChecked)
                FindViewById<EditText>(Resource.Id.tbSaldo).Text = ((RadioButton)sender).Text;
        }

        private void OnPIN(string password, object[] args)
        {
            var resultIntent = new Intent();
            resultIntent.PutExtra("value", (string)args[0]);
            resultIntent.PutExtra("pin", password);
            SetResult(Result.Ok, resultIntent);
            Finish();
        }

        protected override void Process()
        {
            var sal = ChatAPI.Util.GuardValue(FindViewById<EditText>(Resource.Id.tbSaldo).Text).Trim();
            var chars = new char[sal.Length];
            int count = 0;
            foreach (var c in sal)
                if (char.IsDigit(c)) chars[count++] = c;

            if (count == 0)
            {
                PopupError("Jumlah Deposit belum ditentukan");
                return;
            }
            sal = new string(chars, 0, count);
            if (Convert.ToInt64(sal) <= 0)
            {
                PopupError("Jumlah Deposit harus lebih besar dari 0");
                return;
            }
            POS_Dialog.PIN(OnPIN, sal);
        }
    }
}