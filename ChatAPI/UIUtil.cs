using System;
using System.IO;

#if __ANDROID__ // Android

using Image = Android.Graphics.Bitmap;
using Android.Graphics;

#else // Desktop

using System.Drawing;
using System.Drawing.Imaging;

#endif

public static partial class UIUtil // User Interface Utilities
{
    public static Color CreateColor(int argb)
    {
#if __ANDROID__ // Android
        return new Color(argb);
#else
        return Color.FromArgb(argb);
#endif
    }

    public static Color CreateColor(int r, int g, int b)
    {
#if __ANDROID__ // Android
        return new Color(r, g, b);
#else
        return Color.FromArgb(r, g, b);
#endif
    }

    public static Color Highlight(Color color)
    {
        return Highlight(color, 35);
    }

    public static Color Highlight(Color color, int intensity)
    {
        return CreateColor(Math.Min(255, color.R + intensity), Math.Min(255, color.G + intensity), Math.Min(255, color.B + intensity));
    }

    public static Color Darker(Color color, int intensity)
    {
        return CreateColor(Math.Max(0, color.R - intensity), Math.Max(0, color.G - intensity), Math.Max(0, color.B - intensity));
    }

    public static Image ResizeImage(Image image, int width, int height) // High Quality Image Resize
    {
        if (width <= 0) width = 1;
        if (height <= 0) height = 1;
#if __ANDROID__ // Android
        var bitmap = Image.CreateBitmap(width, height, Image.Config.Argb8888);
        var matrix = new Matrix();
        matrix.SetScale(width / (float)image.Width, height / (float)image.Height);
        var canvas = new Canvas(bitmap);
        canvas.DrawBitmap(image, matrix, new Paint(PaintFlags.FilterBitmap));
#else
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, 0, 0, width, height);
        }
#endif
        return bitmap;
    }

    public static Image ResizeImage(Image image, int maxSize) // Resize Image Proportionally
    {
        if (maxSize > 0) // resize needed?
        {
            if (image.Width > maxSize && image.Width > image.Height)
                image = ResizeImage(image, maxSize, (int)((maxSize / (float)image.Width) * image.Height)); // Resize and maintain ratio
            else if (image.Height > maxSize)
                image = ResizeImage(image, (int)((maxSize / (float)image.Height) * image.Width), maxSize); // Resize and maintain ratio
        }
        return image;
    }

#if __ANDROID__
    public static Image Rotate(Image image, int degrees)
    {
        if (degrees != 0 && image != null)
        {
            var m = new Matrix();
            m.SetRotate(degrees,
                    (float)image.Width / 2, (float)image.Height / 2);
            try
            {
                var b2 = Image.CreateBitmap(
                        image, 0, 0, image.Width, image.Height, m, true);
                if (image != b2)
                {
                    image.Recycle();
                    image = b2;
                }
            }
            catch { }
        }
        return image;
    }

#else
    public static readonly ImageCodecInfo JpegEncoder = GetJpegEncoder();
    private static ImageCodecInfo GetJpegEncoder()
    {
        var jpegGuid = ImageFormat.Jpeg.Guid;
        var codecs = ImageCodecInfo.GetImageDecoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == jpegGuid)
                return codec;
        }
        return null;
    }
    public static Image FromArray(byte[] raw)
    {
        return Image.FromStream(new System.IO.MemoryStream(raw));
    }
    public static Image FromFile(string fileName)
    {
        return FromArray(System.IO.File.ReadAllBytes(fileName));
    }
#endif

    public static void Compress(Image image, int quality, Stream outputStream)
    {
#if __ANDROID__
        image.Compress(Bitmap.CompressFormat.Jpeg, quality, outputStream);
#else
        var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        image.Save(outputStream, JpegEncoder, parameters);
#endif
    }

    public static byte[] Compress(Image image, int quality)
    {
        var outputStream = new MemoryStream();
        Compress(image, quality, outputStream);
        return outputStream.ToArray();
    }
}