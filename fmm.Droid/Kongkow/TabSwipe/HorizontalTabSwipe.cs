using System;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace fmm
{
    public partial class HorizontalTabSwipe : RelativeLayout
    {
        public delegate View CreateTab(int index);
        public CreateTab OnCreateTab;
        public Action<int> OnTabCreated;
        public Action<int> OnSelectedIndexChanged;

        public readonly TabHeader Header;
        private HScrollView m_hScrollView;
        public HorizontalTabSwipe(Context context, TabHeader header, int selectedIndex) : base(context)
        {
            this.Header = header;
            var par = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            par.AddRule(LayoutRules.AlignParentBottom);
            m_hScrollView = new HScrollView(this, selectedIndex);
            int hId = View.GenerateViewId();
            header.Id = hId;
            this.AddView(header, par);
            par = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            par.AddRule(LayoutRules.Above, hId);
            this.AddView(m_hScrollView, par);

            foreach (ImageView icon in header.Icons)
                icon.Click += HeaderClick;
        }

        private void HeaderClick(object sender, EventArgs e)
        {
            var index = Array.IndexOf(Header.Icons, sender as ImageView);
            if (index >= 0 && index != m_hScrollView.SelectedIndex)
                SelectTab(index, true);
        }

        public int SelectedIndex
        {
            get
            {
                return m_hScrollView.SelectedIndex;
            }
        }

        //public void SelectTab(int index)
        //{
        //    SelectTab(index, true);
        //}
        public void SelectTab(int index, bool smooth)
        {
            m_hScrollView.SelectTab(index, smooth);
        }

        public View GetContent(int index)
        {
            var tab = m_hScrollView.Tabs[index];
            if (tab == null || tab.ChildCount == 0)
                return null;
            return tab.GetChildAt(0);
        }
    }
}