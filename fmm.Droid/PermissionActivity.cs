using System;
using Android.OS;
using Android.Content;
using ChatAPI;
using Android.App;

namespace fmm
{
    [Activity]
    public class PermissionActivity : Android.App.Activity
    {
        private static int m_permissionState; // 0=Unknown, 1=Granted, 2=Asking

        public static bool PermissionGranted
        { get { return m_permissionState == 1; } }

        public static bool EnsurePermissionGranted(Android.App.Activity a)
        {
            if (m_permissionState == 1)
                return true;
            if (m_permissionState == 0)
            {
                if (VerifyBasicPermission(a))
                {
                    m_permissionState = 1; // Granted
                    return true;
                }
                m_permissionState = 2; // Asking
                a.Finish();
                a.StartActivity(typeof(PermissionActivity));
            }
            return false;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Activity.CurrentActivity = this;
            base.OnCreate(savedInstanceState);
        }

        protected override void OnPause()
        {
            if (m_permissionState == 2) // User press Home or Back during asking permission
                m_permissionState = 0; // Restore state to Unknown

            base.OnPause();
            OverridePendingTransition(0, 0);
        }

        protected override void OnStart()
        {
            Activity.CurrentActivity = this;
            base.OnStart();
            OverridePendingTransition(0, 0);
        }

        protected override void OnResume()
        {
            Activity.CurrentActivity = this;
            base.OnResume();

            if (VerifyBasicPermission(this))
            {
                m_permissionState = 1; // Granted
                RestoreMainActivity();
                return;
            }
            RequestPermission();
        }

        private void RestoreMainActivity()
        {
            Finish();
            StartActivity(typeof(MainActivity));
        }

        private static bool VerifyBasicPermission(Android.App.Activity a)
        {
            return Build.VERSION.SdkInt < BuildVersionCodes.M || Android.Support.V4.Content.ContextCompat.CheckSelfPermission(a, Android.Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted;
        }
        private void RequestPermission()
        {
            Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.WriteExternalStorage }, 77);
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
                    RestoreMainActivity();
                }
            }
        }
    }
}