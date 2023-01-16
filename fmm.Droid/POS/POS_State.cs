using System;
using System.Collections.Generic;
using System.Timers;

using Android.Views;
using Android.Widget;
using ChatAPI.Connector;

namespace ChatAPI
{
    public partial class ContactPOS
    {
        //public Type LastTab = typeof(fmm.POS_Info);
        public ChatMessage CacheRecentStatus;
        public InqData InqData;

        #region " Progress "

        internal object ProgressState;
        private Timer m_timer;
        private string m_timeOutInfo;
        private LinearLayout m_container;

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            if (PosQueue != null && Network.RemoveQueue(PosQueue))
            {
                PosQueue = null;
                return;
            }

            var a = fmm.POS_Activity.Get(this);
            if (a != null)
                a.RunOnUiThread(RemoveProgressWithInfo);
        }

        internal void ShowProgress(object state, int timeOut, string timeOutInfo)
        {
            ProgressState = state;
            if (m_timer == null)
            {
                m_timer = new Timer();
                m_timer.AutoReset = false;
                m_timer.Elapsed += OnElapsed;
            }
            AddProgress();
            if (!m_timer.Enabled)
            {
                m_timeOutInfo = timeOutInfo;
                if (timeOut > 0)
                {
                    timeOut += 200;
                    m_timer.Interval = timeOut;
                    m_timer.Enabled = true;
                }
            }
        }

        private void AddProgress()
        {
            if (m_container == null)
            {
                m_container = new LinearLayout(fmm.Activity.CurrentActivity);
                m_container.Orientation = Orientation.Vertical;
                m_container.SetGravity(GravityFlags.Center);
                m_container.SetBackgroundColor(Android.Graphics.Color.White);

                var progress = new ProgressBar(fmm.Activity.CurrentActivity, null, Android.Resource.Attribute.ProgressBarStyleLarge);
                m_container.AddView(progress, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

                var progressText = new TextView(fmm.Activity.CurrentActivity);
                progressText.Text = "Mohon tunggu ..";
                m_container.AddView(progressText, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            }

            var page = fmm.Activity.CurrentActivity.FindViewById<ViewGroup>(fmm.Resource.Id.MPOS_Page);
            var parent = m_container.Parent as ViewGroup;
            if (parent != page)
            {
                if (parent != null)
                    parent.RemoveView(m_container);

                //fmm.Activity.CurrentActivity.FindViewById(fmm.Resource.Id.MPOS_Header).Visibility = ViewStates.Gone;
                fmm.Activity.CurrentActivity.FindViewById(fmm.Resource.Id.MPOS_Content).Visibility = ViewStates.Gone;

                page.AddView(m_container, ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.FillParent);
            }
        }

        private void RemoveProgress()
        {
            if (m_container == null)
                return;

            var parent = m_container.Parent as ViewGroup;
            if (parent != null)
                parent.RemoveView(m_container);

            var a = fmm.POS_Activity.Get(this);
            if (a != null)
            {
                // a.FindViewById(fmm.Resource.Id.MPOS_Header).Visibility = ViewStates.Visible;
                a.FindViewById(fmm.Resource.Id.MPOS_Content).Visibility = ViewStates.Visible;
            }
        }

        private void RemoveProgressWithInfo()
        {
            RemoveProgress();
            if (!string.IsNullOrEmpty(m_timeOutInfo))
                fmm.Activity.CurrentActivity.PopupOK(this.Name, m_timeOutInfo);
        }

        internal void CloseProgress()
        {
            if (m_timer == null)
                return;

            if (m_timer.Enabled)
                m_timer.Stop();

            if (fmm.Activity.CurrentActivity != null)
                fmm.Activity.CurrentActivity.RunOnUiThread(RemoveProgress);
        }

        internal void RestoreProgress()
        {
            if (m_timer == null)
                return;

            if (m_timer.Enabled)
                AddProgress();
            else
                RemoveProgress();
        }
        #endregion

        #region " History Trx "

        public DateTime TrxHistory_Date = DateTime.Now;
        //public List<TrxData> TrxHistory_List = new List<TrxData>();
        public bool TrxHistoryRefresh = true;
        #endregion

        private void OnTrxPIN(string password, object[] args)
        {
            ShowProgress(args[0], 18000, "Transaksi sedang dalam proses, silahkan tunggu atau Cek Status melalui Data Trx");
            Trx((TrxData)args[0], (string)args[1], password, (string)args[2]);
        }

        internal void SubmitTrx(Product product, string period, string destination, string hpCustomer, long trxId = 0)
        {
            var type = TypeProduct.GetType(product.Category);
            string desc = null;
            if (type != null)
            {
                if (product.Category <= 100)
                {
                    desc = "ANDA AKAN MEMBELI\n" + product.Desc + "\n\n" + type.DestinationText + "\n" + destination + "\n\nHARGA RP " + product.Price.ToString();
                }
                else
                {
                    desc = "ANDA AKAN MEMBAYAR\n" + product.Desc + "\n\n" + type.DestinationText + "\n" + destination;
                }
            }
            var trx = new TrxData(DateTime.Now);
            trx.TrxId = trxId == 0 ? Util.UniqeId : trxId;
            trx.Destination = destination;
            trx.Price = product.Price.ToString();
            trx.TrxStatus = 1; // Pending
            string productCode = product.Category == 301 ? "Transfer Bank" : product.Code;
            fmm.POS_Dialog.PIN(desc, OnTrxPIN, trx, (string.IsNullOrEmpty(period) ? productCode : productCode + "." + period), hpCustomer);
        }

        internal void SubmitTrxCustom(Product product, string period, string destination, string hpCustomer, long trxId, string password)
        {
            var trx = new TrxData(DateTime.Now);
            trx.TrxId = trxId == 0 ? Util.UniqeId : trxId;
            trx.Destination = destination;
            trx.Price = product.Price.ToString();
            trx.TrxStatus = 1; // Pending
            string productCode = product.Category == 301 ? "Transfer Bank" : product.Code;
            object[] args = new object[] { trx, (string.IsNullOrEmpty(period) ? productCode : productCode + "." + period), hpCustomer };
            OnTrxPIN(password, args);
        }
    }
}