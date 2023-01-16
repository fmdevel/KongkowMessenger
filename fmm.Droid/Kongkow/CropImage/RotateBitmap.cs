using System;
using Android.Graphics;

namespace fmm
{
    public class RotateBitmap
    {
        public RotateBitmap(Bitmap bitmap)
        {
            Bitmap = bitmap;
        }

        public RotateBitmap(Bitmap bitmap, int rotation)
        {
            Bitmap = bitmap;
            Rotation = rotation % 360;
        }

        public int Rotation;
        public Bitmap Bitmap;

        public Matrix GetRotateMatrix()
        {
            // By default this is an identity matrix.
            Matrix matrix = new Matrix();

            if (Rotation != 0)
            {
                // We want to do the rotation at origin, but since the bounding
                // rectangle will be changed after rotation, so the delta values
                // are based on old & new width/height respectively.
                int cx = Bitmap.Width / 2;
                int cy = Bitmap.Height / 2;
                matrix.PreTranslate(-cx, -cy);
                matrix.PostRotate(Rotation);
                matrix.PostTranslate(Width / 2, Height / 2);
            }

            return matrix;
        }

        public bool IsOrientationChanged
        {
            get
            {
                return (Rotation / 90) % 2 != 0;
            }
        }

        public int Height
        {
            get
            {
                return IsOrientationChanged ? Bitmap.Width : Bitmap.Height;
            }
        }

        public int Width
        {
            get
            {
                return IsOrientationChanged ? Bitmap.Height : Bitmap.Width;
            }
        }

        // TOOD: Recyle
    }
}