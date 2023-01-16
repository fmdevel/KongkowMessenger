using System;
using Android.Content;
using Android.Widget;
using Android.Graphics;
using ChatAPI;

public static partial class UIUtil // User Interface Utilities
{
    public static void SetDefaultDP(ImageView view, Contact contact)
    {
        if (contact.DP != null)
        {
            var u = Android.Net.Uri.FromFile(new Java.IO.File(contact.DP.Thumbnail));
            var b = u.BuildUpon();
            b.AppendQueryParameter("u", contact.DP.Update.ToString()); // Tricky to force update
            view.SetImageURI(Android.Net.Uri.Parse(b.ToString()));
            //view.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(contact.DP.Thumbnail)));
        }
        else
            view.SetImageResource(contact.IsServerPOS ? fmm.Resource.Drawable.server : fmm.Resource.Drawable.appdefault);
    }

    public static Bitmap GetBitmap(Context context, Android.Net.Uri uri, int maxSize)
    {
        var bitmapStream = context.ContentResolver.OpenInputStream(uri);
        var o = new BitmapFactory.Options();
        o.InJustDecodeBounds = true;

        BitmapFactory.DecodeStream(bitmapStream, null, o);
        bitmapStream.Close();

        BitmapFactory.Options o2 = new BitmapFactory.Options();
        o2.InSampleSize = (o.OutHeight > maxSize || o.OutWidth > maxSize) ? (int)Math.Pow(2, (int)Math.Round(Math.Log(maxSize / (double)Math.Max(o.OutHeight, o.OutWidth)) / Math.Log(0.5))) : 1;
        bitmapStream = context.ContentResolver.OpenInputStream(uri);
        Bitmap b = BitmapFactory.DecodeStream(bitmapStream, null, o2);
        bitmapStream.Close();
        return b;
    }

    public static class DisplayMetrics
    {
        public static readonly float Density;
        public static readonly int WidthPixelsPotrait;
        public static readonly int HeightPixelsPotrait;

        public static int WidthPixelsLandscape
        {
            get { return HeightPixelsPotrait; }
        }

        public static int HeightPixelsLandscape
        {
            get { return WidthPixelsPotrait; }
        }

        static DisplayMetrics()
        {
            var res = Android.App.Application.Context.Resources;
            var m = res.DisplayMetrics;
            Density = m.Density;
            int w = m.WidthPixels;
            int h = m.HeightPixels;
            if (res.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
            {
                WidthPixelsPotrait = h;
                HeightPixelsPotrait = w;
            }
            else
            {
                WidthPixelsPotrait = w;
                HeightPixelsPotrait = h;
            }
        }
    }

    public static int DpToPx(float dp)
    {
        return (int)(dp * DisplayMetrics.Density);
    }

    private static readonly int[] m_badgeRes = { -1, fmm.Resource.Drawable.badge_blue_circle, fmm.Resource.Drawable.badge_green_circle, fmm.Resource.Drawable.badge_gray_shield, fmm.Resource.Drawable.badge_gray_circle, fmm.Resource.Drawable.badge_blue_shield, fmm.Resource.Drawable.badge_red_circle };
    public static int GetBadgeResId(uint type)
    {
        uint r = type / 16;
        return r >= 7 ? fmm.Resource.Drawable.badge_red_shield : m_badgeRes[(int)r];
    }
}