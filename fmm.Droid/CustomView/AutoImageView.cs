using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Widget;

public class AutoImageView : ImageView
{
    public AutoImageView(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs) { }
    public AutoImageView(IntPtr javaRef, Android.Runtime.JniHandleOwnership transfer) : base(javaRef, transfer) { }

    protected override void OnDraw(Canvas canvas)
    {
        if (this.GetScaleType() != ScaleType.CenterCrop)
        {
            this.SetScaleType(ScaleType.CenterCrop);
            return;
        }
        base.OnDraw(canvas);
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        var dw = this.Drawable as BitmapDrawable;
        if (dw == null)
            return;

        var w = dw.IntrinsicWidth;
        var h = dw.IntrinsicHeight;
        int parentW = MeasureSpec.GetSize(widthMeasureSpec);
        if (h > w)
        {
            base.SetMeasuredDimension(parentW, parentW);
        }
        else
        {
            base.SetMeasuredDimension(parentW, h * parentW / w);
        }
    }
}