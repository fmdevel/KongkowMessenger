using System;
using System.Reflection;

namespace ChatAPI
{
    public static partial class Util
    {
        public const string APP_VERSION = "2.0.0";
        public const string APP_NAME = "Kongkow Messenger";
        public const string APP_TOS = "http://www.flash-machine.com/fm/kongkowtos.html";
        public const string APP_PRIVACY = "http://www.flash-machine.com/fm/privacypolicy.html";
#if BUILD_PARTNER
        public const string APP_FMUSER = "";
#endif
#if !__ANDROID__
        public const string APP_COMPANY = "PT Jawara Multi Pembayaran Indonesia";
#endif

        public static readonly byte[] EmptyBytes;
        public static readonly char[] EmptyChars;
        internal delegate string AllocString(int length);
        internal static readonly AllocString FastAllocateString;
        private static readonly string[] m_commonSeparator;

        private static int m_uniqueId;
        private static int m_hwUniqueId;

        static Util()
        {
            EmptyBytes = new byte[] { };
            EmptyChars = new char[] { };
            FastAllocateString = (AllocString)Delegate.CreateDelegate(typeof(AllocString), typeof(string).GetMethod("FastAllocateString", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null));

            m_commonSeparator = new string[] { "&%" };
            var time = DateTime.Now;
            m_uniqueId = (int)(time.AddYears(1 - time.Year).Ticks / 1000000L); // DO NOT CHANGE THIS ALGORITHM
        }

        public static int UniqeId
        {
            get
            {
                return ++m_uniqueId;
            }
        }

        public static int HwUniqueId
        {
            get
            {
                if (m_hwUniqueId == 0)
                    m_hwUniqueId = HwInfoHash();

                return m_hwUniqueId;
            }
        }


        public static string GuardValue(string value)
        {
            return (object)value == null ? string.Empty : value;
        }

        public static byte[] GuardValue(byte[] value)
        {
            return value == null ? EmptyBytes : value;
        }

        public static string[] CommonSplit(string value)
        {
            return value.Split(m_commonSeparator, StringSplitOptions.None);
        }

        public static string CommonJoin(string[] value)
        {
            return string.Join("&%", value);
        }

        public static string LocalFormatDate(DateTime value)
        {
            return LocalFormatDate(value, false);
        }

        public static string LocalFormatDate(DateTime value, bool shorten)
        {
            var timeNow = DateTime.Now;
            var minutesSpan = (timeNow.Ticks - value.Ticks) / 600000000;
            if (minutesSpan >= 0) // guard negative value
            {
                if (minutesSpan <= 1)
                    return Core.Setting.Language == Language.EN ? "Just now" : "Barusan";
                if (minutesSpan <= 30)
                    return minutesSpan.ToString() + (Core.Setting.Language == Language.EN ? (shorten ? " Mnts ago" : " Minutes ago") : (shorten ? " Mnt yg lalu" : " Menit yg lalu"));
            }
            return LocalFormatDate(timeNow, value, shorten);
        }

        public static string LocalFormatDateStatus(DateTime value)
        {
            if (value.Ticks == 0)
                return string.Empty;

            var timeNow = DateTime.Now;
            var secondsSpan = (timeNow.Ticks - value.Ticks) / 10000000;
            if (secondsSpan >= 0) // guard negative value
            {
                if (secondsSpan < 70)
                    return "Online";
                if (secondsSpan <= 30 * 60) // 30 minutes * 60 seconds
                {
                    if (Core.Setting.Language == Language.EN)
                        return "Active " + (secondsSpan / 60).ToString() + " Minutes ago";
                    else
                        return "Aktif " + (secondsSpan / 60).ToString() + " Menit yg lalu";
                }
            }
            return (Core.Setting.Language == Language.EN ? "Last seen " : "Aktif terakhir ") + LocalFormatDate(timeNow, value, false);
        }

