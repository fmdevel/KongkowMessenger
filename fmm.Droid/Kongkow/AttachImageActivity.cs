using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace fmm
{
    [Activity]
    public class AttachImageActivity : Activity
    {
        private Bitmap m_bitmap;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.Kongkow_AttachImage);
            Window.AddFlags(WindowManagerFlags.Fullscreen);

            var imageView = FindViewById<ImageView>(Resource.Id.image);
            m_bitmap = UIUtil.GetBitmap(this, Intent.Data, 1024);

            FindViewById(Resource.Id.RotateLeft).Click += (o, e) =>
            {
                m_bitmap = UIUtil.Rotate(m_bitmap, -90);
                imageView.SetImageBitmap(m_bitmap);
            };

            FindViewById(Resource.Id.RotateRight).Click += (o, e) =>
            {
                m_bitmap = UIUtil.Rotate(m_bitmap, 90);
                imageView.SetImageBitmap(m_bitmap);
            };

            FindViewById(Resource.Id.Send).Click += Send_Click;
            imageView.SetImageBitmap(m_bitmap);

            if(Intent.GetBooleanExtra("disableDesc", false))
                FindViewById(Resource.Id.tbDesc).Visibility = ViewStates.Gone;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_bitmap != null && m_bitmap.IsRecycled)
                m_bitmap.Recycle();
        }

        private void Send_Click(object sender, EventArgs e)
        {
            var resultIntent = new Intent();
            resultIntent.PutExtra("image", UIUtil.Compress(m_bitmap, 46));
            resultIntent.PutExtra("desc", ChatAPI.Util.GuardValue(FindViewById<EditText>(Resource.Id.tbDesc).Text));
            SetResult(Result.Ok, resultIntent);
            Finish();
        }
    }
}