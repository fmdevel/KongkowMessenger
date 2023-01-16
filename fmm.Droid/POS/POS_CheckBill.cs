using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;

namespace fmm
{
    [Activity]
    public class POS_CheckBill : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.POS_CheckBill);

            FindViewById(Resource.Id.btnBack).Click += (sender, e) => this.OnBackPressed();
            FindViewById(Resource.Id.Send).Click += BtnPay_Click;

            var m_web = FindViewById<WebView>(Resource.Id.wHtml);
            m_web.SetWebViewClient(new WebViewClient());
            //web.Settings.JavaScriptEnabled = true;

            var content = Intent.GetStringExtra("html");
            content = content.Replace("\r\n", "<br />").Replace("\n", "<br />");
            if (content.IndexOf("<html", StringComparison.InvariantCultureIgnoreCase) < 0)
                content = "<html><body style='font-family:Arial;text-align:center'><span style='text-align:left;display:inline-block'>" +
                    content + "</span></body></html>";
            m_web.LoadDataWithBaseURL(string.Empty, content , "text/html", "UTF-8", string.Empty);
        }

        private void BtnPay_Click(object sender, EventArgs e)
        {
            SetResult(Result.Ok);
            Finish();
        }
    }
}