using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class POS_Setting : POS_Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Initialize(Resource.Layout.POS_Setting, "Administrasi");

            var mTicketDeposit = new MenuIcon(this, "Isi Deposit", Resource.Drawable.ic_ticket);
            mTicketDeposit.Click += (sender, e) => StartActivityForResult(typeof(POS_SettingPanel_TicketDeposit), 20);

            var mTransferDeposit = new MenuIcon(this, "Transfer Deposit", Resource.Drawable.ic_transfer);
            mTransferDeposit.Click += (sender, e) => StartActivityForResult(typeof(POS_SettingPanel_TransferDeposit), 21);

            var mChangePIN = new MenuIcon(this, "Ganti PIN", Resource.Drawable.ic_key);
            mChangePIN.Click += (sender, e) => StartActivityForResult(typeof(POS_SettingPanel_ChangePIN), 22);

            var mRegisterAgen = new MenuIcon(this, "Daftar Agen", Resource.Drawable.ic_register);
            mRegisterAgen.Click += (sender, e) => StartActivityForResult(typeof(POS_SettingPanel_RegisterAgen), 23);

            var mPrivateChat = new MenuIcon(this, "Chat Manual", Resource.Drawable.appdefault);
            mPrivateChat.Click += (sender, e) => StartChat(ContactPOS.Current);

            var menus = CreateMenuPopup(
                new MenuIconCollection(this, mTicketDeposit, mTransferDeposit, mChangePIN, mRegisterAgen),
                new MenuIconCollection(this, mPrivateChat));
            var content = FindViewById<ScrollView>(Resource.Id.MPOS_Content);
            content.AddView(menus);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;

            var contactPOS = ContactPOS.Current;
            switch (requestCode)
            {
                case 20:
                    contactPOS.ShowProgress(null, 18000, "Hasil tidak diketahui");
                    contactPOS.RequestTicket(data.GetStringExtra("value"), data.GetStringExtra("pin"));
                    break;
                case 21:
                    contactPOS.ShowProgress(null, 18000, "Hasil tidak diketahui");
                    contactPOS.TransferDep(data.GetStringExtra("target"), data.GetStringExtra("value"), data.GetStringExtra("pin"));
                    break;
                case 22:
                    contactPOS.ShowProgress(null, 18000, "Hasil tidak diketahui");
                    contactPOS.ChangePin(data.GetStringExtra("new"), data.GetStringExtra("pin"));
                    break;
                case 23:
                    contactPOS.ShowProgress(null, 18000, "Hasil tidak diketahui");
                    contactPOS.RegisterDownline(data.GetStringExtra("name"), data.GetStringExtra("hp"), data.GetStringExtra("addr"), data.GetStringExtra("postCode"), data.GetStringExtra("pin"));
                    break;
            }
        }
    }
}