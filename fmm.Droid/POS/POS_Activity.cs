using System;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    public abstract class POS_Activity : Activity
    {
        protected override void OnResume()
        {
            base.OnResume();
            ContactPOS.Current.RestoreProgress();
        }

        protected virtual void Initialize(int layoutResId, string title)
        {
            SetContentView(layoutResId);
            ThemedResId = new[] { Resource.Id.ActivityHeader };
            FindViewById<TextView>(Resource.Id.Title).Text = title;
            FindViewById(Resource.Id.ActivityHeader).Click += (sender, e) => { Finish(); };
        }

        internal static void Initialize()
        {
            ContactPOS.OnAgenInfo = OnAgenInfo;
            ContactPOS.OnRegister = OnRegister;
            ContactPOS.OnGetProduk = OnGetProduct;
            ContactPOS.OnTrxSent = OnSetTrxSent;
            ContactPOS.OnTrx = OnTrx;
            ContactPOS.OnCheckBill = OnCheckBill;
            ContactPOS.OnChangePin = OnChangePin;
            ContactPOS.OnAdminInfo = OnAdminInfo;
        }

        public static POS_Activity Get(Contact contact)
        {
            return ContactPOS.Current == contact ? CurrentActivity as POS_Activity : null;
        }

        #region " POS Events "

        private static void OnAgenInfo(ContactPOS contact)
        {
            var info = contact.AgenInfo;
            if (info == null)
                return;

            var msg = info.AdditionalMessage;
            if (!string.IsNullOrEmpty(msg))
                contact.CloseProgress();

            if (CurrentActivity == null)
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    info.AdditionalMessage = null;
                    LocalNotification.NotifyPosInfo(contact, msg);
                }
                return;
            }

            CurrentActivity.RunOnUiThread(() =>
            {
                if (contact == ContactPOS.Current)
                {
                    var main = CurrentActivity as MainActivity;
                    if (main != null && main.ActiveTabIndex == 3)
                    {
                        main.Pos_SetInfo();
                        return;
                    }
                    var a = CurrentActivity as POS_Info;
                    if (a != null)
                    {
                        a.SetInfo();
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    info.AdditionalMessage = null;
                    CurrentActivity.PopupInfo(msg); // UI
                }
            });
        }

        private static void OnRegister(ContactPOS contact, int result, string message)
        {
            contact.CloseProgress();
            var a = CurrentActivity;
            if (a == null)
                return;

            a.RunOnUiThread(() =>
            {
                switch (result)
                {
                    case 0:
                        CurrentActivity.PopupInfo("Selamat! Anda telah terdaftar di " + contact.Name + "\n\nPIN Transaksi Anda: " + message,
                            () =>
                            {
                                var r = CurrentActivity as POS_Register;
                                if (r != null)
                                    r.Finish();
                            });
                        return;
                    case 2:
                        CurrentActivity.PopupInfo("Tidak terlaksana, mohon coba lagi kemudian");
                        return;
                    case 3:
                        CurrentActivity.PopupInfo("Gagal, PIN tidak aman atau mudah ditebak, mohon coba lagi");
                        return;
                    case 4:
                        CurrentActivity.PopupInfo(message);
                        return;
                }
            });
        }

        private static void OnGetProduct(ContactPOS contact, Product[] products)
        {
            if (products == null)
                return;

            TypeProduct.Populate(contact, products);

            var a = Get(contact);
            if (a == null)
                return;

            var aTrx = a as POS_Trx;
            if (aTrx != null)
            {
                aTrx.RunOnUiThread(() =>
                {
                    aTrx.SetProduct(contact);
                });
            }
        }

        private static void OnSetTrxSent(ContactPOS contact)
        {
            var trx = contact.ProgressState as TrxData;
            if (trx != null)
            {
                contact.ProgressState = null;
                var h = contact.GetHistory();
                if (h.FindTrxIndexById(trx.TrxId) < 0) // Is trx already exist
                {
                    h.Add(trx);
                    UpdateHistory(contact);
                }
            }
        }

        private static void OnTrx(ContactPOS contact, TrxResult result)
        {
            if (result == null)
            {
                contact.CloseProgress();
                LocalNotification.NotifyPosInfo(contact, "Tidak terlaksana, mohon coba lagi kemudian");
                return;
            }

            if (result.TrxId != 0)
                contact.CloseProgress();

            var h = contact.GetHistory();
            TrxData trx;
            if (result.TrxId == 0) // Message from CS or Add Deposit Message
            {
                if (string.IsNullOrEmpty(result.Description))
                    return; // No info? are you kidding me?

                trx = new TrxData(DateTime.Now);
                trx.TrxId = Util.UniqeId;
                trx.Description = result.Description;
                h.Add(trx);
                UpdateHistory(contact);
            }
            else
            {
                trx = h.FindTrxById(result.TrxId);
                if (trx == null)
                    return;

                h.Update(result);
                UpdateHistory(contact);
            }

            if (trx.IsStrukAvailable)
                LocalNotification.NotifyPosStruk(contact, trx);
            else
                LocalNotification.NotifyPosInfo(contact, result.Description);
        }

        private static void UpdateHistory(ContactPOS contact)
        {
            var a_history = CurrentActivity as POS_History;
            if (a_history != null)
            {
                a_history.RunOnUiThread(a_history.DisplayData);
                return;
            }
            contact.TrxHistoryRefresh = true;
            contact.GetAgenInfo(true);
        }

        private static void OnAdminInfo(ContactPOS contact, string result)
        {
            contact.CloseProgress();
            if (string.IsNullOrEmpty(result))
            {
                LocalNotification.NotifyPosInfo(contact, "Tidak terlaksana, mohon coba lagi kemudian");
                return;
            }

            if (result != "PIN salah")
            {
                var trx = new TrxData(DateTime.Now);
                trx.TrxId = Util.UniqeId;
                trx.Description = result;
                contact.GetHistory().Add(trx);
                UpdateHistory(contact);
            }
            LocalNotification.NotifyPosInfo(contact, result);
        }

        private static void OnChangePin(ContactPOS contact, int result)
        {
            contact.CloseProgress();
            var a = CurrentActivity;
            if (a == null)
                return;

            a.RunOnUiThread(() =>
            {
                switch (result)
                {
                    case 0:
                        CurrentActivity.PopupInfo("Penggantian PIN berhasil");
                        return;
                    case 1:
                        CurrentActivity.PopupInfo("Gagal, PIN Lama salah");
                        return;
                    case 3:
                        CurrentActivity.PopupInfo("Gagal, PIN tidak aman atau mudah ditebak, mohon coba lagi");
                        return;
                }
                CurrentActivity.PopupInfo("Penggantian PIN Gagal, mohon coba lagi kemudian");
            });
        }

        private static void OnCheckBill(ContactPOS contact, TrxResult result)
        {
            if (ContactPOS.Current != contact)
                return;

            Activity a = CurrentActivity as POS_CustomTrx;
            if (a == null) a = CurrentActivity as POS_TrxPostpaid;

            if (a == null)
                return;

            if (result.TrxId != contact.InqData.TrxId)
                return; // DELAYED RESPONSE

            contact.CloseProgress();
            if (result == null)
            {
                a.RunOnUiThread(() => CurrentActivity.PopupOK(contact.Name, "Cek Tidak terlaksana, mohon coba lagi kemudian"));
                return;
            }

            var content = result.HtmlStruk;
            if (string.IsNullOrEmpty(content))
            {
                content = result.Description;
                if (string.IsNullOrEmpty(content))
                    return;
            }

            if (result.TrxStatus == 2)
            {
                CurrentActivity.RunOnUiThread(() => CurrentActivity.PopupOK(contact.Name, content));
                return;
            }

            a.RunOnUiThread(() =>
            {
                var intent = new Android.Content.Intent(a, typeof(POS_CheckBill));
                intent.PutExtra("html", content);
                a.StartActivityForResult(intent, 14);
            });
        }
#endregion
    }
}