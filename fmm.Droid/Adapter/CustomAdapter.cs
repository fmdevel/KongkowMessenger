#if BUILD_PARTNER
using System;
using Android.Graphics;
using Android.Widget;
using Android.Views;
using ChatAPI.Connector;
using Android.Content;

namespace fmm
{
    public abstract class CustomAdapter<T> : SpinnerAdapter<T>
    {
        public Android.App.AlertDialog Dialog;
        protected abstract void CreateView(LinearLayout view, T item, Android.Content.Context context);
        protected abstract void UpdateView(LinearLayout view, T item);
        protected abstract void OnClick(T item);
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = this.Items[position];
            var view = convertView as LinearLayout;
            if (view == null)
            {
                view = new LinearLayout(parent.Context);
                view.Orientation = Orientation.Horizontal;
                view.SetGravity(GravityFlags.CenterVertical);
                view.SetBackgroundResource(Resource.Drawable.custom_list_bk);
                view.Clickable = true;
                view.Click += View_Click;
                CreateView(view, item, parent.Context);
            }
            else
                UpdateView(view, item);

            view.Tag = position;
            return view;
        }

        private void View_Click(object sender, EventArgs e)
        {
            if (Dialog != null) Dialog.Dismiss();
            OnClick(this.Items[(int)((View)sender).Tag]);
        }
    }

    public class CustomProductAdapter : CustomAdapter<Product>
    {
        protected override void CreateView(LinearLayout view, Product item, Context context)
        {
            var pad = UIUtil.DpToPx(8);
            var desc = new TextView(context);
            desc.SetSingleLine(false);
            desc.SetTextColor(Color.Black);
            desc.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
            desc.Text = item.Desc;

            var code = new TextView(context);
            code.SetTextColor(Color.DarkGreen);
            code.SetTextSize(Android.Util.ComplexUnitType.Dip, 14);
            code.Text = item.Code;

            var llLeft = new LinearLayout(context);
            llLeft.Orientation = Orientation.Vertical;
            llLeft.SetPadding(pad, pad, pad, pad);
            llLeft.AddView(desc, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            llLeft.AddView(code, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

            var price = new TextView(context);
            price.SetPadding(pad, pad, UIUtil.DpToPx(22), pad);
            price.SetMinWidth(UIUtil.DpToPx(110));
            price.SetTextColor(Color.DarkGray);
            price.SetTextSize(Android.Util.ComplexUnitType.Dip, 15);
            price.Text = item.Price <= 0 ? null : ChatAPI.Util.Rp(item.Price);

            view.AddView(llLeft, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, 1));
            view.AddView(price);
        }
        protected override void UpdateView(LinearLayout view, Product item)
        {
            var llLeft = (LinearLayout)view.GetChildAt(0);
            ((TextView)llLeft.GetChildAt(0)).Text = item.Desc;
            ((TextView)llLeft.GetChildAt(1)).Text = item.Code;
            ((TextView)view.GetChildAt(1)).Text = item.Price <= 0 ? null : ChatAPI.Util.Rp(item.Price);
        }
        protected override void OnClick(Product item)
        {
            POS_CustomListProduct.SelectedProduct = item;
            Activity.CurrentActivity.StartActivity(typeof(POS_CustomTrx));
        }
    }


    public class CustomProviderAdapter : CustomAdapter<Provider>
    {
        protected override void CreateView(LinearLayout view, Provider item, Context context)
        {
            var pad = UIUtil.DpToPx(8);
            var logo = new ImageView(context);
            int logoSize = UIUtil.DpToPx(38);
            logo.SetScaleType(ImageView.ScaleType.FitXy);
            var par = new ViewGroup.MarginLayoutParams(logoSize, logoSize);
            par.MarginStart = pad;
            logo.LayoutParameters = par;
            SetRes(logo, item);

            var desc = new TextView(context);
            desc.SetPadding(pad, 0, UIUtil.DpToPx(22), 0);
            desc.SetSingleLine(false);
            desc.SetTextColor(Color.Black);
            desc.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
            desc.Text = item.Name;

            view.AddView(logo);
            view.AddView(desc, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        }

        protected override void UpdateView(LinearLayout view, Provider item)
        {
            SetRes((ImageView)view.GetChildAt(0), item);
            ((TextView)view.GetChildAt(1)).Text = item.Name;
        }
        protected override void OnClick(Provider item)
        {
            POS_CustomListProduct.Start(item.Name, item.ListProduct);
        }

        private void SetRes(ImageView logo, Provider item)
        {
            int resId = POS_CustomListProduct.GetRes(item.Name);
            if (resId == 0)
                logo.SetImageDrawable(null);
            else
                logo.SetImageResource(resId);
        }
    }

    public class CustomSimpleProductAdapter : CustomAdapter<Product>
    {
        public TypeProduct TypeProduct;
        protected override void CreateView(LinearLayout view, Product item, Context context)
        {
            var pad = UIUtil.DpToPx(8);
            var desc = new TextView(context);
            desc.SetPadding(pad, pad, UIUtil.DpToPx(22), pad);
            desc.SetSingleLine(false);
            desc.SetTextColor(Color.Black);
            desc.SetTextSize(Android.Util.ComplexUnitType.Dip, 16);
            desc.Text = item.Desc;
            view.AddView(desc, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        }

        protected override void UpdateView(LinearLayout view, Product item)
        {
            ((TextView)view.GetChildAt(0)).Text = item.Desc;
        }
        protected override void OnClick(Product item)
        {
            POS_CustomListProduct.TypeProduct = TypeProduct;
            POS_CustomListProduct.SelectedProduct = item;
            POS_CustomListProduct.SelectedResId = POS_CustomListProduct.GetRes(item.Provider);
            POS_CustomListProduct.Destination = null;          
            Activity.CurrentActivity.StartActivity(typeof(POS_CustomTrx));
        }
    }
}
#endif