        private static string LocalFormatDate(DateTime timeNow, DateTime value, bool shorten)
        {
            var today = timeNow.Date;
            var valueDate = value.Date;
            if (valueDate == today)
            {
                return value.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (valueDate == today.AddDays(-1))
            {
                return (Core.Setting.Language == Language.EN ? (shorten ? "Yest " : "Yesterday ") : (shorten ? "Kmrn " : "Kemarin ")) + value.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            }
            return value.ToString("d MMM h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string Rp(long value)
        {
            return string.Format("Rp {0:#,0}", value).Replace(',', '.');
        }

        public static string FullPhoneNumber(string value)
        {
            return (object)value != null && value.StartsWith("08") ? "62" + value.Substring(1) : value;
        }

        public static bool IsValidUsername(string username)
        {
            if ((object)username == null || username.Length < 6 || username.Length > 27) goto Invalid; // Must have length between 6 and 27
            char c1 = username[username.Length - 1];
            if (!((c1 >= 'a' && c1 <= 'z') || (c1 >= '0' && c1 <= '9'))) goto Invalid; // Must be ended with Alphanumeric
            c1 = username[0];
            if (c1 < 'a' || c1 > 'z') goto Invalid; // Must be started with Letter

            for (int i = 1; i < username.Length; i++)
            {
                char c2 = username[i];
                if (c2 == '_')
                {
                    if (c1 == '_' || c1 == '-' || c1 == '.') goto Invalid; // Can not contains "__" or "-_" or "._"
                }
                else if (c2 == '-')
                {
                    if (c1 == '_' || c1 == '-' || c1 == '.') goto Invalid; // Can not contains "_-"  or "--" or ".-"
                }
                else if (c2 == '.')
                {
                    if (c1 == '_' || c1 == '-' || c1 == '.') goto Invalid; // Can not contains "_."  or "-." or ".."
                }
                else if (!((c2 >= 'a' && c2 <= 'z') || (c2 >= '0' && c2 <= '9')))
                    goto Invalid; // Must be Alphanumeric

                c1 = c2; // Switch old to new char scan
            }

            return true;
        Invalid:
            return false;
        }

        public static bool DeleteFile(string fileName)
        {
            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    System.IO.File.Delete(fileName);
                }
                catch { return false; }
            }
            return true;
        }

#if __ANDROID__
        public static void StartThread(System.Threading.ThreadStart action)
        {
            var t = new System.Threading.Thread(action);
            t.IsBackground = true;
            t.Start();
        }
#else
        public static void StartThread(System.Threading.ThreadStart action)
        {
            var t = new System.Threading.Thread(action);
            t.IsBackground = true;
            t.Start();
        }
#endif


#if __ANDROID__

        private static int HwInfoHash()
        {
            int hash = 0;
            try
            {
                hash = Android.OS.Build.Serial.GetHashCode();
            }
            catch { }
            try
            {
                hash ^= Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId).GetHashCode();
            }
            catch { }
            try
            {
                hash ^= (Android.OS.Build.Hardware + Android.OS.Build.Device + Android.OS.Build.Model + Android.OS.Build.Product).GetHashCode();
            }
            catch { }
            return hash;
        }

        public static void PublishFileToGallery(string fileName)
        {
            try
            {
                Android.Media.MediaScannerConnection.ScanFile(Android.App.Application.Context, new string[] { fileName }, null, null);
            }
            catch { }
        }

        //public static void CallNumber(string number)
        //{
        //    try
        //    {
        //        var intent = new Android.Content.Intent(Android.Content.Intent.ActionDial);
        //        intent.SetData(Android.Net.Uri.Parse("tel:" + number));
        //        fmm.Activity.CurrentActivity.StartActivity(intent);
        //    }
        //    catch { }
        //}

        public static void OpenAssociatedFile(Android.Net.Uri uri, string mimeType)
        {
            try
            {
                var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
                intent.SetDataAndType(uri, mimeType);
                intent.SetFlags(Android.Content.ActivityFlags.ClearTop);
                fmm.Activity.CurrentActivity.StartActivity(intent);
            }
            catch { }
        }

        public static void ShareToAnotherApp(string fileName)
        {
            ShareToAnotherApp(Android.Net.Uri.FromFile(new Java.IO.File(fileName)), Java.Net.URLConnection.GuessContentTypeFromName(fileName));
        }

        public static void ShareToAnotherApp(Android.Net.Uri uri, string mimeType)
        {
            try
            {
                var intent = new Android.Content.Intent(Android.Content.Intent.ActionSend);
                intent.AddFlags(Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop ? Android.Content.ActivityFlags.ClearWhenTaskReset : Android.Content.ActivityFlags.NewDocument);
                intent.SetType(mimeType);
                intent.PutExtra(Android.Content.Intent.ExtraStream, uri);
                fmm.Activity.CurrentActivity.StartActivity(intent);
            }
            catch { }
        }

        public static void SetFocusAndShowSoftKeyboard(Android.Widget.EditText view)
        {
            view.RequestFocus();
            view.RequestFocusFromTouch();
            ((Android.Views.InputMethods.InputMethodManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.InputMethodService))
            .ToggleSoftInput(Android.Views.InputMethods.ShowFlags.Forced, Android.Views.InputMethods.HideSoftInputFlags.ImplicitOnly);
            view.SelectAll();
        }

        public static void SetHideSoftKeyboard(Android.Widget.EditText view)
        {
            view.ClearFocus();
            ((Android.Views.InputMethods.InputMethodManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.InputMethodService))
            .HideSoftInputFromWindow(view.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
        }
#else

        private static int HwInfoHash()
        {
            int hash = 0;
            try
            {
                var wmi = System.Runtime.InteropServices.Marshal.BindToMoniker("winmgmts:\\\\.\\root\\cimv2");
                var src = wmi.GetType().InvokeMember("ExecQuery", BindingFlags.InvokeMethod, null, wmi, new[] { "SELECT * FROM Win32_DiskDrive" });
                foreach (var prop in (System.Collections.IEnumerable)src)
                {
                    var t = prop.GetType();
                    var value = t.InvokeMember("SerialNumber", BindingFlags.GetProperty, null, prop, null);
                    if (value != null)
                        hash = value.ToString().GetHashCode();

                    value = t.InvokeMember("Signature", BindingFlags.GetProperty, null, prop, null);
                    if (value != null) // ((uint)Convert.ToInt64(value)).ToString() to get string signature
                        hash ^= (value is int) ? (int)value : (int)((uint)value); // return value is int but via System.Management, return uint, Microsoft idiots
                }
            }
            catch { }
            try
            {
                hash ^= Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Microsoft").OpenSubKey("Windows NT").OpenSubKey("CurrentVersion").GetValue("ProductId").ToString().GetHashCode();
            }
            catch { }
            return hash;
        }


#endif
    }
}