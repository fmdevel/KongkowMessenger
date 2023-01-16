using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using ChatAPI;

namespace fmm
{
    [Activity]
    public class SettingActivity : ActivationBaseActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_Setting);
            FindViewById(Resource.Id.rlNotification).Click += btnNotification;
            FindViewById(Resource.Id.rlLanguange).Click += btnLanguange;
            FindViewById(Resource.Id.rlThemes).Click += btnThemes;
            FindViewById(Resource.Id.rlWallpaper).Click += btnWallpaper;
            FindViewById(Resource.Id.rlRecoveryOptions).Click += btnRecoveryOptions;
            FindViewById(Resource.Id.rlChangePassword).Click += btnChangePassword;
            FindViewById(Resource.Id.PrivacyPolicy).Click += PrivacyPolicy;
            FindViewById(Resource.Id.TOS).Click += TOS;
        }

        private void StartColorPickerActivity(int mode) // 0=Wallpaper, 1=Themes
        {
            var intent = new Intent(this, typeof(ColorPickerActivity));
            intent.PutExtra("mode", mode);
            StartActivity(intent);
        }

        private void btnWallpaper(object sender, EventArgs e)
        {
            var menuGallery = new MenuIcon(this, "Gallery", Resource.Drawable.ic_gallery);
            var menuWallpaper = new MenuIcon(this, Resources.GetString(Resource.String.ChooseColor), Resource.Drawable.solidColor);
            var menuReset = new MenuIcon(this, "Reset", Resource.Drawable.clear);
            ShowPopup("Wallpaper", (menu) =>
            {
                if (menu == menuGallery)
                {
                    var intent = new Intent(Intent.ActionGetContent);
                    intent.AddCategory(Intent.CategoryOpenable);
                    intent.SetType("image/*");
                    StartActivityForResult(Intent.CreateChooser(intent, string.Empty), 1);
                }
                else if (menu == menuWallpaper)
                    StartColorPickerActivity(0);
                else if (menu == menuReset)
                {
                    Core.Setting.ChatWallpaper = string.Empty;
                    Core.Setting.ColorWallpaper = Color.WhiteSmoke; // Default Color
                    Toast.MakeText(this, Resources.GetString(Resource.String.RemoveWallpaper), ToastLength.Short).Show();
                }
            }, new MenuIconCollection(this, menuGallery, menuWallpaper, menuReset));
        }

        private bool m_recovery;
        private void btnRecoveryOptions(object sender, EventArgs e)
        {
            m_recovery = true;
            AskPassword(() => { Core.ActivationGetSecurityQuestion(Core.Owner.Username); }, false);
        }
        private void btnChangePassword(object sender, EventArgs e)
        {
            m_recovery = false;
            Core.ActivationGetSecurityQuestion(Core.Owner.Username);
        }
        protected override bool ActivationResult(bool networkSent, ActivationType type, byte resultCode, object extraInfo)
        {
            if (type == ActivationType.GetSecurityQuestion)
            {
                var sq = (Core.SecurityQuestion)extraInfo;
                if (sq != null)
                {
                    if (m_recovery)
                    {
                        if (sq.SelectedQuestionIndex1 < 0 || sq.SelectedQuestionIndex2 < 0)
                            ActivationUpdateRecoveryOptionsActivity.Start(sq.QuestionList1, sq.QuestionList2, true);
                        else
                            ActivationViewRecoveryOptionsActivity.Start(sq);
                    }
                    else
                    {
                        if (sq.SelectedQuestionIndex1 < 0 || sq.SelectedQuestionIndex2 < 0)
                            PopupError(Resources.GetString(Resource.String.NoRecoveryOptions));
                        else
                            ActivationForgotPasswordActivity.Start(Core.Owner.Username, sq.QuestionList1[sq.SelectedQuestionIndex1], sq.QuestionList2[sq.SelectedQuestionIndex2]);
                    }
                    return true;
                }
            }

            return base.ActivationResult(networkSent, type, resultCode, extraInfo);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;

            if (requestCode == 1)
            {
                var intent = new Intent(string.Empty, data.Data, this, typeof(CropImageActivity));
                intent.PutExtra("x", 2);
                intent.PutExtra("y", 3);
                StartActivityForResult(intent, 9);
            }
            else if (requestCode == 9)
            {
                var wallpaper = System.IO.Path.Combine(Core.PublicDataDir, "ChatWallpaper");
                System.IO.File.WriteAllBytes(wallpaper, data.GetByteArrayExtra("image"));

                Core.Setting.ChatWallpaper = wallpaper;
                Toast.MakeText(this, Resources.GetString(Resource.String.WallpaperSet), ToastLength.Short).Show();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            Core.Setting.FontSize = FindViewById<Spinner>(Resource.Id.spFontSize).SelectedItemPosition;
            Core.Setting.Vibrate = FindViewById<ToggleButton>(Resource.Id.tgVibrate).Checked;
            Core.Setting.ChatSecure = FindViewById<ToggleButton>(Resource.Id.tgChatSecure).Checked;
        }

        private void btnNotification(object sender, EventArgs e)
        {
            StartActivity(typeof(NotificationActivity));
        }

        private void btnLanguange(object sender, EventArgs e)
        {
            SelectLanguage(this);
        }

        private void btnThemes(object sender, EventArgs e)
        {
            var menuWallpaper = new MenuIcon(this, GetString(Resource.String.ChooseColor), Resource.Drawable.solidColor);
            var menuReset = new MenuIcon(this, "Reset", Resource.Drawable.clear);
            ShowPopup(Resources.GetString(Resource.String.Themes), (menu) =>
            {
                if (menu == menuWallpaper)
                    StartColorPickerActivity(1);
                else if (menu == menuReset)
                {
                    Core.Setting.Themes = Core.Setting.ThemesDefault;
                    this.OnResume(); // Force update current theme
                }
            }, new MenuIconCollection(this, menuWallpaper, menuReset));
        }

        internal static bool PendingRecreate;
        protected override void OnResume()
        {
            base.OnResume();
            FindViewById<TextView>(Resource.Id.LanguageName).Text = Core.Setting.Language == Language.EN ? "English" : "Indonesia";
            FindViewById<TextView>(Resource.Id.NotificationName).Text = Core.Setting.Read("NotificationName");
            FindViewById<ToggleButton>(Resource.Id.tgVibrate).Checked = Core.Setting.Vibrate;
            FindViewById<ToggleButton>(Resource.Id.tgChatSecure).Checked = Core.Setting.ChatSecure;

            var sp = FindViewById<Spinner>(Resource.Id.spFontSize);
            sp.Adapter = new SpinnerAdapter<string>(new[] {
                Resources.GetString(Resource.String.FontSmall),
                Resources.GetString(Resource.String.FontMedium),
                Resources.GetString(Resource.String.FontLarge),
                Resources.GetString(Resource.String.FontExtraLarge)});

            sp.SetSelection((int)Core.Setting.FontSize);

            var shape = new Android.Graphics.Drawables.GradientDrawable();
            shape.SetCornerRadius(6);
            shape.SetStroke(1, Color.Gray);
            shape.SetColor(Core.Setting.Themes);
            FindViewById<View>(Resource.Id.SelectedThemes).SetBackgroundDrawable(shape);
        }

        internal static void SelectLanguage(Activity a)
        {
            var layout = a.LayoutInflater.Inflate(Resource.Layout.Kongkow_Language, null);
            var rIndonesia = layout.FindViewById<RadioButton>(Resource.Id.rIndonesia);
            var rEnglish = layout.FindViewById<RadioButton>(Resource.Id.rEnglish);

            if (Core.Setting.IsLanguageSpecified)
            {
                rIndonesia.Checked = Core.Setting.Language == Language.ID;
                rEnglish.Checked = Core.Setting.Language == Language.EN;
            }
            a.PopupOK("Pilih Bahasa / Choose Language", layout, () =>
            {
                int selectedLang = -1;
                if (rIndonesia.Checked) selectedLang = 0;
                if (rEnglish.Checked) selectedLang = 1;
                if (selectedLang >= 0)
                {
                    if (!Core.Setting.IsLanguageSpecified || Core.Setting.Language != (Language)((byte)selectedLang))
                    {
                        Core.Setting.Language = (Language)((byte)selectedLang);
                        PendingRecreate = true;
                    }
                }
            }, () => EnsureLanguageSpecified(a));
        }

        private static void EnsureLanguageSpecified(Activity a)
        {
            try
            {
                if (!Core.Setting.IsLanguageSpecified)
                    SelectLanguage(a);
                else if (PendingRecreate)
                {
                    if (CurrentActivity as MainActivity != null)
                        PendingRecreate = false;

                    CurrentActivity.Recreate();
                }
            }
            catch { }
        }

        private void PrivacyPolicy(object sender, EventArgs e)
        {
            TryNavigate(Util.APP_PRIVACY);
        }

        private void TOS(object sender, EventArgs e)
        {
            TryNavigate(Util.APP_TOS);
        }
    }
}