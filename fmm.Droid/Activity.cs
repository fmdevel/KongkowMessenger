using System;
using Android.OS;
using Android.Content;
using Android.App;
using ChatAPI;

namespace fmm
{
    public abstract partial class Activity : Android.App.Activity
    {
        public static Activity CurrentActivity;
        public static Contact CurrentContact;
        protected int[] ThemedResId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            CurrentActivity = this;
            if (EnsurePermissionGranted(false))
            {
                Network.UserSeen = true;
                SetTheme(this);
            }
            base.OnCreate(savedInstanceState);
        }

        protected override void OnPause()
        {
            CurrentActivity = null;
            if (PermissionGranted) Network.UserSeen = false;
            else if (m_permissionState == 2) // User press Home or Back during asking permission
                m_permissionState = 0; // Restore state to Unknown

            base.OnPause();
            OverridePendingTransition(0, 0);
        }

        protected override void OnStart()
        {
            CurrentActivity = this;
            if (PermissionGranted) Network.UserSeen = true;
            base.OnStart();
            OverridePendingTransition(0, 0);
        }

        protected override void OnResume()
        {
            CurrentActivity = this;
            base.OnResume();
            if (EnsurePermissionGranted(true))
            {
                Network.UserSeen = true;
                if (ThemedResId != null)
                    foreach (int resId in ThemedResId)
                    {
                        var view = FindViewById(resId);
                        if (view != null)
                            view.SetBackgroundColor(Core.Setting.Themes);
                    }
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            CurrentActivity = this;
            Network.UserSeen = true;
            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            if (level == TrimMemory.UiHidden)
            {
                if (Core.Owner != null)
                    Core.CloseAllConversation(CurrentContact);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
            base.OnTrimMemory(level);
        }

        protected override void AttachBaseContext(Context @base)
        {
            if (EnsurePermissionGranted(false))
            {
                MainActivity.Initialize();
                if (UpdateConfig(@base)) @base = new ContextWrapper(@base);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    try
                    {
                        var type = Java.Lang.Class.FromType(typeof(StrictMode));
                        var m = type.GetMethod("disableDeathOnFileUriExposure");
                        m.Invoke(null);
                    }
                    catch { }
                }
            }
            base.AttachBaseContext(@base);
        }

        internal static bool UpdateConfig(Context context)
        {
            var config = context.Resources.Configuration;
            string lang = Core.Setting.Language == Language.ID ? "in" : "en"; // "in" for Indonesia, not "id"
            if (config.Locale.Language != lang) // Note: Locale.Language returns "in" for Indonesia
            {
                var locale = new Java.Util.Locale(lang);
                Java.Util.Locale.Default = locale;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1) config.SetLocale(locale); else config.Locale = locale;
                context.Resources.UpdateConfiguration(config, null);
                return true;
            }
            return false;
        }


        #region " Handler "

        private Handler m_handler;

        protected void ClearHandler()
        {
            if (m_handler != null)
                m_handler.RemoveCallbacksAndMessages(null);
        }

        protected void PostDelayed(Action action, long delay)
        {
            if (m_handler == null) m_handler = new Handler();
            m_handler.PostDelayed(action, delay);
        }

        #endregion

        #region " OS Theme "

        private static int m_supportedTheme;
        private static void SetTheme(Context context)
        {
            if (m_supportedTheme == 0)
                TrySetTheme(context);
            else if (m_supportedTheme > 0)
                context.SetTheme(m_supportedTheme);
        }

        private static void TrySetTheme(Context context)
        {
            try
            {
                context.SetTheme(Android.Resource.Style.ThemeDeviceDefaultLightNoActionBar);
                m_supportedTheme = Android.Resource.Style.ThemeDeviceDefaultLightNoActionBar;
                return;
            }
            catch { }
            try
            {
                context.SetTheme(Android.Resource.Style.ThemeHoloLightNoActionBar);
                m_supportedTheme = Android.Resource.Style.ThemeHoloLightNoActionBar;
                return;
            }
            catch { }
            try
            {
                context.SetTheme(Android.Resource.Style.ThemeLightNoTitleBar);
                m_supportedTheme = Android.Resource.Style.ThemeLightNoTitleBar;
                return;
            }
            catch { }
            m_supportedTheme = -1;
        }

        #endregion

        #region " Permission "

        private static int m_permissionState; // 0=Unknown, 1=Granted, 2=Asking
        public static bool PermissionGranted { get { return m_permissionState == 1; } }

        private bool EnsurePermissionGranted(bool ask)
        {
            if (PermissionGranted)
                return true;

            if (m_permissionState == 0)
            {
                if (VerifyPermission()) //if (VerifyPermission(this))
                {
                    m_permissionState = 1; // Granted
                    return true;
                }
                if (ask)
                {
                    m_permissionState = 2; // Asking
                    RequestPermission();
                }
            }
            return false;
        }


        internal static bool VerifyPermission()
        {
            return VerifyPermission(Android.Manifest.Permission.WriteExternalStorage);
        }
        internal static bool VerifyPermission(string permission)
        {
            return Build.VERSION.SdkInt < BuildVersionCodes.M || Application.Context.CheckSelfPermission(permission) == Android.Content.PM.Permission.Granted;
        }
        private void RequestPermission()
        {
            RequestPermissions(new string[] { Android.Manifest.Permission.WriteExternalStorage }, 77);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            if (requestCode == 77)
            {
                if (grantResults.Length > 0)
                {
                    for (int i = 0; i < grantResults.Length; i++) if (grantResults[i] != Android.Content.PM.Permission.Granted)
                        {
                            m_permissionState = 0; // Denied, then Restore state to Unknown
                            return;
                        }
                    m_permissionState = 1; // Granted
                    Recreate();
                }
            }
        }

        #endregion
    }
}