using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    [Activity]
    public class POS_TrxPrepaid : POS_Trx
    {
        private const int DEFAULT_PERIOD = 0;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize(new ProductAdapter(), "Pembelian");

            var periods = new int[9];
            for (int i = 0; i < periods.Length; i++)
                periods[i] = i + 1;

            var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
            cbPeriod.Adapter = new SpinnerAdapter<int>(periods);
            cbPeriod.SetSelection(DEFAULT_PERIOD);

            ContactPOS.Current.GetProduct();
        }

        protected override void OnResume()
        {
            base.OnResume();
            SetProduct(ContactPOS.Current.ProductProviderPrepaid);
        }

        protected override void OnTypeProductChanged(TypeProduct type)
        {
            var tbHpCustomer = FindViewById<EditText>(Resource.Id.tbHpCustomer);
            if (type != null && type.EnableHpCustomer)
            {
                tbHpCustomer.Visibility = ViewStates.Visible;
            }
            else
            {
                tbHpCustomer.Visibility = ViewStates.Gone;
                tbHpCustomer.Text = null;
            }
        }

        protected override void Process(Product product, string destination)
        {
            var cbPeriod = FindViewById<Spinner>(Resource.Id.cbPeriod);
            ContactPOS.Current.SubmitTrx(product, (cbPeriod.SelectedItemPosition == DEFAULT_PERIOD) ? null : (cbPeriod.SelectedItemPosition + 1).ToString(), destination, FindViewById<EditText>(Resource.Id.tbHpCustomer).Text.Trim());
            cbPeriod.SetSelection(DEFAULT_PERIOD);
        }
    }
}