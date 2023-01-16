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
    public class POS_History : POS_Activity
    {
        private TrxHistoryAdapter m_trxHistoryAdapter;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TryAssignCurrentContact();
            base.OnCreate(savedInstanceState);
            Initialize(Resource.Layout.POS_TrxHistory, "Data Transaksi");

            var contactPOS = ContactPOS.Current;
            m_trxHistoryAdapter = new TrxHistoryAdapter();
            //m_trxHistoryAdapter.Items = contactPOS.TrxHistory_List;
            FindViewById<ListView>(Resource.Id.ListViewer).Adapter = m_trxHistoryAdapter;

            var lbDate = FindViewById<TextView>(Resource.Id.lbDate);
            lbDate.Click += LbDate_Click;
            lbDate.Text = contactPOS.TrxHistory_Date.ToString("dd MMM yyyy");

            FindViewById<ImageView>(Resource.Id.btnSearchByDate).Click += BtnSearchByDate_Click;
            FindViewById<ImageView>(Resource.Id.btnSearch).Click += BtnSearch_Click;

            var intent = this.Intent;
            if (intent != null)
            {
                var info = intent.GetStringExtra("Info");
                if (!string.IsNullOrEmpty(info))
                {
                    intent.RemoveExtra("Info");
                    PopupOK(contactPOS.Name, info);
                }
                else
                {
                    var trxId = intent.GetLongExtra("trxId", 0);
                    if (trxId != 0)
                    {
                        intent.RemoveExtra("trxId");
                        POS_Struk.Show(trxId);
                    }
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (ContactPOS.Current.TrxHistoryRefresh)
                DisplayData();
        }

        private void TryAssignCurrentContact()
        {
            var intent = this.Intent;
            if (intent != null)
            {
                int id = intent.GetIntExtra("NotifID", 0);
                if (id >= 11) // POS Notif
                {
                    intent.RemoveExtra("NotifID");
                    LocalNotification.ClearNotification(id);      
                    //var contactId = intent.GetStringExtra("Contact");
                    //if (!string.IsNullOrEmpty(contactId))
                    //{
                    //    var contactPOS = Core.FindContact(contactId) as ContactPOS;
                    //    if (contactPOS != null)
                    //        CurrentContact = contactPOS;
                    //}
                }
            }
        }

        private void LbDate_Click(object sender, EventArgs e)
        {
            POS_Dialog.DatePicker(DateChanged, ContactPOS.Current.TrxHistory_Date);
        }

        private void DateChanged(DateTime date)
        {
            ContactPOS.Current.TrxHistory_Date = date;
            FindViewById<TextView>(Resource.Id.lbDate).Text = date.ToString("dd MMM yyyy");
            DisplayData();
        }

        private void BtnSearchByDate_Click(object sender, EventArgs e)
        {
            DisplayData();
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            DisplayData(ContactPOS.Current.GetHistory().FindTrxByAny(FindViewById<EditText>(Resource.Id.tbSearch).Text));
        }

        internal void DisplayData()
        {
            DisplayData(ContactPOS.Current.GetHistory().FindTrxByDate(ContactPOS.Current.TrxHistory_Date));
        }

        private void DisplayData(List<TrxData> list)
        {
            var contactPOS = ContactPOS.Current;
            contactPOS.TrxHistoryRefresh = false;
            if (list == null || list.Count == 0)
            {
                m_trxHistoryAdapter.Items = TrxHistoryAdapter.Empty;
                m_trxHistoryAdapter.NotifyDataSetChanged();
            }
            else
            {
                list.Reverse();
                m_trxHistoryAdapter.Items = list;
                m_trxHistoryAdapter.NotifyDataSetChanged();
            }
        }
    }
}