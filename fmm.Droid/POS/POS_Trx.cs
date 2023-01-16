using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    public abstract class POS_Trx : POS_Activity
    {

        protected void Initialize(SpinnerAdapter<Product> adapter, string title)
        {
            base.Initialize(Resource.Layout.POS_Trx, title);
            var cbProductType = FindViewById<Spinner>(Resource.Id.cbProductType);
            cbProductType.ItemSelected += CbProductType_ItemSelected;
            cbProductType.Adapter = m_aProductType = new SpinnerAdapter<TypeProduct>();

            var cbProductProvider = FindViewById<Spinner>(Resource.Id.cbProductProvider);
            cbProductProvider.ItemSelected += CbProductProvider_ItemSelected;
            cbProductProvider.Adapter = m_aProductProvider = new SpinnerAdapter<Provider>();

            FindViewById<TextView>(Resource.Id.Title).Text = title;
            FindViewById<Spinner>(Resource.Id.cbProduct).Adapter = m_aProduct = adapter;
            FindViewById(Resource.Id.Send).Click += BtnProcess_Click;
        }


        private SpinnerAdapter<TypeProduct> m_aProductType;
        private SpinnerAdapter<Provider> m_aProductProvider;
        private SpinnerAdapter<Product> m_aProduct;

        protected abstract void Process(Product product, string destination);
        protected virtual void OnTypeProductChanged(TypeProduct type)
        {
        }

        public void SetProduct(ContactPOS contact)
        {
            if ((this as POS_TrxPrepaid) != null)
                SetProduct(contact.ProductProviderPrepaid);
            else if ((this as POS_TrxPostpaid) != null)
                SetProduct(contact.ProductProviderPostpaid);
        }

        protected void SetProduct(System.Collections.Generic.List<ListProvider> listProvider)
        {
            var aType = new TypeProduct[listProvider.Count];
            for (int i = 0; i < aType.Length; i++)
                aType[i] = listProvider[i].Type;

            m_aProductType.Items = aType;
            m_aProductType.NotifyDataSetChanged();

            if (listProvider.Count > 0)
                ValidateProductType(FindViewById<Spinner>(Resource.Id.cbProductType).SelectedItemPosition);
        }

        private void ValidateProductType(int position)
        {
            if (position < 0)
                return;

            var lblDestination = FindViewById<TextView>(Resource.Id.lblDestination);
            lblDestination.Text = null;
            if (position < m_aProductType.Items.Count)
            {
                var type = m_aProductType.Items[position]; // m_aProductType.GetItem(position);
                if (type != null)
                {
                    var providers = ContactPOS.Current.GetListProvider(type.Category).Providers;
                    m_aProductProvider.Items = providers; //m_aProductProvider.AddAll(providers);
                    m_aProductProvider.NotifyDataSetChanged();

                    if (providers.Count > 0)
                        ValidateProductProvider(FindViewById<Spinner>(Resource.Id.cbProductProvider).SelectedItemPosition);

                    lblDestination.Text = type.DestinationText;
                }
                OnTypeProductChanged(type);
            }
        }

        private void ValidateProductProvider(int position)
        {
            if (position < 0)
                return;

            if (position < m_aProductProvider.Items.Count)
            {
                var prov = m_aProductProvider.Items[position];
                if (prov != null)
                {
                    m_aProduct.Items = prov.ListProduct;
                    m_aProduct.NotifyDataSetChanged();
                }
            }
        }

        private void CbProductType_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            ValidateProductType(e.Position);
        }

        private void CbProductProvider_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            ValidateProductProvider(e.Position);
        }

        private void DoProcess()
        {
            var cbProduct = FindViewById<Spinner>(Resource.Id.cbProduct);
            if (cbProduct.SelectedItemPosition < 0)
            {
                PopupError("Produk belum dipilih");
                return;
            }

            var txtDestination = FindViewById<EditText>(Resource.Id.txtDestination);
            var dest = txtDestination.Text.Replace(" ", null).Replace("-", null);
            if (dest.Length <= 4)
            {
                PopupError(FindViewById<TextView>(Resource.Id.lblDestination).Text + " belum benar");
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

            Process(m_aProduct.Items[cbProduct.SelectedItemPosition], dest);
            FindViewById<EditText>(Resource.Id.tbHpCustomer).Text = null;
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

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            DoProcess();
        }
    }
}