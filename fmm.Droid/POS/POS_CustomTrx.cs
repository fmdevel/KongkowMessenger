using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    [Activity]
    public class POS_CustomTrx : POS_Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            base.Initialize(Resource.Layout.POS_CustomTrx, POS_CustomListProduct.TypeProduct.Category <= 100 ? "Anda akan membeli" : "Pembayaran");
            var bSend = FindViewById<TextView>(Resource.Id.Send);
            var txtDestination = FindViewById<EditText>(Resource.Id.txtDestination);
            if (POS_CustomListProduct.TypeProduct.Category <= 100)
            {
                if (!string.IsNullOrEmpty(POS_CustomListProduct.Destination)) txtDestination.SetBackgroundDrawable(null);
                if (!POS_CustomListProduct.TypeProduct.EnableHpCustomer) FindViewById(Resource.Id.tbHpCustomer).Visibility = Android.Views.ViewStates.Gone;

                var periods = new int[9];
                for (int i = 0; i < periods.Length; i++)
                    periods[i] = i + 1;

                var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
                cbPeriod.Adapter = new SpinnerAdapter<int>(periods);
                cbPeriod.SetSelection(0);
            }
            else
            {
                FindViewById(Resource.Id.lbPrice).Visibility = Android.Views.ViewStates.Gone;
                bSend.Text = POS_CustomListProduct.TypeProduct.Category == 301 ? "CEK REKENING" : "CEK TAGIHAN";

                var periods = new int[13];
                for (int i = 0; i < periods.Length; i++)
                    periods[i] = i;

                var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
                cbPeriod.Adapter = new SpinnerAdapter<int>(periods);
                cbPeriod.SetSelection(1);
                var lbPeriod = FindViewById<TextView>(Resource.Id.lbPeriod);
                lbPeriod.Text = "Bulan";
                var isBPJS = (POS_CustomListProduct.TypeProduct != null && POS_CustomListProduct.TypeProduct.Category == 105) ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
                lbPeriod.Visibility = isBPJS;
                cbPeriod.Visibility = isBPJS;
            }
            string provider;
            if (POS_CustomListProduct.TypeProduct.Category == 2)
                provider = "Token PLN";
            else
            {
                provider = POS_CustomListProduct.SelectedProduct.Provider;
                if (string.IsNullOrEmpty(provider)) provider = POS_CustomListProduct.TypeProduct.Name;
            }
            FindViewById<TextView>(Resource.Id.lbProvider).Text = provider;

            FindViewById<TextView>(Resource.Id.lbDesc).Text = POS_CustomListProduct.SelectedProduct.Desc;
            FindViewById<TextView>(Resource.Id.lbPrice).Text = Util.Rp(POS_CustomListProduct.SelectedProduct.Price);
            if (POS_CustomListProduct.SelectedResId != 0)
                FindViewById<RoundedImageView>(Resource.Id.Logo).SetImageResource(POS_CustomListProduct.SelectedResId);

            txtDestination.Text = POS_CustomListProduct.Destination;
            txtDestination.Hint = POS_CustomListProduct.TypeProduct.DestinationText;

            if (POS_CustomListProduct.TypeProduct.Category == 301)
            {
                var txtDenom = FindViewById<EditText>(Resource.Id.txtDenom);
                txtDenom.Hint = "Jumlah Transfer";
                txtDenom.Visibility = Android.Views.ViewStates.Visible;
            }

            FindViewById(Resource.Id.MPOS_Page).SetBackgroundColor(Core.Setting.Themes);
            bSend.Click += BtnProcess_Click;
        }
        protected override void OnResume()
        {
            SetButtonColor(Core.Setting.Themes);
            base.OnResume();
        }

        protected override void OnPause()
        {
            SetButtonColor(Android.Graphics.Color.White);
            base.OnPause();
        }

        private void SetButtonColor(Android.Graphics.Color color)
        {
            ((Android.Graphics.Drawables.GradientDrawable)((Android.Graphics.Drawables.LayerDrawable)(FindViewById<TextView>(Resource.Id.Send)).Background).GetDrawable(0)).SetColor(color);
        }

        private void DoProcess()
        {
            var txtDestination = FindViewById<EditText>(Resource.Id.txtDestination);
            var dest = txtDestination.Text.Replace(" ", null).Replace("-", null);
            if (dest.Length <= 4)
            {
                PopupError(FindViewById<TextView>(Resource.Id.lblDestination).Text + " belum benar");
                return;
            }

            var password = FindViewById<EditText>(Resource.Id.tbPassword).Text;
            if (password.Length == 0)
            {
                PopupError("PIN belum diisi");
                return;
            }

            if (ContactPOS.RegistrationNeeded)
            {
                ShowPopup("Informasi",
                    "Anda belum dapat melakukan transaksi karena belum terdaftar.\n\nLakukan registrasi?",
                    "Ya",
                    () => StartActivity(typeof(POS_Register)),
                    "Tidak", null);

                return;
            }
            var a = ContactPOS.Current.AgenInfo;
            if (a != null && a.Saldo <= 0)
            {
                ShowPopup("Informasi",
                    "Anda belum memiliki saldo untuk melakukan transaksi.\n\nLakukan isi deposit?",
                    "Ya",
                    () => StartActivityForResult(typeof(POS_SettingPanel_TicketDeposit), 20),
                    "Tidak", null);

                return;
            }

            if (POS_CustomListProduct.TypeProduct.Category <= 100)
            {
                var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
                ContactPOS.Current.SubmitTrxCustom(POS_CustomListProduct.SelectedProduct, (cbPeriod.SelectedItemPosition == 0) ? null : (cbPeriod.SelectedItemPosition + 1).ToString(), dest, FindViewById<EditText>(Resource.Id.tbHpCustomer).Text.Trim(), 0, password);
            }
            else
            {
                var contactPOS = ContactPOS.Current;
                var product = POS_CustomListProduct.SelectedProduct;
                var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);

                if (product.Category == 301)
                {
                    var txtDenom = FindViewById<EditText>(Resource.Id.txtDenom);
                    var denom = txtDenom.Text.Replace(" ", null).Replace("-", null);
                    if (denom.Length < 5)
                    {
                        PopupError("Jumlah Transfer belum benar");
                        return;
                    }
                    dest = product.Code + dest + "-" + denom; // KodeBank+NoRek-Denom
                }

                contactPOS.InqData = new InqData(Util.UniqeId, product, (product.Category == 105) ? cbPeriod.SelectedItemPosition.ToString() : null, dest, FindViewById<EditText>(Resource.Id.tbHpCustomer).Text.Trim());

                contactPOS.ShowProgress(null, 18000, null);
                contactPOS.CheckBill(contactPOS.InqData, password);
            }
        }

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            DoProcess();
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
            else if (requestCode == 14)
            {
                var contactPOS = ContactPOS.Current;
                var inq = contactPOS.InqData;
                contactPOS.SubmitTrx(inq.Product, inq.Period, inq.Destination, inq.HpCustomer);
            }
        }
    }
}