
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using System;

namespace fmm
{
    [Activity]
    public class CropImageActivity : Activity
    {
        private int aspectX, aspectY;
        private CropImageView imageView;
        private Bitmap m_bitmap;
        public HighlightView Crop;
        public bool Saving;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.Kongkow_CropImage);
            Window.AddFlags(WindowManagerFlags.Fullscreen);

            aspectX = Intent.GetIntExtra("x", 1);
            aspectY = Intent.GetIntExtra("y", 1);

            imageView = new CropImageView(this);
            FindViewById<ViewGroup>(Resource.Id.CropImageView).AddView(imageView, -1, -1);
            m_bitmap = UIUtil.GetBitmap(this, Intent.Data, 1024);
            FindViewById(Resource.Id.Send).Click += (sender, e) => { onSaveClicked(); };
            FindViewById(Resource.Id.RotateLeft).Click += (o, e) =>
            {
                m_bitmap = UIUtil.Rotate(m_bitmap, -90);
                RotateBitmap rotateBitmap = new RotateBitmap(m_bitmap);
                imageView.SetImageRotateBitmapResetBase(rotateBitmap, true);
                addHighlightView();
            };

            FindViewById(Resource.Id.RotateRight).Click += (o, e) =>
            {
                m_bitmap = UIUtil.Rotate(m_bitmap, 90);
                RotateBitmap rotateBitmap = new RotateBitmap(m_bitmap);
                imageView.SetImageRotateBitmapResetBase(rotateBitmap, true);
                addHighlightView();
            };

            imageView.SetImageBitmapResetBase(m_bitmap, true);
            addHighlightView();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_bitmap != null && m_bitmap.IsRecycled)
                m_bitmap.Recycle();
        }

        private void addHighlightView()
        {
            Crop = new HighlightView(imageView);

            int width = m_bitmap.Width;
            int height = m_bitmap.Height;

            Rect imageRect = new Rect(0, 0, width, height);
            int cropWidth = Math.Min(width, height);
            int cropHeight = cropWidth;

            if (aspectX != 0 && aspectY != 0)
            {
                if (aspectX > aspectY)
                {
                    cropHeight = cropWidth * aspectY / aspectX;
                }
                else
                {
                    cropWidth = cropHeight * aspectX / aspectY;
                }
            }

            int x = (width - cropWidth) / 2;
            int y = (height - cropHeight) / 2;

            RectF cropRect = new RectF(x, y, x + cropWidth, y + cropHeight);
            Crop.Setup(imageView.ImageMatrix, imageRect, cropRect, aspectX != 0 && aspectY != 0);

            imageView.ClearHighlightViews();
            Crop.Focused = true;
            imageView.AddHighlightView(Crop);
        }

        private void onSaveClicked()
        {
            if (Saving)
            {
                return;
            }

            Saving = true;

            var r = Crop.CropRect;
            int width = r.Width();
            int height= r.Height();
            if(aspectX == aspectY)
            {
                width = Math.Min(width, 512);
                height = Math.Min(height, 512);
            }
            Bitmap imageResult = Bitmap.CreateBitmap(width, height, Bitmap.Config.Rgb565);
            {
                Canvas canvas = new Canvas(imageResult);
                Rect dstRect = new Rect(0, 0, width, height);
                canvas.DrawBitmap(m_bitmap, r, dstRect, new Paint(PaintFlags.FilterBitmap));
            }

            SetResult(Result.Ok, new Intent().PutExtra("image", UIUtil.Compress(imageResult, 46)));
            imageResult.Recycle();
            Finish();
        }
    }
}
