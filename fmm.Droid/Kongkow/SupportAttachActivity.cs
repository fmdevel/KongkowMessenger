using System;
using Android.App;
using Android.Content;
using Android.Views;
using ChatAPI;

namespace fmm
{
    public abstract class SupportAttachActivity : Activity
    {
        private View m_attachView;
        private MenuIcon m_menuCamera;
        private MenuIcon m_menuGallery;
        private MenuIcon m_menuDocument;
        private bool m_disableDesc;

        public void SetAttachView(int resId, bool disableDesc)
        {
            m_attachView = FindViewById(resId);
            m_attachView.Click += view_Click;
            m_disableDesc = disableDesc;
        }

        private void view_Click(object sender, EventArgs e)
        {
            m_menuCamera = new MenuIcon(this, "Camera", Resource.Drawable.ic_camera);
            m_menuGallery = new MenuIcon(this, "Gallery", Resource.Drawable.ic_gallery);
            m_menuDocument = new MenuIcon(this, "Document", Resource.Drawable.mime_doc);
            ShowPopup(null, OnSelectMenu, new MenuIconCollection(this, m_menuCamera, m_menuGallery, m_menuDocument));
        }

        private void OnSelectMenu(MenuIcon menu)
        {
            Intent intent;
            if (menu == m_menuCamera)
            {
                intent = new Intent(Android.Provider.MediaStore.ActionImageCapture);
                var outputUri = Android.Net.Uri.FromFile(new Java.IO.File(Core.PublicDataDir, DateTime.Now.Ticks.ToString() + ".jpg"));
                m_attachView.Tag = outputUri;
                intent.PutExtra(Android.Provider.MediaStore.ExtraOutput, outputUri);
                StartActivityForResult(intent, 7);
            }
            else if (menu == m_menuGallery)
            {
                intent = new Intent(Intent.ActionGetContent);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("image/*");
                StartActivityForResult(Intent.CreateChooser(intent, string.Empty), 8);
            }
            else if (menu == m_menuDocument)
            {
                intent = new Intent(Intent.ActionGetContent);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*");
                StartActivityForResult(Intent.CreateChooser(intent, string.Empty), 43);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode != Result.Ok)
                return;

            if (requestCode == 9)
                OnAttach(data.GetByteArrayExtra("image"), ".jpg", data.GetStringExtra("desc"));

            else if (requestCode == 43)
                HandleAttachFile(data.Data);

            else if (requestCode == 7 || requestCode == 8)
            {
                var intent = new Intent(string.Empty,
                    requestCode == 7 ? (Android.Net.Uri)m_attachView.Tag : data.Data,
                    this, typeof(AttachImageActivity));
                intent.PutExtra("disableDesc", m_disableDesc);
                StartActivityForResult(intent, 9);
            }
        }

        private void HandleAttachFile(Android.Net.Uri uri)
        {
            var returnCursor = ContentResolver.Query(uri, null, null, null, null);
            int nameIndex = returnCursor.GetColumnIndex(Android.Provider.OpenableColumns.DisplayName);
            int sizeIndex = returnCursor.GetColumnIndex(Android.Provider.OpenableColumns.Size);
            returnCursor.MoveToFirst();
            string name = returnCursor.GetString(nameIndex);
            long size = returnCursor.GetLong(sizeIndex);
            returnCursor.Close();
            if (size > 30 * 1024 * 1024)
            {
                PopupError(Resources.GetString(Resource.String.FileTooLarge) + ", max 30 MB");
                return;
            }
            var inStream = ContentResolver.OpenInputStream(uri);
            int fileSize = (int)size;
            var buffer = new byte[fileSize];
            int offset = 0;
            while (fileSize > 0)
            {
                var count = inStream.Read(buffer, offset, fileSize);
                if (count <= 0)
                    break;

                offset += count;
                fileSize -= count;
            }
            OnAttach(buffer, System.IO.Path.GetExtension(name), string.Empty);
        }

        protected abstract void OnAttach(byte[] raw, string fileType, string desc);
    }
}