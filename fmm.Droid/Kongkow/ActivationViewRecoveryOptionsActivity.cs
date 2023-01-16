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
    public class ActivationViewRecoveryOptionsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ActivationViewRecoveryOptions);
            ThemedResId = new[] { Resource.Id.ActivityHeader };

            FindViewById<TextView>(Resource.Id.question1).Text = Intent.GetStringExtra("question1");
            FindViewById<TextView>(Resource.Id.question2).Text = Intent.GetStringExtra("question2");
            FindViewById<TextView>(Resource.Id.answer1).Text = Intent.GetStringExtra("answer1");
            FindViewById<TextView>(Resource.Id.answer2).Text = Intent.GetStringExtra("answer2");
            FindViewById(Resource.Id.btnCancel).Click += btnCancel_Click;
            FindViewById(Resource.Id.btnNext).Click += btnNext_Click;
        }

        public static void Start(Core.SecurityQuestion sq)
        {
            Core.Setting.LastRemindSecurityQuestion = DateTime.Now;
            var i = new Intent(CurrentActivity, typeof(ActivationViewRecoveryOptionsActivity));
            i.PutExtra("question1", sq.QuestionList1[sq.SelectedQuestionIndex1]);
            i.PutExtra("question2", sq.QuestionList2[sq.SelectedQuestionIndex2]);
            i.PutExtra("answer1", sq.Answer1);
            i.PutExtra("answer2", sq.Answer2);
            i.PutStringArrayListExtra("QuestionList1", sq.QuestionList1);
            i.PutStringArrayListExtra("QuestionList2", sq.QuestionList2);
            CurrentActivity.StartActivity(i);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            var list1 = Intent.GetStringArrayListExtra("QuestionList1");
            var list2 = Intent.GetStringArrayListExtra("QuestionList2");           
            ActivationUpdateRecoveryOptionsActivity.Start(list1, list2, false);
            Finish();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }
}