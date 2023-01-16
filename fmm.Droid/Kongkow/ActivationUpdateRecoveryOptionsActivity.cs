using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using ChatAPI;

namespace fmm
{
    [Activity(NoHistory = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Locked)]
    public class ActivationUpdateRecoveryOptionsActivity : ActivationBaseActivity
    {
        private bool m_force;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationUpdateRecoveryOptions);
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;
            FindViewById(Resource.Id.invalidAnswer1).Visibility = ViewStates.Gone;
            FindViewById(Resource.Id.invalidAnswer2).Visibility = ViewStates.Gone;
            SetupSpinner("QuestionList1", Resource.Id.spQuestion1);
            SetupSpinner("QuestionList2", Resource.Id.spQuestion2);
            m_force = Intent.GetBooleanExtra("force", false);
            var btnCancel = FindViewById(Resource.Id.btnCancel);
            if (m_force) btnCancel.Visibility = ViewStates.Gone; else btnCancel.Click += btnCancel_Click;
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
        }

        private void SetupSpinner(string name, int id)
        {
            var il = Intent.GetStringArrayListExtra(name);
            var list = new string[il.Count + 1];
            list[0] = Resources.GetString(Resource.String.SelectQuestion);
            il.CopyTo(list, 1);
            var sp = FindViewById<Spinner>(id);
            sp.Adapter = new SpinnerAdapter<string>(list);
            sp.SetSelection(0);
        }

        public override void OnBackPressed()
        {
            if (!m_force) base.OnBackPressed();
        }

        public static void Start(IList<string> questionList1, IList<string> questionList2, bool force)
        {
            var i = new Intent(CurrentActivity, typeof(ActivationUpdateRecoveryOptionsActivity));
            i.PutStringArrayListExtra("QuestionList1", questionList1);
            i.PutStringArrayListExtra("QuestionList2", questionList2);
            i.PutExtra("force", force);
            CurrentActivity.StartActivity(i);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            var sp1 = FindViewById<Spinner>(Resource.Id.spQuestion1);
            var sp2 = FindViewById<Spinner>(Resource.Id.spQuestion2);
            if (sp1.SelectedItemPosition <= 0 || sp2.SelectedItemPosition <= 0)
            {
                PopupError(Resources.GetString(Resource.String.SelectQuestion));
                return;
            }

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

            Core.ActivationUpdateSecurityQuestion(Core.Owner.Username, (byte)(sp1.SelectedItemPosition - 1), answer1, (byte)(sp2.SelectedItemPosition - 1), answer2);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Finish();
        }

        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            FindViewById(Resource.Id.btnCancel).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.btnNext).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.pb).Visibility = ViewStates.Gone;

            if (type == ActivationType.UpdateSecurityQuestion && resultCode == 0)
            {
                Core.Setting.LastRemindSecurityQuestion = DateTime.Now;
                PopupInfo(Resources.GetString(Resource.String.RecoveryOptionsUpdated), Finish);
                return true;
            }

            return base.ActivationResult(networkSent, type, resultCode, extraInfo);
        }


    }
}