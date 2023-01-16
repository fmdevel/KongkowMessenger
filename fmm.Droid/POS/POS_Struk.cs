using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Webkit;
using Android.Graphics;
using Android.Print;

using ChatAPI;
using ChatAPI.Connector;

namespace fmm
{
    [Activity]
    public class POS_Struk : PrintingSupport.PrintActivity // Activity
    {
        private TrxData m_trx;
        private string m_fileName;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                WebView.EnableSlowWholeDocumentDraw();
            SetContentView(Resource.Layout.POS_StrukPage);

            var btnSave = new MenuIcon(this, "Simpan", Orientation.Horizontal, Resource.Drawable.ic_save, 33);
            btnSave.Click += BtnSave_Click;
            var btnPrint = new MenuIcon(this, "Cetak", Orientation.Horizontal, Resource.Drawable.ic_print, 33);
            btnPrint.Click += BtnPrint_Click;
            var btnShare = new MenuIcon(this, "Share", Orientation.Horizontal, Resource.Drawable.ic_share, 33);
            btnShare.Click += BtnShare_Click;
            FindViewById<LinearLayout>(Resource.Id.HeaderStruk)
                .AddView(CreateMenuPopup(new MenuIconCollection(this, btnSave, btnPrint, btnShare)));

            var trxId = Intent.GetLongExtra("trxId", 0);
            if (trxId == 0)
                return;

            if (ContactPOS.Current == null)
                return;

            m_trx = ContactPOS.Current.GetHistory().FindTrxById(trxId);
            if (m_trx == null)
                return;

            var m_web = FindViewById<WebView>(Resource.Id.wHtml);
            m_web.SetWebViewClient(new WebViewClient());
            var m = m_trx.GetStruk();
            if (m.IndexOf("</") < 0)
                m = "<div style='word-wrap:break-word'>" + m + "</div>";
            m_web.LoadDataWithBaseURL(string.Empty, m, "text/html", "UTF-8", string.Empty);
        }

        public static void Show(long trxId)
        {
            var intent = new Intent(CurrentActivity, typeof(POS_Struk));
            intent.PutExtra("trxId", trxId);
            CurrentActivity.StartActivity(intent);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            m_fileName = CreateBitmapAndSave();
            if ((object)m_fileName != null)
                ShowPopup("Informasi", "Struk berhasil tersimpan. Buka Galeri?", "OK", OnGaleryOpenOK, "Nanti saja", null);
        }

        private void BtnShare_Click(object sender, EventArgs e)
        {
            m_fileName = CreateBitmapAndSave();
            if ((object)m_fileName != null)
                Util.ShareToAnotherApp(m_fileName);
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            ShowPopupList();
        }

        protected override PrintDocumentAdapter OnPrint()
        {
            return FindViewById<WebView>(Resource.Id.wHtml).CreatePrintDocumentAdapter();
        }

        protected override Bitmap OnBTPrint()
        {
            var b = CreateBitmap();
            if (m_trx.GetStruk().IndexOf("</") >= 0)
            {
                if (b.Width > b.Height)
                    b = UIUtil.Rotate(b, 90);
            }
            else if (b.Width < b.Height)
                b = UIUtil.Rotate(b, -90);
            return b;
        }

        private string GetFileName(string fileType)
        {
            return System.IO.Path.Combine(Core.PublicDataDir, m_trx.Destination + "_" + m_trx.Date.Ticks.ToString() + fileType);
        }

        private Bitmap CreateBitmap()
        {
            var w = FindViewById<WebView>(Resource.Id.wHtml);
            var p = w.CapturePicture();
            var b = Bitmap.CreateBitmap(p.Width, p.Height, Bitmap.Config.Argb8888);
            var c = new Canvas(b);
            p.Draw(c);
            return Crop(b);
        }

        private string CreateBitmapAndSave()
        {
            var b = CreateBitmap();
            var raw = UIUtil.Compress(b, 90);
            b.Recycle();
            var fileName = GetFileName(".jpg");
            try
            {
                File.WriteAllBytes(fileName, raw);
            }
            catch { }

            Util.PublishFileToGallery(fileName);
            return fileName;
        }

        private static Bitmap Crop(Bitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            int cropWidth = width;
            while (--cropWidth > 0)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = bitmap.GetPixel(cropWidth, y);
                    int r = (p >> 16) & 0xff;
                    int g = (p >> 8) & 0xff;
                    int b = p & 0xff;
                    if (r <= 160 || g <= 160 || b <= 160)
                        goto width_done; // darker pixel found
                }
            }
        width_done:

            int cropHeight = height;
            while (--cropHeight > 0)
            {
                for (int x = 0; x <= cropWidth; x++)
                {
                    var p = bitmap.GetPixel(x, cropHeight);
                    int r = (p >> 16) & 0xff;
                    int g = (p >> 8) & 0xff;
                    int b = p & 0xff;
                    if (r <= 160 || g <= 160 || b <= 160)
                        goto height_done; // darker pixel found
                }
            }
        height_done:

            cropWidth = Math.Min(cropWidth + 30, width);
            cropHeight = Math.Min(cropHeight + 30, height);
            if (cropWidth == width && cropHeight == height)
                return bitmap;

            var b2 = Bitmap.CreateBitmap(bitmap, 0, 0, cropWidth, cropHeight);
            bitmap.Recycle();
            return b2;
        }

        private void OnGaleryOpenOK()
        {
            Util.OpenAssociatedFile(Android.Net.Uri.FromFile(new Java.IO.File(m_fileName)), Java.Net.URLConnection.GuessContentTypeFromName(m_fileName));
        }
    }
}