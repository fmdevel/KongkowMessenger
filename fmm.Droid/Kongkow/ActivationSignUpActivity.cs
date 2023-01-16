using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    [Activity(NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class ActivationSignUpActivity : ActivationBaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationSignUp);
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            FindViewById<EditText>(Resource.Id.tbUsername).TextChanged += tbUsername_TextChanged;
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
        }

        public override void OnBackPressed()
        {
            Core.Setting.ActivationState = ActivationState.NEED_ACTIVATION;
            base.OnBackPressed();
        }

        public string Username
        {
            get { return Util.FullPhoneNumber(FindViewById<EditText>(Resource.Id.tbUsername).Text.Trim().ToLower()); }
        }
        private void ShowError(string message)
        {
            var lbDesc = FindViewById<TextView>(Resource.Id.lbDesc);
            lbDesc.Text = message;
            lbDesc.Visibility = ViewStates.Visible;
        }

        private void CantUseUsername()
        {
            ShowError(Resources.GetString(Resource.String.UsernameNotAvailable));
            FindViewById(Resource.Id.btnNext).Enabled = false;
        }

        private void tbUsername_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            string u = Username;
            if (Util.IsValidUsername(u))
            {
                FindViewById(Resource.Id.lbDesc).Visibility = ViewStates.Invisible;
                FindViewById(Resource.Id.btnNext).Enabled = true;
                return;
            }

            if (u.Length >= 6)
                CantUseUsername();
            else
            {
                ShowError("Username" + Resources.GetString(Resource.String.TooShort));
                FindViewById(Resource.Id.btnNext).Enabled = false;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            string u = Username;
            if (!Util.IsValidUsername(u))
                return;

            string name = FindViewById<EditText>(Resource.Id.tbName).Text.Trim();
            if (name.Length == 0)
            {
                PopupError(Resources.GetString(Resource.String.Name) + Resources.GetString(Resource.String.CannotBeEmpty));
                return;
            }
            else if (name.Length < 3)
            {
                PopupError(Resources.GetString(Resource.String.Name) + Resources.GetString(Resource.String.TooShort));
                return;
            }

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

            FindViewById(Resource.Id.lbDesc).Visibility = ViewStates.Invisible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Invisible;
            Core.ActivationSignUp(name, u, pass);
        }

        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            if (resultCode == 201)
            {
                CantUseUsername();
                return true;
            }
            return base.ActivationResult(networkSent, type, resultCode, extraInfo);
        }
    }
}