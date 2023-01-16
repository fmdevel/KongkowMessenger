using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Graphics;

namespace fmm
{
    public class TabSlider : LinearLayout
    {
        private View m_slider;
        private LayoutParams m_params;
        public TabSlider(Context context) : base(context)
        {
            this.Orientation = Orientation.Horizontal;
            m_slider = new View(this.Context);
            m_params = new LayoutParams(ViewGroup.LayoutParams.WrapContent, UIUtil.DpToPx(3.5f));
            m_params.BottomMargin = 1;
            this.AddView(m_slider, m_params);
        }

        public void SetColor(Color color)
        {
            m_slider.SetBackgroundColor(color);
        }

        public void SetWidth(int width)
        {
            m_params.Width = width;
            m_slider.LayoutParameters = m_params;
        }

        public void SetPosition(int position)
        {
            m_params.LeftMargin = position;
            m_slider.LayoutParameters = m_params;
        }
    }
}