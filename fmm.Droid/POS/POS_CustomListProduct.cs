using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    [Activity]
    public class POS_CustomListProduct : POS_Activity
    {
        internal static string ProviderName;
        internal static List<Product> ListProduct;
        internal static TypeProduct TypeProduct;
        internal static Product SelectedProduct;
        internal static int SelectedResId;
        internal static string Destination;

        internal static void Start(string providerName, List<Product> listProduct)
        {
            ProviderName = TypeProduct.Category == 2 ? "Token PLN" : providerName;
            ListProduct = listProduct;
            SelectedResId = GetRes(providerName);
            CurrentActivity.StartActivity(typeof(POS_CustomListProduct));
        }

        private CustomProductAdapter m_adapter;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Destination = null;
            m_adapter = new CustomProductAdapter();
            if (TypeProduct.Category == 1)
            {
                Initialize(Resource.Layout.POS_CustomTrxPulsa, "Pulsa & Data");
                m_listProduct = new List<Product>[3];
                m_lastTab = -1;
            }
            else
            {
                Initialize(Resource.Layout.POS_CustomListProduct, ProviderName);
                if (SelectedResId != 0)
                {
                    var logo = new ImageView(this);
                    int logoSize = UIUtil.DpToPx(40);
                    logo.SetScaleType(ImageView.ScaleType.FitXy);
                    var par = new ViewGroup.MarginLayoutParams(logoSize, logoSize);
                    par.MarginEnd = UIUtil.DpToPx(10);
                    logo.LayoutParameters = par;
                    logo.SetImageResource(SelectedResId);
                    FindViewById<LinearLayout>(Resource.Id.ActivityHeader).AddView(logo, 1);
                }
                m_adapter.Items = ListProduct;
            }
            FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_adapter;
            FindViewById(Resource.Id.MPOS_Page).SetBackgroundColor(Core.Setting.Themes);
            if (TypeProduct.Category == 1) HandlePulsa();
        }

        private void HandlePulsa()
        {
            var h = FindViewById<LinearLayout>(Resource.Id.CustomHeader);
            for (int index = 0; index < 3; index++)
            {
                var tab = h.GetChildAt(index);
                tab.Tag = index;
                tab.Click += (sender, e) => { if (m_lastTab >= 0) SelectTab((int)((View)sender).Tag); };
            }
            FindViewById<EditText>(Resource.Id.txtDestination).TextChanged += DestinationChanged;
            FindViewById(Resource.Id.btnAddContact).Click += AddContact;
        }

        private void AddContact(object sender, EventArgs e)
        {
            if (EnsurePermissionGranted(false))
                BrowseContact();
            else
                EnsurePermissionGranted(true);
        }
        private void BrowseContact()
        {
            var uri = Android.Net.Uri.Parse("content://contacts");
            Intent intent = new Intent(Intent.ActionPick, uri); // new Intent(Intent.ACTION_PICK, _uri);
            intent.SetType(Android.Provider.ContactsContract.CommonDataKinds.Phone.ContentType); // intent.SetType(Phone.CONTENT_TYPE);
            StartActivityForResult(intent, 178);
        }

        private string m_lastPrefix;
        private int m_lastTab;
        private List<Product>[] m_listProduct;
        private void DestinationChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            var dest = ((EditText)sender).Text;
            if (dest.Length >= 10)
            {
                dest = NormalizeNumber(dest);
                if (dest.StartsWith("62")) dest = "0" + dest.Substring(2);
                if (dest.Length >= 10)
                {
                    Destination = dest;
                    if (string.IsNullOrEmpty(m_lastPrefix) || !dest.StartsWith(m_lastPrefix))
                    {
                        m_lastPrefix = dest.Substring(0, 4);
                        var title = FindViewById<TextView>(Resource.Id.Title);
                        var logo = FindViewById<RoundedImageView>(Resource.Id.Logo);
                        var op = OpPrefixLogo.Get(dest);
                        if (op == null)
                        {
                            title.Text = "Pulsa & Data";
                            logo.SetImageDrawable(null);
                        }
                        else
                        {
                            title.Text = op.Name;
                            logo.SetImageResource(op.ResId);
                            SelectedResId = op.ResId;
                        }
                        ProductPrefix.GetListProduct(dest, out m_listProduct[0], out m_listProduct[1], out m_listProduct[2]);
                        if (m_listProduct[0] != null || m_listProduct[1] != null || m_listProduct[2] != null)
                        {
                            SelectTab(m_lastTab == -1 ? 0 : m_lastTab);
                            return;
                        }
                    }
                    else
                    {
                        SelectTab(m_lastTab == -1 ? 0 : m_lastTab);
                        return;
                    }
                }
            }

            SelectTab(-1);
        }

        private void SelectTab(int index)
        {
            if (m_lastTab != index)
                for (int i = 0; i < 3; i++)
                    if (i != index) NormalizeTab(i);

            var listViewer = FindViewById<ListView>(Resource.Id.ListViewer);
            if (index >= 0)
            {
                if (m_lastTab != index)
                {
                    var tab = (LinearLayout)FindViewById<LinearLayout>(Resource.Id.CustomHeader).GetChildAt(index);
                    var c = Core.Setting.Themes;
                    ((TextView)tab.GetChildAt(0)).SetTextColor(c);
                    tab.GetChildAt(1).SetBackgroundColor(c);
                }
                IList<Product> list = m_listProduct[index];
                if (list == null) list = IListAdapter<Product>.Empty;
                if (m_adapter.Items != list)
                {
                    m_adapter.Items = list;
                    m_adapter.NotifyDataSetChanged();
                }
                listViewer.Visibility = ViewStates.Visible;
            }
            else
                listViewer.Visibility = ViewStates.Gone;

            m_lastTab = index;
        }

        private static string NormalizeNumber(string value)
        {
            var chars = new char[value.Length];
            int index = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c >= '0' && c <= '9')
                    chars[index++] = c;
            }
            return new string(chars, 0, index);
        }

        private void NormalizeTab(int index)
        {
            var tab = (LinearLayout)FindViewById<LinearLayout>(Resource.Id.CustomHeader).GetChildAt(index);
            ((TextView)tab.GetChildAt(0)).SetTextColor(Android.Graphics.Color.DimGray);
            tab.GetChildAt(1).SetBackgroundDrawable(null);
        }



        private class OpPrefixLogo
        {
            private static OpPrefixLogo[] m_all =
            new OpPrefixLogo[] {
                new OpPrefixLogo(Resource.Drawable.custom_simpati, "SIMPATI", "0811 0812 0813 0821 0822"),
                new OpPrefixLogo(Resource.Drawable.custom_as, "KARTU AS", "0851 0852 0853 0854 0823"),
                new OpPrefixLogo(Resource.Drawable.custom_mentari, "MENTARI", "0814 0815 0816 0858"),
                new OpPrefixLogo(Resource.Drawable.custom_im3, "IM3", "0855 0856 0857"),
                new OpPrefixLogo(Resource.Drawable.custom_xl, "XL", "0817 0818 0819 0859 0877 0878 0879"),
                new OpPrefixLogo(Resource.Drawable.custom_axis, "AXIS", "0831 0832 0833 0838"),
                new OpPrefixLogo(Resource.Drawable.custom_tri, "TRI", "0895 0896 0897 0898 0899"),
                new OpPrefixLogo(Resource.Drawable.custom_smartfren, "SMARTFREN", "0881 0882 0883 0884 0885 0886 0887 0888 0889")
            };

            internal string[] Prefixes;
            internal string Name;
            internal int ResId;
            private OpPrefixLogo(int resId, string name, string prefixes)
            {
                this.ResId = resId;
                this.Name = name;
                Prefixes = prefixes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }

            internal static OpPrefixLogo Get(string phoneNumber)
            {
                foreach (OpPrefixLogo op in m_all)
                {
                    foreach (string p in op.Prefixes)
                        if (phoneNumber.StartsWith(p))
                            return op;
                }
                return null;
            }
        }

        internal static int GetRes(string provider)
        {
            int resId = ProviderLogo.Get(provider);
            if (resId == 0 && TypeProduct != null) resId = ProviderLogo.Get(TypeProduct.Name);
            return resId;
        }

        internal static class ProviderLogo
        {
            internal static string[] Names = (
                "Pulsa,PLN,Voucher Game,Voucher TV,e-Commerce,e-Money," +
                "TELKOM,PDAM,TV Berlangganan,BPJS,HP Pascabayar,Finance," +
                "Gojek,Grab,Tokepedia,Deal,Bioskop,Shopee,Dana,OVO,TapCash,Flazz,BRIZZI,LinkAja")
                .Split(',', StringSplitOptions.None);
            internal static int[] ResIds = new int[] {
                Resource.Drawable.custom_pulsa,Resource.Drawable.custom_token,Resource.Drawable.custom_vouchergame,Resource.Drawable.custom_vouchertv,Resource.Drawable.custom_ecommerce, Resource.Drawable.custom_emoney,
                Resource.Drawable.custom_telkom,Resource.Drawable.custom_pdam,Resource.Drawable.custom_tvberlangganan, Resource.Drawable.custom_bpjs,Resource.Drawable.custom_hppascabayar,Resource.Drawable.custom_finance,
                Resource.Drawable.custom_saldogojek, Resource.Drawable.custom_saldograb,Resource.Drawable.custom_tokopedia,Resource.Drawable.custom_voucherdeals,Resource.Drawable.custom_bioskop,Resource.Drawable.custom_shopeepay,Resource.Drawable.custom_dana,Resource.Drawable.custom_ovo,Resource.Drawable.custom_bnitapcash,Resource.Drawable.custom_flazz,Resource.Drawable.custom_brizzi,Resource.Drawable.custom_linkaja};

            internal static int Get(string provider)
            {
                if (!string.IsNullOrEmpty(provider))
                    for (int i = 0; i < Names.Length; i++)
                    {
                        if (provider.IndexOf(Names[i], StringComparison.InvariantCultureIgnoreCase) >= 0)
                            return ResIds[i];
                    }
                return 0;
            }
        }

        #region " Permissions "

        private static int m_permissionState; // 0=Unknown, 1=Granted, 2=Asking
        private static bool ReadContactsPermissionGranted { get { return m_permissionState == 1; } }

        private bool EnsurePermissionGranted(bool ask)
        {
            if (ReadContactsPermissionGranted)
                return true;

            if (m_permissionState == 0)
            {
                if (VerifyReadContactsPermission())
                {
                    m_permissionState = 1; // Granted
                    return true;
                }
                if (ask)
                {
                    m_permissionState = 2; // Asking
                    RequestPermission();
                }
            }
            return false;
        }

        private static bool VerifyReadContactsPermission()
        {
            return VerifyPermission(Android.Manifest.Permission.ReadContacts);
        }

        private void RequestPermission()
        {
            RequestPermissions(new string[] { Android.Manifest.Permission.ReadContacts }, 78);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            if (requestCode == 78)
            {
                if (grantResults.Length > 0)
                {
                    for (int i = 0; i < grantResults.Length; i++) if (grantResults[i] != Android.Content.PM.Permission.Granted)
                        {
                            m_permissionState = 0; // Denied, then Restore state to Unknown
                            return;
                        }
                    m_permissionState = 1; // Granted
                    //Recreate();
                    BrowseContact();
                }
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok && requestCode == 178)
            {
                try
                {
                    var uri = data.Data;
                    var projection = new string[] { Android.Provider.ContactsContract.CommonDataKinds.Phone.Number, "display_name" };
                    var cursor = Android.App.Application.Context.ContentResolver.Query(uri, projection, null, null, null);
                    cursor.MoveToNext();

                    int numberColumnIndex = cursor.GetColumnIndex(Android.Provider.ContactsContract.CommonDataKinds.Phone.Number);
                    FindViewById<EditText>(Resource.Id.txtDestination).Text = cursor.GetString(numberColumnIndex);
                }
                catch { }
            }
        }

        #endregion
    }
}