using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;

public class RoundedImageView : ImageView
{
    public RoundedImageView(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs) { }
    public RoundedImageView(IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer) : base(javaRef, transfer) { }

    protected override void OnDraw(Canvas canvas)
    {
        if (this.IsInEditMode)
            base.OnDraw(canvas);
        else
        {
            var dw = this.Drawable as BitmapDrawable;
            if (dw == null)
                return;

            var w = this.MeasuredWidth;
            var h = this.MeasuredHeight;
            //var halfW = w / 2.0f;
            //var halfH = h / 2.0f;
            var radius = Math.Min(w, h) / 6;
            var p = m_path;
            if (p == null || w != m_pathWidth || h != m_pathHeight)
            {
                m_pathWidth = w;
                m_pathHeight = h;
                if (p != null)
                    p.Dispose();
                m_path = p = new Path();

                //p.AddCircle(halfW, halfH, radius, Path.Direction.Ccw);
                p.AddRoundRect(0, 0, w, h, radius, radius, Path.Direction.Ccw);
            }
            canvas.Save();
            canvas.ClipPath(p);

            base.OnDraw(canvas);
            var paint = dw.Paint;
            paint.SetStyle(Paint.Style.Stroke);
            paint.Color = BorderColor;
            paint.Flags = PaintFlags.AntiAlias;
            paint.StrokeWidth = UIUtil.DpToPx(1) + 1;
            canvas.Restore();
            //canvas.DrawCircle(halfW, halfH, radius, paint);
            p.AddRoundRect(0, 0, w, h, radius, radius, Path.Direction.Ccw);
            paint.Reset();
        }
    }

    public Color BorderColor;
    private Path m_path;
    private int m_pathWidth;
    private int m_pathHeight;

    protected override void Dispose(bool disposing)
    {
        if (m_path != null)
        {
            m_path.Dispose();
            m_path = null;
        }
        base.Dispose(disposing);
    }
}