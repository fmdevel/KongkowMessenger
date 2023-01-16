using System;
using Android.Content;
using Android.Graphics;
using Android.Widget;

public class OnlineStatus : ImageView
{
    public OnlineStatus(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs) { }
    public OnlineStatus(IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer) : base(javaRef, transfer) { }
    protected override void OnDraw(Canvas canvas)
    {
        var w = this.MeasuredWidth;
        var h = this.MeasuredHeight;
        var halfW = w / 2.0f;
        var halfH = h / 2.0f;
        var radius = Math.Max(halfW, halfH);
        var paint = m_paint;
        if (paint == null)
            m_paint = paint = new Paint(PaintFlags.AntiAlias);

        paint.SetStyle(Paint.Style.Fill);
        paint.Color = new Color(0, 0xdd, 00);
        canvas.DrawCircle(halfW, halfH, radius - 1, paint);
        paint.SetStyle(Paint.Style.Stroke);
        paint.StrokeWidth = 1;
        paint.Color = Color.White;
        canvas.DrawCircle(halfW, halfH, radius - 1, paint);
    }

    private Paint m_paint;

    protected override void Dispose(bool disposing)
    {
        if (m_paint != null)
        {
            m_paint.Dispose();
            m_paint = null;
        }
        base.Dispose(disposing);
    }
}