using System;
using Android.Content;
using Android.Widget;
using Android.Graphics;
using ChatAPI;

namespace fmm
{

    public enum TabState : int
    {
        Active = 1,
        Inactive = 0,
        InactiveNotif = 2
    }

    public class TabIcon : ImageView
    {

        public int[] m_icons;
        private TabState m_state;
        public TabIcon(Context context, int inactiveBmp, int highlight, int inactiveNotifBmp) : base(context)
        {
            m_icons = new int[] { inactiveBmp, highlight, inactiveNotifBmp };
            SetImageResource(inactiveBmp);
        }

        public TabState State
        {
            get
            {
                return m_state;
            }
            set
            {
                if (value != m_state)
                {
                    m_state = value;
                    SetImageResource(m_icons[(int)value]);
                }
            }
        }

        public void UpdateActiveIcon()
        {
            if (State == TabState.Active)
                SetImageResource(m_icons[1]);
        }
    }
}

//using System;
//using Android.Content;
//using Android.Widget;
//using Android.Graphics;
//using ChatAPI;

//namespace fmm
//{

//    public enum TabState : int
//    {
//        Active = 1,
//        Inactive = 0,
//        InactiveNotif = 2
//    }

//    public class TabIcon : ImageView
//    {

//        public Bitmap[] m_icons;
//        private TabState m_state;
//        public TabIcon(Context context, Bitmap inactiveBmp, Bitmap inactiveNotifBmp) : base(context)
//        {
//            m_icons = new Bitmap[] { inactiveBmp, null, inactiveNotifBmp };
//            SetImageBitmap(inactiveBmp);
//        }

//        public TabState State
//        {
//            get
//            {
//                return m_state;
//            }
//            set
//            {
//                if (value != m_state)
//                {
//                    m_state = value;
//                    if (value == TabState.Active && m_icons[1] == null)
//                        AdjustIconColor();

//                    SetImageBitmap(m_icons[(int)value]);
//                }
//            }
//        }

//        public void UpdateActiveIcon()
//        {
//            AdjustIconColor();
//            if (State == TabState.Active)
//                SetImageBitmap(m_icons[1]);
//        }

//        private Color m_lastAdjustColor;
//        private void AdjustIconColor()
//        {
//            if (m_icons[1] == null || m_lastAdjustColor != Core.Setting.Themes)
//            {
//                m_icons[1] = AdjustIconColor(m_icons[0], Core.Setting.Themes);
//                m_lastAdjustColor = Core.Setting.Themes;
//            }
//        }

//        private static Bitmap AdjustIconColor(Bitmap bmp, Color themes)
//        {
//            var ret = Bitmap.CreateBitmap(bmp.Width, bmp.Height, bmp.GetConfig());
//            var canvas = new Canvas(ret);
//            var paint = new Paint();

//            var cm = new ColorMatrix(new float[]
//            {
//                0, 0, 0, 0, themes.R,
//                0, 0, 0, 0, themes.G,
//                0, 0, 0, 0, themes.B,
//                0, 0, 0, 1, 0
//            });
//            paint.SetColorFilter(new ColorMatrixColorFilter(cm));
//            canvas.DrawBitmap(bmp, 0, 0, paint);
//            return ret;
//        }
//    }
//}