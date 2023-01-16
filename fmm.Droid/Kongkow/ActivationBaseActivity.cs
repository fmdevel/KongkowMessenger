using System;
using Android.OS;
using ChatAPI;

namespace fmm
{
    public class ActivationBaseActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ThemedResId = new[] { Resource.Id.ActivityHeader };
        }
        protected override void OnResume()
        {
            base.OnResume();
            Core.OnActivation = SetActivationResult;
        }

        protected override void OnPause()
        {
            Core.OnActivation = null;
            base.OnPause();
        }

        private bool SetActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            RunOnUiThread(() => ActivationResult(networkSent, type, resultCode, extraInfo));
            return true;
        }
        protected virtual bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            if (!networkSent)
            {
                PopupError(Resources.GetString(Resource.String.NoConnection));
                return true;
            }

            if (resultCode == 204)
            {
                Core.Logout();
                PopupError(Resources.GetString(Resource.String.TooManyAttempts), Finish); // Too many attempts, try again next 30 minutes
                return true;
            }

            else if (type == ActivationType.SignUp || type == ActivationType.UseExistingAccount)
            {
                if (resultCode == 0)
                {
                    var sq = (Core.SecurityQuestion)extraInfo;
                    if (sq != null)
                    {
                        if (sq.SelectedQuestionIndex1 < 0 || sq.SelectedQuestionIndex2 < 0)
                        {
                            var a = new Action(() =>
                            {
                                ActivationUpdateRecoveryOptionsActivity.Start(sq.QuestionList1, sq.QuestionList2, true);
                                Finish();
                            });

                            if (type == ActivationType.SignUp)
                                PopupInfo(Resources.GetString(Resource.String.SignUpThanks), a);
                            else
                                a.Invoke();
                        }
                        else
                        {                           
                            ActivationViewRecoveryOptionsActivity.Start(sq);
                            Finish();
                        }
                    }
                    return true;
                }

                if (resultCode == 202)
                {
                    PopupError(Resources.GetString(Resource.String.InvalidLogin)); // Invalid Username or Password
                    return true;
                }

                if (resultCode == 205) // Too many user registered with same ip address. This is anti-flood mechanism
                {
                    PopupError(Resources.GetString(Resource.String.ServerBusy)); // Server busy, try again next 30 minutes
                    return true;
                }
            }

            return false; // Unhandled
        }
    }
}