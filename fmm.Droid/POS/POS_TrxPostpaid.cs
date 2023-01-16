using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    [Activity]
    public class POS_TrxPostpaid : POS_Trx
    {
        private const int DEFAULT_PERIOD = 1;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize(new SpinnerAdapter<Product>(), "Pembayaran");

            var periods = new int[13];
            for (int i = 0; i < periods.Length; i++)
                periods[i] = i;

            var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
            cbPeriod.Adapter = new SpinnerAdapter<int>(periods);
            cbPeriod.SetSelection(DEFAULT_PERIOD);
            cbPeriod.Visibility = ViewStates.Invisible;
            var lbPeriod = FindViewById<TextView>(Resource.Id.lbPeriod);
            lbPeriod.Text = "Bulan";
            lbPeriod.Visibility = ViewStates.Invisible;

            ContactPOS.Current.GetProduct();
        }


        protected override void OnResume()
        {
            base.OnResume();
            SetProduct(ContactPOS.Current.ProductProviderPostpaid);
        }

        protected override void OnTypeProductChanged(TypeProduct type)
        {
            var isBPJS = (type != null && type.Category == 105) ? ViewStates.Visible : ViewStates.Gone;
            FindViewById<TextView>(Resource.Id.lbPeriod).Visibility = isBPJS;
            FindViewById<Spinner>(Resource.Id.cbPeriod).Visibility = isBPJS;
        }

        private void OnTrxPIN(string password, object[] args)
        {
            var contactPOS = ContactPOS.Current;
            var product = (Product)args[0];
            var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
            contactPOS.InqData = new InqData(Util.UniqeId, product, (product.Category == 105) ? cbPeriod.SelectedItemPosition.ToString() : null, (string)args[1], (string)args[2]);
            contactPOS.ShowProgress(null, 18000, null);
            contactPOS.CheckBill(contactPOS.InqData, password);
            cbPeriod.SetSelection(DEFAULT_PERIOD);
        }

        protected override void Process(Product product, string destination)
        {
            POS_Dialog.PIN(OnTrxPIN, product, destination, FindViewById<EditText>(Resource.Id.tbHpCustomer).Text.Trim());
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;

            if (requestCode == 14)
            {
                var contactPOS = ContactPOS.Current;
                var inq = contactPOS.InqData;
                contactPOS.SubmitTrx(inq.Product, inq.Period, inq.Destination, inq.HpCustomer);
            }
        }
    }
}