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
    public class ViewProfileActivity : Activity
    {
        private Android.Net.Uri m_outputUri;
        private bool m_isEditingName;
        private bool m_isEditingStatus;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ViewProfile);

            var contact = CurrentContact;
            FindViewById<TextView>(Resource.Id.ContactId).Text = contact.IsServerPOS ? "Server" : "@" + contact.Username;
            var r = UIUtil.GetBadgeResId(contact.AccType);
            var badge = FindViewById<ImageView>(Resource.Id.badge);
            if (r != -1) badge.SetImageResource(r);

            var name = contact.Name;
            if (name != contact.ID)
                FindViewById<EditText>(Resource.Id.ContactName).Text = name;

            if (contact.Status.Length > 0)
                FindViewById<EditText>(Resource.Id.ContactStatus).Text = contact.Status;

            string joinDate = contact.JoinDate == 0 ? null : Resources.GetString(Resource.String.JoinDate) + new DateTime(contact.JoinDate).ToLocalTime().ToString("d MMMM yyyy");
            FindViewById<TextView>(Resource.Id.lbJoinDate).Text = joinDate + (contact.AccType >= 16 ? "\r\n" + ((AccountType)contact.AccType).ToString().Replace('_', ' ') : null);

            var contactDP = FindViewById<ImageView>(Resource.Id.ContactDP);
            if (contact.DP != null)
                contactDP.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(contact.DP.Full)));
            else if (contact.IsServerPOS)
                contactDP.SetImageResource(Resource.Drawable.server);

            var layoutDP = FindViewById<RelativeLayout>(Resource.Id.ContactDPLayout);
            var layoutParamDP = layoutDP.LayoutParameters;
            if (Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
            {
                FindViewById<LinearLayout>(Resource.Id.ProfilePageLayout).Orientation = Orientation.Horizontal;
                layoutParamDP.Height = ViewGroup.LayoutParams.MatchParent;
                layoutParamDP.Width = (UIUtil.DisplayMetrics.WidthPixelsLandscape * 4) / 10;
                contactDP.SetScaleType(ImageView.ScaleType.CenterCrop);
            }
            else
            {
                layoutParamDP.Width = UIUtil.DisplayMetrics.WidthPixelsPotrait;
                layoutParamDP.Height = layoutParamDP.Width;
            }
            layoutDP.LayoutParameters = layoutParamDP;


            InitPopupMenu();
            FindViewById(Resource.Id.ContactNameSubmit).Click += ContactNameSubmit_Click;
            FindViewById(Resource.Id.ContactStatusSubmit).Click += ContactStatusSubmit_Click;
            FindViewById(Resource.Id.ContactIdCopy).Click += ContactIdCopy_Click;
            FindViewById(Resource.Id.SaveDPImage).Click += SaveDPImage_Click;

            var isOwner = contact.ID == Core.Owner.ID;
            FindViewById(Resource.Id.ContactStatus).Enabled = isOwner;
            var visible = isOwner ? ViewStates.Visible : ViewStates.Invisible;
            FindViewById(Resource.Id.ContactNameSubmit).Visibility = visible;
            FindViewById(Resource.Id.ContactDPEditImage).Visibility = visible;
            FindViewById(Resource.Id.ContactStatusSubmit).Visibility = visible;
            FindViewById(Resource.Id.SaveDPImage).Visibility = visible == ViewStates.Visible ? ViewStates.Invisible : ViewStates.Visible;


            RefreshEditStatus();
            RefreshEditName();
        }

        public override void OnBackPressed()
        {
            if (m_isEditingName)
            {
                m_isEditingName = false;
                RefreshEditName();
                return;
            }
            if (m_isEditingStatus)
            {
                m_isEditingStatus = false;
                RefreshEditStatus();
                return;
            }
            base.OnBackPressed();
        }

        private void SaveDPImage_Click(object sender, EventArgs e)
        {
            if (CurrentContact.DP != null)
            {
                Attachment att = new Attachment(".jpg", System.IO.File.ReadAllBytes(CurrentContact.DP.Full));
                Util.PublishFileToGallery(att.FileName);
                Toast.MakeText(Application.Context, Resources.GetString(Resource.String.PhotoSaved), ToastLength.Long).Show();
            }
        }

        private void ContactStatusSubmit_Click(object sender, EventArgs e)
        {
            var contact = CurrentContact;
            var contactStatusEdit = FindViewById<EditText>(Resource.Id.ContactStatus);

            if (m_isEditingStatus)
            {
                var newStatus = contactStatusEdit.Text.Trim();
                if (newStatus != contact.Status)
                {
                    contact.Status = newStatus;
                    Core.SaveContactInfo(contact);
                    if (!string.IsNullOrEmpty(newStatus))
                    {
                        //Core.UploadOwnerStatus();
                        Core.SyncContact(contact);
                        var itemFeed = new Feed(CurrentContact, DateTime.Now, ChatAPI.Notification.USER_UPDATE_STATUS, newStatus);
                        Core.UpdateFeed(itemFeed);
                    }
                    NotifySuccess(Resources.GetString(Resource.String.StatusUpdated));
                }
            }

            m_isEditingStatus = !m_isEditingStatus;
            RefreshEditStatus();
            if (m_isEditingStatus)
            {
                Util.SetFocusAndShowSoftKeyboard(contactStatusEdit);
            }
        }

        private void RefreshEditStatus()
        {
            FindViewById<ImageView>(Resource.Id.ContactStatusSubmit).SetImageResource(m_isEditingStatus ? Resource.Drawable.send : Resource.Drawable.ic_edit);
            SetEditable(FindViewById<EditText>(Resource.Id.ContactStatus), m_isEditingStatus);
        }

        private void RefreshEditName()
        {
            FindViewById<ImageView>(Resource.Id.ContactNameSubmit).SetImageResource(m_isEditingName ? Resource.Drawable.send : Resource.Drawable.ic_edit);
            SetEditable(FindViewById<EditText>(Resource.Id.ContactName), m_isEditingName);
        }

        private static void SetEditable(EditText view, bool flag)
        {
            view.Focusable = flag;
            view.FocusableInTouchMode = flag;
            view.Clickable = flag;
        }

        private void ContactNameSubmit_Click(object sender, EventArgs e)
        {
            var contact = CurrentContact;
            var contactName = FindViewById<EditText>(Resource.Id.ContactName);
            if (m_isEditingName)
            {
                var newName = contactName.Text.Trim();
                var oldName = contact.Name;
                if (oldName == contact.ID)
                    oldName = string.Empty;
                if (newName != oldName)
                {
                    contact.Name = newName;
                    Core.SaveContactInfo(contact);
                    Core.SyncContact(contact);
                }
            }
            m_isEditingName = !m_isEditingName;
            RefreshEditName();
            if (m_isEditingName)
            {
                Util.SetFocusAndShowSoftKeyboard(contactName);
            }
        }

        private void ContactIdCopy_Click(object sender, EventArgs e)
        {
            NotifySuccess(Resources.GetString(Resource.String.Copied));
        }

        private void NotifySuccess(string text)
        {
            ((Android.Text.ClipboardManager)Application.Context.GetSystemService(ClipboardService)).Text = CurrentContact.Username;
            Toast.MakeText(this, text, ToastLength.Long).Show();
        }

        private void InitPopupMenu()
        {
            var popupMenu = FindViewById<ImageView>(Resource.Id.ContactDPEditImage);
            popupMenu.Click += (object sender, EventArgs e) =>
            {
                var menuCamera = new MenuIcon(this, "Camera", Resource.Drawable.ic_camera);
                var menuGallery = new MenuIcon(this, "Gallery", Resource.Drawable.ic_gallery);
                ShowPopup(null, (menu) =>
                {
                    if (menu == menuCamera)
                    {
                        var fileOut = new Java.IO.File(Core.PublicDataDir, DateTime.Now.Ticks.ToString() + ".jpg");
                        m_outputUri = Android.Net.Uri.FromFile(fileOut);
                        var imageIntent = new Intent(Android.Provider.MediaStore.ActionImageCapture);
                        imageIntent.PutExtra(Android.Provider.MediaStore.ExtraOutput, m_outputUri);
                        StartActivityForResult(imageIntent, 7);
                    }
                    else if (menu == menuGallery)
                    {
                        var imageIntent = new Intent(Intent.ActionGetContent);
                        imageIntent.AddCategory(Intent.CategoryOpenable);
                        imageIntent.SetType("image/*");
                        StartActivityForResult(Intent.CreateChooser(imageIntent, string.Empty), 8);
                    }
                }, new MenuIconCollection(this, menuCamera, menuGallery));
            };
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;
            if (requestCode == 7)
            {
                StartActivityForResult(new Intent(string.Empty, m_outputUri, this, typeof(CropImageActivity)), 9);
            }
            else if (requestCode == 8)
            {
                StartActivityForResult(new Intent(string.Empty, data.Data, this, typeof(CropImageActivity)), 9);
            }
            else if (requestCode == 9)
            {
                var imageView = FindViewById<ImageView>(Resource.Id.ContactDP);
                var dp = data.GetByteArrayExtra("image");
                CurrentContact.SetDP(dp);
                Core.SaveContactInfo(CurrentContact);
                Core.UploadOwnerDP();
                imageView.SetImageURI(null); // Set null to force refresh
                imageView.SetImageURI(Android.Net.Uri.FromFile(new Java.IO.File(CurrentContact.DP.Full)));

                var itemFeed = new Feed(CurrentContact, DateTime.Now, ChatAPI.Notification.USER_UPDATE_DP, null);
                itemFeed.SetDP(dp);
                Core.UpdateFeed(itemFeed);
            }
        }

        internal static void Start(View anchor)
        {
            var a = CurrentActivity;
            var contactPOS = CurrentContact as ContactPOS;
            if (contactPOS == null)
            {
                a.StartActivity(typeof(ViewProfileActivity));
            }
            else
            {
                var menus = new string[] { "\u279C FLASH KURIR", a.Resources.GetString(Resource.String.ViewProfile) + " " + contactPOS.Name };
                var textColors = new Color[] { Color.DarkBlue, Color.DarkGray };
                a.CreateMenuPopup(menus, textColors,
                    (index) =>
                    {
                        if (index == 0)
                            OpenFlashKurir(CurrentContact as ContactPOS);
                        else
                            CurrentActivity.StartActivity(typeof(ViewProfileActivity));
                    }).ShowAsDropDown(anchor);
            }
        }

    }
}