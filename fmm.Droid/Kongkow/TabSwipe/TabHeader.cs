using System;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace fmm
{
    public class TabHeader : LinearLayout
    {
        public readonly ImageView[] Icons;
        public readonly TabSlider Slider;
        public TabHeader(Context context, ImageView[] icons, TabSlider slider) : base(context)
        {
            this.Icons = icons;
            this.Slider = slider;
            this.Orientation = Orientation.Vertical;
            var panelIcons = new LinearLayout(this.Context);
            panelIcons.Orientation = Orientation.Horizontal;
            var dp6 = UIUtil.DpToPx(6);
            var dp40 = UIUtil.DpToPx(40);
            foreach (ImageView icon in icons)
            {
                icon.SetScaleType(ImageView.ScaleType.FitCenter);
                icon.SetPadding(dp6, dp6, dp6, dp6);
                panelIcons.AddView(icon, new LayoutParams(ViewGroup.LayoutParams.WrapContent, dp40, 1.0f));
            }

            var dropShadow = new LinearLayout(Context);
            dropShadow.SetBackgroundResource(Resource.Drawable.drop_shadow);
            this.AddView(dropShadow, ViewGroup.LayoutParams.MatchParent, UIUtil.DpToPx(2));
            this.AddView(slider, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            this.AddView(panelIcons, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            slider.SetColor(Android.Graphics.Color.White);
        }

        public TabHeader(Context context, ImageView[] icons) : this(context, icons, new TabSlider(context)) { }
    }
}