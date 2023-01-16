#if BUILD_PARTNER
using System;
using Android.Graphics;
using Android.Widget;
using Android.Views;
using ChatAPI.Connector;

namespace fmm
{
    public class ProductAdapter : SpinnerAdapter<Product>
    {
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var p = this.Items[position];
            var view = convertView as LinearLayout;
            LinearLayout llBottom;
            if (view == null)
            {
                var desc = new TextView(parent.Context);
                var pad = UIUtil.DpToPx(6);
                desc.SetPadding(pad, pad * 5 / 4, pad, 0);
                desc.SetSingleLine(false);
                desc.SetTextColor(Color.Black);
                desc.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
                desc.Text = p.Desc;

                view = new LinearLayout(parent.Context);
                view.Orientation = Orientation.Vertical;
                view.SetBackgroundColor(Color.White);
                view.AddView(desc, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

                llBottom = new LinearLayout(parent.Context);
                llBottom.Orientation = Orientation.Horizontal;
                var price = new TextView(parent.Context);
                price.SetPadding(pad, 0, pad, pad * 5 / 4);
                price.SetTextColor(Color.DarkGreen);
                price.SetTextSize(Android.Util.ComplexUnitType.Dip, 14);
                price.SetTypeface(price.Typeface, TypefaceStyle.Bold);
                price.Text = p.Price.ToString("Rp 0");
                llBottom.AddView(price, UIUtil.DpToPx(100), ViewGroup.LayoutParams.WrapContent);
                var code = new TextView(parent.Context);
                code.SetPadding(pad, 0, pad, pad * 5 / 4);
                code.SetTextColor(Color.DarkOrange);
                code.SetTextSize(Android.Util.ComplexUnitType.Dip, 14);
                code.Text = p.Code;
                llBottom.AddView(code, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                view.AddView(llBottom);
            }
            else
            {
                ((TextView)view.GetChildAt(0)).Text = p.Desc;
                llBottom = (LinearLayout)view.GetChildAt(1);
                ((TextView)llBottom.GetChildAt(0)).Text = p.Price.ToString("Rp 0");
                ((TextView)llBottom.GetChildAt(1)).Text = p.Code;
            }
            return view;
        }
    }
}
#endif