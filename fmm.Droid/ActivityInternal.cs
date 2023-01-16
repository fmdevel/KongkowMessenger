using System;
using Android.OS;
using Android.Content;
using ChatAPI;
using Android.App;

namespace fmm
{
    partial class Activity
    {
        internal void StartChat(Contact contact)
        {
            if (Core.Setting.ChatSecure && Core.Setting.ShouldAskPassword(contact as ContactPOS == null ? TimeSpan.FromDays(3) : TimeSpan.FromHours(1)))
            {
                AskPassword(() =>
                {
                    CurrentContact = contact;
                    StartActivity(typeof(PrivateChatActivity));
                });
            }
            else
            {
                CurrentContact = contact;
                StartActivity(typeof(PrivateChatActivity));
            }
        }

        private static int m_wrongPassCount;
        internal void AskPassword(Action OnOK)
        {
            AskPassword(OnOK, true);
        }
        internal void AskPassword(Action OnOK, bool forceLogout)
        {
            var alert = new AlertDialog.Builder(this);
            //if (Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb)
            //    alert.SetInverseBackgroundForced(true);

            var layout = LayoutInflater.Inflate(Resource.Layout.Kongkow_AskPassword, null);
            alert.SetView(layout);
            alert.SetTitle("Password");
            var dialog = alert.Create();

            var onDismiss = new OnDismissListener(() => { if (forceLogout) AskPassword(OnOK); });
            var lbUsername = layout.FindViewById<Android.Widget.TextView>(Resource.Id.lbUsername).Text = "@" + Core.Owner.Username;
            var lbDesc = layout.FindViewById(Resource.Id.lbDesc);
            var tbPass = layout.FindViewById<Android.Widget.EditText>(Resource.Id.tbPass);
            var btnCancel = layout.FindViewById<Android.Widget.Button>(Resource.Id.btnCancel);
            btnCancel.Click += (sender, e) => { if (forceLogout) Logout(); else dialog.Dismiss(); };
            if (!forceLogout) btnCancel.Text = Resources.GetString(Resource.String.Cancel);

            tbPass.TextChanged += (sender, e) => { lbDesc.Visibility = Android.Views.ViewStates.Invisible; };
            layout.FindViewById(Resource.Id.btnNext).Click += (sender, e) =>
            {
                string pass = tbPass.Text;
                if (pass.Length == 0)
                    return;

                if (Crypto.VerifyPassword(pass))
                {
                    m_wrongPassCount = 0;
                    Core.Setting.LastAskPassword = DateTime.Now;
                    onDismiss.Action = null;
                    dialog.Dismiss();
                    OnOK.Invoke();
                    return;
                }
                if (++m_wrongPassCount < 4)
                    lbDesc.Visibility = Android.Views.ViewStates.Visible;
                else
                    Logout(); // Too many retries, LMAO
            };

            dialog.SetOnDismissListener(onDismiss);
            dialog.Show();
        }

        internal void AddContact(bool server)
        {
            var alert = new AlertDialog.Builder(this);
            if (Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb)
                alert.SetInverseBackgroundForced(true);

            var layout = LayoutInflater.Inflate(Resource.Layout.Kongkow_AddContact, null);
            alert.SetView(layout);
            alert.SetTitle(Resources.GetString(server ? Resource.String.AddContactServer : Resource.String.AddContact));
            var dialog = alert.Create();

            var lbDesc = layout.FindViewById<Android.Widget.TextView>(Resource.Id.lbDesc);
            var tbUsername = layout.FindViewById<Android.Widget.EditText>(Resource.Id.tbUsername);
            tbUsername.Hint = server ? "KodePengguna" : "Username";
            var btnNext = layout.FindViewById(Resource.Id.btnNext);
            layout.FindViewById(Resource.Id.btnCancel).Click += (sender, e) => { dialog.Dismiss(); };
            var pb = layout.FindViewById(Resource.Id.pb);

            tbUsername.TextChanged += (sender, e) =>
            {
                var u = tbUsername.Text.Trim().ToLower();
                if (u.Length > 0 && u[0] == '@')
                    u = u.Substring(1);
                u = Util.FullPhoneNumber(u);
                if (u.Length < 2)
                {
                    lbDesc.Text = (server ? "KodePengguna" : "Username") + Resources.GetString(Resource.String.TooShort);
                    btnNext.Enabled = false;
                }
                else
                {
                    lbDesc.Text = null;
                    btnNext.Enabled = true;
                }
            };

            btnNext.Click += (sender, e) =>
            {
                var u = tbUsername.Text.Trim().ToLower();
                if (u.Length > 0 && u[0] == '@')
                    u = u.Substring(1);
                u = Util.FullPhoneNumber(u);

                Core.OnActivation = new Core.ActivationResult((networkSent, type, resultCode, extraInfo) =>
                {
                    if (!networkSent)
                    {
                        RunOnUiThread(() =>
                        {
                            pb.Visibility = Android.Views.ViewStates.Invisible;
                            lbDesc.Text = Resources.GetString(Resource.String.NoConnection);
                        });
                        return true;
                    }

                    RunOnUiThread(() =>
                    {
                        if (type == ActivationType.CheckUsernameAvailability)
                        {
                            pb.Visibility = Android.Views.ViewStates.Invisible;
                            string id = (string)extraInfo;
                            if (resultCode == 0 || string.IsNullOrEmpty(id)) // Username is unavailable for registration (non existent user)
                                lbDesc.Text = Resources.GetString(Resource.String.UsernameNotFound);

                            else
                            {
                                dialog.Dismiss();
                                if (Core.Owner != null && Core.Owner.ID == id)
                                {
                                    // WTF adding his own ID
                                }
                                else
                                    StartChat(Core.AddContactAuto(id, null, u));
                            }
                        }
                    });
                    return true;
                });

                pb.Visibility = Android.Views.ViewStates.Visible;
                pb.Visibility = Android.Views.ViewStates.Visible;
                Core.ActivationCheckUsernameAvailability(u);
            };

            dialog.SetOnDismissListener(new OnDismissListener(() => { Core.OnActivation = null; }));
            dialog.Show();
        }

        internal static void Logout()
        {
            Core.Logout();
            var a = CurrentActivity;
            if (a == null)
                Process.KillProcess(Process.MyPid());
            else
            {
                a.RunOnUiThread(() =>
                {
                    var i = new Intent(Application.Context, typeof(MainActivity));
                    i.AddFlags(ActivityFlags.ClearTop);
                    a.StartActivity(i);
                });
            }
        }

        internal static bool TryNavigate(string url)
        {
            try
            {
                CurrentActivity.StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)));
                return true;
            }
            catch { }
            return false;
        }

        internal static void OpenFlashKurir(ContactPOS contact)
        {
            if (contact != null && Core.Owner != null)
            {
                int uid = Util.UniqeId;
                int key = Core.LoginToken ^ uid;
                var token = uid.ToString() + "|" + key.ToString() + "|" + Core.Owner.ID + "|" + contact.ID;
                var chars = new char[((4 * token.Length / 3) + 3) & ~3];
                int count = Convert.ToBase64CharArray(StringEncoder.UTF8.GetBytes(token), 0, token.Length, chars, 0);
                for (int i = 0; i < chars.Length; i++)
                {
                    char c = chars[i];
                    if (c == '+') chars[i] = '-';
                    else if (c == '/') chars[i] = '*';
                    else if (c == '=') chars[i] = '|';
                }
                TryNavigate("https://flashkurir.com/login.aspx?token=" + new string(chars, 0, count));
            }
        }
    }
}