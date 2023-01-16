using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using ChatAPI;

namespace fmm
{
    [Activity(NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class ActivationForgotPasswordActivity : ActivationBaseActivity
    {
        private string m_username;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationForgotPassword);
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            FindViewById(Resource.Id.invalidAnswer1).Visibility = ViewStates.Gone;
            FindViewById(Resource.Id.invalidAnswer2).Visibility = ViewStates.Gone;
            m_username = Intent.GetStringExtra("Username");
            FindViewById<TextView>(Resource.Id.question1).Text = Intent.GetStringExtra("question1");
            FindViewById<TextView>(Resource.Id.question2).Text = Intent.GetStringExtra("question2");
            if (Core.Owner != null) FindViewById<TextView>(Resource.Id.lbTitle).Text = Resources.GetString(Resource.String.ChangePassword);

            FindViewById(Resource.Id.btnCancel).Click += btnCancel_Click;
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
        }

        public static void Start(string username, string question1, string question2)
        {
            Core.Setting.ActivationState = ActivationState.ANSWER_SECURITY_QUESTION;
            var i = new Intent(CurrentActivity, typeof(ActivationForgotPasswordActivity));
            i.PutExtra("Username", username);
            i.PutExtra("question1", question1);
            i.PutExtra("question2", question2);
            CurrentActivity.StartActivity(i);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            var err1 = FindViewById(Resource.Id.invalidAnswer1);
            string answer1 = FindViewById<EditText>(Resource.Id.answer1).Text.Trim();
            if (answer1.Length < 2)
            {
                err1.Visibility = ViewStates.Visible;
                return;
            }

            var err2 = FindViewById(Resource.Id.invalidAnswer2);
            string answer2 = FindViewById<EditText>(Resource.Id.answer2).Text.Trim();
            if (answer2.Length < 2)
            {
                err2.Visibility = ViewStates.Visible;
                return;
            }

            FindViewById(Resource.Id.pb).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Invisible;
            FindViewById(Resource.Id.btnCancel).Visibility = ViewStates.Invisible;
            err1.Visibility = ViewStates.Gone;
            err2.Visibility = ViewStates.Gone;

            Core.ActivationAnswerSecurityQuestion(m_username, answer1, answer2);
        }

        public override void OnBackPressed()
        {
            Core.Setting.ActivationState = Core.Owner == null ? ActivationState.NEED_ACTIVATION : ActivationState.ACTIVATED;
            Finish();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            OnBackPressed();
        }

        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            FindViewById(Resource.Id.btnCancel).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;

            if (base.ActivationResult(networkSent, type, resultCode, extraInfo))
                return true;

            if (type == ActivationType.AnswerSecurityQuestion)
            {
                var res = (Core.AnswerSecurityQuestionResult)extraInfo;
                if (res != null)
                {
                    FindViewById(Resource.Id.invalidAnswer1).Visibility = res.Answer1IsCorrect ? ViewStates.Gone : ViewStates.Visible;
                    FindViewById(Resource.Id.invalidAnswer2).Visibility = res.Answer2IsCorrect ? ViewStates.Gone : ViewStates.Visible;

                    if (res.Answer1IsCorrect && res.Answer2IsCorrect && !string.IsNullOrEmpty(res.RecoveryCode))
                    {
                        ActivationSetPasswordActivity.Start(m_username, res.LoginToken, res.RecoveryCode);
                        Finish();
                    }

                    return true;
                }
            }
            return false;
        }

    }
}