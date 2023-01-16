using System;
using Android.App;
using Android.Views;
using Android.OS;
using Android.Widget;
using Android.Content;

using ChatAPI;

namespace fmm
{
    public static class POS_Dialog
    {
        public static void PIN(Action<string, object[]> OnOK, params object[] args)
        {
            PIN(null, OnOK, args);
        }

        public static void PIN(string desc, Action<string, object[]> OnOK, params object[] args)
        {
            var layout = Activity.CurrentActivity.LayoutInflater.Inflate(Resource.Layout.POS_AskPIN, null);
            var lbDesc = layout.FindViewById<TextView>(Resource.Id.lbDesc);
            if (string.IsNullOrEmpty(desc))
                lbDesc.Visibility = ViewStates.Gone;
            else
                lbDesc.Text = desc;

            var tbPassword = layout.FindViewById<EditText>(Resource.Id.tbPassword);
            Activity.CurrentActivity.ShowPopup("Masukkan PIN", layout, "OK", () =>
            {
                var password = tbPassword.Text;
                if (!string.IsNullOrEmpty(password))
                    if (OnOK != null) OnOK.Invoke(password, args);
            }, "Batal", null);
        }

        public static void DatePicker(Action<DateTime> OnOK, DateTime date)
        {
            var view = new DatePicker(Activity.CurrentActivity);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                view.CalendarViewShown = false;
                view.SpinnersShown = true;
            }
            view.DateTime = date;
            Activity.CurrentActivity.PopupOK("Pilih Tanggal", view, () => { if (OnOK != null) OnOK.Invoke(view.DateTime); }, null);
        }
    }
}