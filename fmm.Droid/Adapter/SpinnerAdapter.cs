using System;
using Android.Views;
using Android.Widget;
using Android.Graphics;

namespace fmm
{
    public class SpinnerAdapter<T> : IListAdapter<T>
    {
        public SpinnerAdapter() { }

        public SpinnerAdapter(T[] items)
        {
            base.Items = items;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var text = this.Items[position].ToString();
            if (convertView == null)
            {
                var textView = new TextView(parent.Context);
                var pad = UIUtil.DpToPx(6);
                pad = pad * 3 / 2;
                textView.SetPadding(pad, pad, pad, pad);
                // textView.SetPadding(pad, pad * 2, pad, pad * 2);
                textView.SetSingleLine(false);
                textView.SetTextColor(Color.Black);
                textView.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
                textView.Text = text;
                return textView;
            }
            ((TextView)convertView).Text = text;
            return convertView;
        }
    }
}