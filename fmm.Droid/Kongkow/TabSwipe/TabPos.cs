#if BUILD_PARTNER
using System;
using System.Threading;

using Android.App;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public partial class MainActivity
    {
        private void Initialize_Pos()
        {
            var mPulsa = CreateMenu(Resource.Drawable.custom_pulsa, "Pulsa & Data", 1);
            var mToken = CreateMenu(Resource.Drawable.custom_token, "Token PLN", 2, "PLN");
            var mVoucherGame = CreateMenu(Resource.Drawable.custom_vouchergame, "Voucher Game", 3, "Voucher Game");
            var mVoucherTV = CreateMenu(Resource.Drawable.custom_vouchertv, "Voucher TV", 4, "Voucher TV");

            var mECommerce = CreateMenu(Resource.Drawable.custom_ecommerce, "e-Commerce", 5);
            var mGojek = CreateMenu(Resource.Drawable.custom_saldogojek, "Saldo Gojek", 5, "Gojek");
            var mGrab = CreateMenu(Resource.Drawable.custom_saldograb, "Saldo Grab", 5, "Grab");
            var mEMoney = CreateMenu(Resource.Drawable.custom_emoney, "e-Money", 6);

            var mBioskop = CreateMenu(Resource.Drawable.custom_bioskop, "Bioskop", 5, "Bioskop");
            var mTiketPesawat = CreateMenu(Resource.Drawable.custom_tiketpesawat, "Tiket\nPesawat", 0);
            var mTiketKereta = CreateMenu(Resource.Drawable.custom_tiketkereta, "Tiket\nKereta", 0);
            var mLinkAja = CreateMenu(Resource.Drawable.custom_linkaja, "LinkAja", 6, "LinkAja");

            var mHPPascabayar = CreateMenu(Resource.Drawable.custom_hppascabayar, "HP\nPascabayar", 106);
            var mTelkom = CreateMenu(Resource.Drawable.custom_telkom, "Telkom Group", 102);
            var mPLNPascabayar = CreateMenu(Resource.Drawable.custom_plnpascabayar, "PLN\nPascabayar", 101);
            var mTVBerlangganan = CreateMenu(Resource.Drawable.custom_tvberlangganan, "TV\nBerlangganan", 104);

            var mBPJS = CreateMenu(Resource.Drawable.custom_bpjs, "BPJS", 105);
            var mPDAM = CreateMenu(Resource.Drawable.custom_pdam, "PDAM", 103);
            var mTransBank = CreateMenu(Resource.Drawable.custom_transbank, "Transfer Bank", 301);
            var mFlashKurir = CreateMenu(Resource.Drawable.custom_flashkurir, "Flash Kurir", () => { OpenFlashKurir(ContactPOS.Current); });

            var mFinance = CreateMenu(Resource.Drawable.custom_finance, "Finance", 107);
            //var mOther = CreateMenu(Resource.Drawable.custom_add, "Lainnya", (Action)null);
            var mVoucherDeals = CreateMenu(Resource.Drawable.custom_voucherdeals, "Voucher\nDeals", 5, "Voucher Deal");

            var menuPP = CreateMenuPopup(
                new MenuIconCollection(this, 32, mPulsa, mToken, mVoucherGame, mVoucherTV),
                new MenuIconCollection(this, 32, mECommerce, mGojek, mGrab, mEMoney),
                new MenuIconCollection(this, 32, mBioskop, mTiketPesawat, mTiketKereta, mLinkAja),
                new MenuIconCollection(this, 32, mHPPascabayar, mTelkom, mPLNPascabayar, mTVBerlangganan),
                new MenuIconCollection(this, 32, mBPJS, mPDAM, mFinance, mVoucherDeals),
                new MenuIconCollection(this, 32, mTransBank, mFlashKurir, CreateBlankMenu(), CreateBlankMenu()));

            var content = m_tabView.GetContent(3).FindViewById<ViewGroup>(Resource.Id.menuPP);
            content.AddView(menuPP);

            var mInfo = CreateMenu(Resource.Drawable.custom_informasi, "Informasi", typeof(POS_Info));
            var mAdministrasi = CreateMenu(Resource.Drawable.custom_administrasi, "Administrasi", typeof(POS_Setting));
            var mDataTrx = CreateMenu(Resource.Drawable.custom_datatransaksi, "Data Transaksi", typeof(POS_History));

            var menuInfo = CreateMenuPopup(
                new MenuIconCollection(this, 32, mInfo, mAdministrasi, mDataTrx));

            content = m_tabView.GetContent(3).FindViewById<ViewGroup>(Resource.Id.menuInfo);
            content.AddView(menuInfo);
        }

        private MenuIcon CreateBlankMenu()
        {
            return CreateMenu(Resource.Drawable.custom_blank, string.Empty, (Action)null);
        }

        private MenuIcon CreateMenu(int iconResId, string text, Type type)
        {
            return CreateMenu(iconResId, text, () => { StartActivity(type); });
        }
        private MenuIcon CreateMenu(int iconResId, string text, Action action)
        {
            var menu = new MenuIcon(this, text, 13, Orientation.Vertical, iconResId, 40);
            if (action != null)
            {
                menu.Click += (sender, e) =>
                {
                    if (ContactPOS.Current == null)
                        PopupError(Resources.GetString(Resource.String.NoConnection));
                    else
                        action.Invoke();
                };
            }
            return menu;
        }

        private MenuIcon CreateMenu(int iconResId, string text, int category)
        {
            return CreateMenu(iconResId, text, category, null);
        }
        private MenuIcon CreateMenu(int iconResId, string text, int category, string providerName)
        {
            var menu = new MenuIcon(this, text, 13, Orientation.Vertical, iconResId, 40);
            menu.Click += (sender, e) =>
            {
                if (ContactPOS.Current == null)
                    PopupError(Resources.GetString(Resource.String.NoConnection));
                else if (category <= 0)
                    PopupInfo("Coming soon!");
                else
                    ScanProvider(category, providerName);
            };
            return menu;
        }

        private void ScanProvider(int category, string providerName)
        {
            var contact = ContactPOS.Current;
            if (contact == null)
            {
                PopupError(Resources.GetString(Resource.String.NoConnection));
                return;
            }

            var listProvider = contact.GetListProvider(category);
            if (listProvider == null || listProvider.Providers.Count == 0)
            {
                goto Unavailable;
            }

            if (category == 1)
            {
                POS_CustomListProduct.TypeProduct = listProvider.Type;
                POS_CustomListProduct.Start(null, null);
                return;
            }
            else if (category <= 100)
            {
                if (string.IsNullOrEmpty(providerName))
                {
                    var layout = LayoutInflater.Inflate(Resource.Layout.Kongkow_ListViewer, null);
                    var adapter = new CustomProviderAdapter();
                    adapter.Items = listProvider.Providers;
                    POS_CustomListProduct.TypeProduct = listProvider.Type;
                    layout.FindViewById<ListView>(Resource.Id.ListViewer).Adapter = adapter;
                    adapter.Dialog = CreatePopup(listProvider.Type.Name, layout, null, null, null, null);
                    adapter.Dialog.Show();
                    return;
                }
                else
                {
                    foreach (ChatAPI.Connector.Provider provider in listProvider.Providers)
                    {
                        if (string.Equals(provider.Name, providerName, StringComparison.OrdinalIgnoreCase))
                        {
                            POS_CustomListProduct.TypeProduct = listProvider.Type;
                            POS_CustomListProduct.Start(provider.Name, provider.ListProduct);
                            return;
                        }
                    }
                }
            }
            else if (category == 101) // PLN Postpaid
            {
                POS_CustomListProduct.TypeProduct = listProvider.Type; ;
                POS_CustomListProduct.SelectedProduct = listProvider.Providers[0].ListProduct[0];
                POS_CustomListProduct.SelectedResId = Resource.Drawable.custom_plnpascabayar;
                POS_CustomListProduct.Destination = null;
                StartActivity(typeof(POS_CustomTrx));
                return;
            }
            else if (category <= 200 || category == 301)
            {
                var layout = LayoutInflater.Inflate(Resource.Layout.Kongkow_ListViewer, null);
                var adapter = new CustomSimpleProductAdapter();
                adapter.Items = listProvider.Providers[0].ListProduct;
                adapter.TypeProduct = listProvider.Type;
                layout.FindViewById<ListView>(Resource.Id.ListViewer).Adapter = adapter;
                adapter.Dialog = CreatePopup(listProvider.Type.Name, layout, null, null, null, null);
                adapter.Dialog.Show();
                return;
            }

        Unavailable:
            PopupError("Produk saat ini belum tersedia");
        }


        internal void Refresh_Pos()
        {
            if (ContactPOS.Current != null)
            {
                Pos_SetInfo();
            }
        }

        internal void Update_Pos()
        {
            m_tabIcons[3].State = TabState.Active;
            AnyUnprocessedPosUpdate = false;
            m_tabIcons[0].State = AnyUnprocessedChat ? TabState.InactiveNotif : TabState.Inactive;
            m_tabIcons[1].State = AnyUnprocessedFeed ? TabState.InactiveNotif : TabState.Inactive;
            m_tabIcons[2].State = TabState.Inactive;

            var view = FindViewById(Resource.Id.ActivityFooter);
            if (view != null)
                view.SetBackgroundColor(Core.Setting.Themes);

            if (Core.Owner != null)
            {
                var dp = FindViewById<CircleImageView>(Resource.Id.MPOS_DP);
                UIUtil.SetDefaultDP(dp, Core.Owner);
            }

            if (ContactPOS.Current != null)
            {
                ContactPOS.Current.GetAgenInfo(false);
                Pos_SetInfo();
            }
        }


        internal void Pos_SetInfo()
        {
            if (m_bannerWait == null)
            {
                m_bannerWait = new AutoResetEvent(false);
                m_bannerForceUpdate = true;
                Util.StartThread(BannerLoop);
            }
            else
            {
                m_bannerForceUpdate = true;
                m_bannerWait.Set();
            }

            Pos_SetServerInfo();
            Pos_SetAgenInfo();
            ContactPOS.Current.GetProduct();
        }

        private void Pos_SetServerInfo()
        {
            var status = ContactPOS.Current.Status;
            if (status.Length == 0)
                status = ContactPOS.Current.Name;

            var marquee = m_tabView.GetContent(3).FindViewById<TextView>(Resource.Id.MPOS_HeaderTitle);
            marquee.Text = status;
            marquee.Selected = true;
        }

        private void Pos_SetAgenInfo()
        {
            var a = ContactPOS.Current.AgenInfo;
            if (a == null)
                return;

            var content = m_tabView.GetContent(3);
            content.FindViewById<TextView>(Resource.Id.lbName).Text = a.Nama;
            content.FindViewById<TextView>(Resource.Id.lbSaldo).Text = Util.Rp(a.Saldo);
            content.FindViewById<TextView>(Resource.Id.lbStatus).Text = (a.Aktif ? "Aktif" : "Non Aktif");

            var view = content.FindViewById(Resource.Id.ActivityFooter);
            if (view != null)
                view.Visibility = ViewStates.Visible;

            HackRecentChat(a);
        }

        private void HackRecentChat(ChatAPI.Connector.Agen agen)
        {
            var m = agen.Saldo.ToString();
            // Hack recent chat to get high position order in chat list
            m = "Saldo=" + m + " " + ContactPOS.Current.Status;
            var hackRecentChat = ContactPOS.Current.CacheRecentStatus;
            if (hackRecentChat == null)
            {
                ContactPOS.Current.CacheRecentStatus = hackRecentChat = new ChatMessage(ContactPOS.Current, ChatMessage.TypeChat.PRIVATE_CHAT, m);
                Core.UpdateRecentChat(hackRecentChat);
            }
            else if (hackRecentChat.Message != m)
            {
                //hackRecentChat.Message = m;
                //hackRecentChat.Time = DateTime.Now;
                Core.UpdateRecentChat(hackRecentChat);
            }

            var msg = agen.AdditionalMessage;
            if (!string.IsNullOrEmpty(msg))
            {
                agen.AdditionalMessage = null;
                PopupInfo(msg);
            }
        }

        private static Core.Banner m_bannerCurrent;
        private static string m_bannerLastFile;
        private static int m_bannerCounter;
        private static bool m_bannerForceUpdate;
        private static AutoResetEvent m_bannerWait;

        private static void MaintainBanner()
        {
            if (m_bannerCounter == 0)
                m_bannerCounter = (Util.UniqeId & 255);

            var a = CurrentActivity as MainActivity;
            if (a == null || a.ActiveTabIndex != 3)
                return;

            var banners = Core.GetActiveBanners();
            m_bannerCurrent = (banners == null || banners.Length == 0) ? null : banners[m_bannerCounter++ % banners.Length];
            a.RunOnUiThread(a.ShowBanner);
        }

        private void ShowBanner()
        {
            var pb = FindViewById<ImageView>(Resource.Id.pbBanner);
            if (pb == null)
                return;

            var b = m_bannerCurrent;
            if (b == null)
            {
                if (!string.IsNullOrEmpty(m_bannerLastFile))
                {
                    m_bannerLastFile = null;
                    pb.SetImageURI(null);
                    pb.Visibility = ViewStates.Gone;
                }
                return;
            }

            int height;
            if (Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
                height = (UIUtil.DisplayMetrics.WidthPixelsLandscape * 2) / 3;
            else
                height = (UIUtil.DisplayMetrics.WidthPixelsPotrait * 2) / 3;

            height = (height * 70) / 100;

            var layout = pb.LayoutParameters;
            if (height != layout.Height)
            {
                layout.Height = height;
                pb.LayoutParameters = layout;
            }

            if (m_bannerLastFile != b.FileName || m_bannerForceUpdate)
            {
                m_bannerLastFile = b.FileName;
                if (m_bannerForceUpdate)
                {
                    m_bannerForceUpdate = false;
                    pb.SetImageURI(null);
                }
                pb.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(b.FileName)));
            }
            if (pb.Visibility != ViewStates.Visible)
                pb.Visibility = ViewStates.Visible;

            pb.Click -= BannerClick; // Fix click not working bug
            pb.Click += BannerClick; // Fix click not working bug
        }

        private static void BannerLoop()
        {
            while (true)
            {
                MaintainBanner();
                m_bannerWait.WaitOne(Network.UserSeen ? 5000 : 27000);
            }
        }

        private void BannerClick(object sender, EventArgs e)
        {
            var b = m_bannerCurrent;
            if (b != null)
                TryNavigate(b.Url);
        }
    }
}
#endif