using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity(NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class ActivationLoginActivity : ActivationBaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationLogin);
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            FindViewById<EditText>(Resource.Id.tbUsername).TextChanged += tbUsername_TextChanged;
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
            FindViewById(Resource.Id.lbSignUp).Click += lbSignUp_Click;
            FindViewById(Resource.Id.lbForgotPassword).Click += lbForgotPassword_Click;
        }

        public override void OnBackPressed()
        {
            Finish();
            Process.KillProcess(Process.MyPid());
        }

        public string Username
        {
            get { return Util.FullPhoneNumber(FindViewById<EditText>(Resource.Id.tbUsername).Text.Trim().ToLower()); }
        }
        private void tbUsername_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            FindViewById(Resource.Id.btnNext).Enabled = (Username.Length >= 2);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            string u = Username;
            if (!CheckUsername(u))
                return;

            string pass = FindViewById<EditText>(Resource.Id.tbPass).Text;
            if (pass.Length == 0)
            {
                PopupError("Password" + Resources.GetString(Resource.String.CannotBeEmpty));
                return;
            }

            FindViewById(Resource.Id.pb).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Invisible;
            FindViewById(Resource.Id.llSignUp).Visibility = ViewStates.Invisible;
            Core.ActivationUseExisting(u, pass);
        }

        private void lbSignUp_Click(object sender, EventArgs e)
        {
            Core.Setting.ActivationState = ActivationState.USER_SIGNUP;
            StartActivity(typeof(ActivationSignUpActivity));
            Finish();
        }

        private bool CheckUsername(string u)
        {
            if (u.Length == 0)
            {
                PopupError("Username" + Resources.GetString(Resource.String.CannotBeEmpty));
                return false;
            }
            if (u.Length < 2)
            {
                PopupError("Username" + Resources.GetString(Resource.String.TooShort));
                return false;
            }
            return true;
        }

        private void lbForgotPassword_Click(object sender, EventArgs e)
        {
            string u = Username;
            if (!CheckUsername(u))
                return;

            FindViewById(Resource.Id.pb).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Invisible;
            FindViewById(Resource.Id.llSignUp).Visibility = ViewStates.Invisible;
            Core.ActivationCheckUsernameAvailability(u);
        }

        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            FindViewById(Resource.Id.llSignUp).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            Core.SecurityQuestion sq;

            if (base.ActivationResult(networkSent, type, resultCode, extraInfo))
                return true;

            if (type == ActivationType.CheckUsernameAvailability)
            {
                if (resultCode == 0) // Username is available for registration (not existent user)
                    PopupError(Resources.GetString(Resource.String.UsernameNotFound));
                else
                    Core.ActivationGetSecurityQuestion(Username);

                return true;
            }

            else if (type == ActivationType.GetSecurityQuestion)
            {
                sq = (Core.SecurityQuestion)extraInfo;
                if (sq != null)
                {
                    if (sq.SelectedQuestionIndex1 < 0 || sq.SelectedQuestionIndex2 < 0)
                    {
                        PopupError(Resources.GetString(Resource.String.NoRecoveryOptions));
                    }
                    else
                    {
                        ActivationForgotPasswordActivity.Start(Username, sq.QuestionList1[sq.SelectedQuestionIndex1], sq.QuestionList2[sq.SelectedQuestionIndex2]);
                        Finish();
                    }
                    return true;
                }
            }

            return false;
        }

    }
}