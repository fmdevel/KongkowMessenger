using System;
using Android.Views;
using Android.Widget;
using Android.Graphics;

namespace fmm
{
    public partial class HorizontalTabSwipe
    {
        private class HScrollView : HorizontalScrollView
        {
            private readonly HorizontalTabSwipe m_parent;
            public readonly LinearLayout[] Tabs;
            public int SelectedIndex;
            private int m_measuredWidth;
            private int m_initialIndex;
            private bool m_pendingLayout;

            public HScrollView(HorizontalTabSwipe parent, int selectedIndex) : base(parent.Context)
            {
                m_parent = parent;
                Tabs = new LinearLayout[parent.Header.Icons.Length];
                m_initialIndex = Math.Max(selectedIndex, 0);
                SelectedIndex = -1;
                HorizontalScrollBarEnabled = false;
            }

            private void EnsureTabCreated(int index)
            {
                if (index < 0)
                    return;

                var tab = Tabs[index];
                if (tab.ChildCount == 0)
                {
                    tab.AddView(m_parent.OnCreateTab(index), ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                    if (m_parent.OnTabCreated != null)
                        m_parent.OnTabCreated(index);
                }
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
                var oldMeasuredWidth = m_measuredWidth;
                var measuredWidth = this.MeasuredWidth;
                m_measuredWidth = measuredWidth;
                if (this.ChildCount == 0)
                {
                    var internalView = new LinearLayout(this.Context);
                    internalView.Orientation = Orientation.Horizontal;
                    for (int i = 0; i < Tabs.Length; i++)
                    {
                        var l = new LinearLayout(this.Context);
                        Tabs[i] = l;
                        l.Orientation = Orientation.Vertical;
                        internalView.AddView(l, measuredWidth, ViewGroup.LayoutParams.MatchParent);
                    }
                    this.AddView(internalView, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.MatchParent);
                    m_pendingLayout = true;
                }
                else if (measuredWidth != oldMeasuredWidth)
                {
                    for (int i = 0; i < Tabs.Length; i++)
                        Tabs[i].LayoutParameters = new LinearLayout.LayoutParams(measuredWidth, ViewGroup.LayoutParams.MatchParent);
                    m_pendingLayout = true;
                }
            }

            protected override void OnDraw(Canvas canvas)
            {
                if (m_pendingLayout)
                {
                    m_pendingLayout = false;
                    bool triggerEvent = (SelectedIndex < 0);
                    if (triggerEvent)
                        SelectedIndex = m_initialIndex;
                    EnsureTabCreated(SelectedIndex);
                    int snapX = SelectedIndex * m_measuredWidth;
                    ScrollTo(snapX, 0); // scroll immediately!!
                    m_parent.Header.Slider.SetWidth(m_measuredWidth / Tabs.Length);
                    SetSliderPosition(snapX);
                    if (triggerEvent)
                        RaiseSelectedIndexChanged();

                    this.Invalidate();
                    return;
                }
                base.OnDraw(canvas);
            }

            private void SetSliderPosition(int position)
            {
                m_parent.Header.Slider.SetPosition(position / Tabs.Length);
            }

            public void SelectTab(int index)
            {
                SelectTab(index, true);
            }
            public void SelectTab(int index, bool smooth)
            {
                bool triggerEvent = (SelectedIndex != index);
                SelectedIndex = index;
                EnsureTabCreated(index);
                int snapX = index * m_measuredWidth;
                if (smooth)
                    SmoothScrollTo(snapX, 0); // use Smooth Scroll
                else
                    ScrollTo(snapX, 0);

                SetSliderPosition(snapX);
                if (triggerEvent)
                    RaiseSelectedIndexChanged();
            }

            private void RaiseSelectedIndexChanged()
            {
                if (m_parent.OnSelectedIndexChanged != null)
                    m_parent.OnSelectedIndexChanged(SelectedIndex);
            }

            public override bool OnTouchEvent(MotionEvent e)
            {
                if (m_measuredWidth != 0)
                {
                    int index = SelectedIndex;
                    int snapX = index * m_measuredWidth;
                    int sx = ScrollX;

                    if (e.Action == MotionEventActions.Up || e.Action == MotionEventActions.Cancel)
                    {
                        if (Math.Abs(sx - snapX) * 8 > m_measuredWidth) // 12.5% movement of screen
                        {
                            var dt = e.EventTime - e.DownTime;
                            if (dt < 500)
                            {
                                if (sx < snapX && index > 0)
                                {
                                    SelectTab(index - 1);
                                    return true;
                                }
                                else if (index + 1 < Tabs.Length)
                                {
                                    SelectTab(index + 1);
                                    return true;
                                }

                            }
                        }

                        SelectTab((ScrollX + (m_measuredWidth / 2)) / m_measuredWidth);
                        return true;
                    }
                    else if (e.Action == MotionEventActions.Move)
                    {
                        if (sx > snapX && index + 1 < Tabs.Length)
                        {
                            EnsureTabCreated(index + 1);
                            SetSliderPosition(sx);
                        }
                        else if (sx < snapX && index > 0)
                        {
                            EnsureTabCreated(index - 1);
                            SetSliderPosition(sx);
                        }
                    }
                }
                return base.OnTouchEvent(e);
            }
        }
    }
}