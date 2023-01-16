#if BUILD_PARTNER
using System;
using Android.Views;
using Android.Widget;
using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    public class TrxHistoryAdapter : IListAdapter<TrxData>
    {
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.POS_TrxTableRow, null);
                convertView.FindViewById<TextView>(Resource.Id.lbTrxCekStatus).Click += Check_Click;
            }
            Update(convertView, this.Items[position]);
            return convertView;
        }

        private static void Check_Click(object sender, EventArgs e)
        {
            long trxId = (long)((TextView)sender).Tag;
            if (trxId <= 0)
                return;

            var contactPOS = ContactPOS.Current;
            var trx = contactPOS.GetHistory().FindTrxById(trxId);
            if (trx == null)
                return;

            if (trx.EnableCheckStatus)
            {
                contactPOS.ShowProgress(null, 9000, null);
                contactPOS.CheckStatus(trx);
            }
            else if (trx.IsStrukAvailable)
                POS_Struk.Show(trxId);
        }

        private static void Update(View view, TrxData Trx)
        {
            view.FindViewById<EditText>(Resource.Id.tbTrxDate).Text = Trx.GetDate();
            var dest = view.FindViewById<EditText>(Resource.Id.tbTrxDest);
            if (string.IsNullOrEmpty(Trx.Destination))
                dest.Visibility = ViewStates.Gone;
            else
            {
                dest.Text = Trx.Destination;
                dest.Visibility = ViewStates.Visible;
            }
            view.FindViewById<EditText>(Resource.Id.tbTrxDesc).Text = Trx.Description;
            var price = view.FindViewById<EditText>(Resource.Id.tbTrxPrice);
            if (string.IsNullOrEmpty(Trx.Price) || Trx.Price == "0")
            {
                if (!string.IsNullOrEmpty(Trx.Destination))
                {
                    price.Text = null; // Destination is visible, do not hide price, just null it
                    price.Visibility = ViewStates.Visible;
                }
                else
                    price.Visibility = ViewStates.Gone;
            }
            else
            {
                price.Text = "Rp " + Trx.Price;
                price.Visibility = ViewStates.Visible;
            }
            view.FindViewById<TextView>(Resource.Id.lbTrxStatus).Text = Trx.GetStatus();

            var check = view.FindViewById<TextView>(Resource.Id.lbTrxCekStatus);
            string action = null;
            if (Trx.EnableCheckStatus)
                action = "Cek Status";
            else if (Trx.IsStrukAvailable)
                action = "Cek Struk";

            if ((object)action == null)
            {
                check.Visibility = ViewStates.Invisible;
                check.Tag = (long)0;
            }
            else
            {
                check.Text = action;
                check.Visibility = ViewStates.Visible;
                check.Tag = Trx.TrxId;
            }
        }
    }
}
#endif