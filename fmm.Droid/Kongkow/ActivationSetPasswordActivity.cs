using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity(NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class ActivationSetPasswordActivity : ActivationBaseActivity
    {
        private string m_username;
        private int m_loginToken;
        private string m_recoveryCode;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationSetPassword);
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            m_username = Intent.GetStringExtra("Username");
            m_loginToken = Intent.GetIntExtra("LoginToken", 0);
            m_recoveryCode = Intent.GetStringExtra("RecoveryCode");
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
        }

        public override void OnBackPressed()
        {
            Core.Setting.ActivationState = Core.Owner == null ? ActivationState.NEED_ACTIVATION : ActivationState.ACTIVATED;
            base.OnBackPressed();
        }

        public static void Start(string username, int loginToken, string recoveryCode)
        {
            Core.Setting.LastRemindSecurityQuestion = DateTime.Now;
            Core.Setting.ActivationState = ActivationState.NEED_SET_PASSWORD;
            var i = new Intent(CurrentActivity, typeof(ActivationSetPasswordActivity));
            i.PutExtra("Username", username);
            i.PutExtra("LoginToken", loginToken);
            i.PutExtra("RecoveryCode", recoveryCode);
            CurrentActivity.StartActivity(i);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            string pass = FindViewById<EditText>(Resource.Id.tbPass).Text;
            if (pass.Length == 0)
            {
                PopupError("Password" + Resources.GetString(Resource.String.CannotBeEmpty));
                return;
            }
            if (pass.Length < 8)
            {
                PopupError(Resources.GetString(Resource.String.TooShortPassword));
                return;
            }
            foreach (char c in pass)
                if (!char.IsDigit(c)) goto PassIsOK;

            PopupError(Resources.GetString(Resource.String.InsecurePassword));
            return;

        PassIsOK:
            if (!string.Equals(pass, FindViewById<EditText>(Resource.Id.tbPassConfirm).Text))
            {
                PopupError(Resources.GetString(Resource.String.InvalidConfirmPassword));
                return;
            }

            FindViewById(Resource.Id.pb).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Invisible;
            Core.ActivationRecovery(m_username, m_loginToken, m_recoveryCode, pass);
        }

        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;

            if (base.ActivationResult(networkSent, type, resultCode, extraInfo))
                return true;

            if (type == ActivationType.Recovery)
            {
                if (resultCode == 0)
                {
                    if (Core.Owner == null)
                    {
                        PopupInfo(Resources.GetString(Resource.String.SuccessPleaseLogin), Finish);
                        Core.Setting.ActivationState = ActivationState.NEED_ACTIVATION;
                    }
                    else
                    {
                        PopupInfo(Resources.GetString(Resource.String.Success), Finish);
                        Core.Setting.ActivationState = ActivationState.ACTIVATED;
                    }
                }
                else
                {
                    Core.Setting.ActivationState = Core.Owner == null ? ActivationState.NEED_ACTIVATION : ActivationState.ACTIVATED;
                    PopupError(Resources.GetString(Resource.String.FailedPleaseTryAgain), Finish);
                }
                
                return true;
            }
            return false;
        }
    }
}