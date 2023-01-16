using System;

using Android.App;
using Android.OS;
using Android.Content;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class POS_Register : POS_Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize(Resource.Layout.POS_Register, "Registrasi Transaksi");
            FindViewById(Resource.Id.Send).Click += BtnProcess_Click;

            var tos = FindViewById<TextView>(Resource.Id.TOS);
            tos.Click += TOS;
            tos.PaintFlags |= Android.Graphics.PaintFlags.UnderlineText;

            if (Core.Owner.ID != Core.Owner.Name)
                FindViewById<EditText>(Resource.Id.tbName).Text = Core.Owner.Name;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!ContactPOS.RegistrationNeeded)
                Finish();
        }

        private void TOS(object sender, EventArgs e)
        {
            TryNavigate(Util.APP_TOS);
        }

        private void BtnProcess_Click(object sender, EventArgs e)
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

            var pin = Util.GuardValue(FindViewById<EditText>(Resource.Id.tbNewPin).Text).Trim();
            if (pin.Length == 0)
            {
                PopupError("PIN belum disi");
                return;
            }
            if (pin.Length <= 3)
            {
                PopupError("PIN terlalu pendek");
                return;
            }
            if (pin == "1234")
            {
                PopupError("PIN tidak aman atau mudah ditebak");
                return;
            }
            if (Util.GuardValue(FindViewById<EditText>(Resource.Id.tbConfPin).Text).Trim() != pin)
            {
                PopupError("Konfirmasi PIN tidak sama");
                return;
            }

            ContactPOS.Current.ShowProgress(null, 18000, "Hasil tidak diketahui");
            ContactPOS.Current.Register(name, addr, postCode, pin);
        }

    }
}