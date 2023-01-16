using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.Views;
using Android.OS;
using Android.Graphics;
using ChatAPI;

namespace fmm
{
    partial class Activity
    {
        public AlertDialog CreatePopup(string title, View content, string OK, Action OnOK, string cancel, Action OnCancel, Action OnDismiss)
        {
            var alert = new AlertDialog.Builder(this);
            //if (Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb)
            //    alert.SetInverseBackgroundForced(true);

            alert.SetView(content);

            if (!string.IsNullOrEmpty(title))
                alert.SetTitle(title);

            var dialog = alert.Create();

            if (!string.IsNullOrEmpty(OK))
                dialog.SetButton(-1, OK, (OnOK == null) ? null : new EventHandler<DialogClickEventArgs>((sender, e) => { OnOK.Invoke(); }));

            if (!string.IsNullOrEmpty(cancel))
                dialog.SetButton(-2, cancel, (OnCancel == null) ? null : new EventHandler<DialogClickEventArgs>((sender, e) => { OnCancel.Invoke(); }));

            if (OnDismiss != null)
                dialog.SetOnDismissListener(new OnDismissListener(OnDismiss));

            return dialog;
        }

        public AlertDialog CreatePopup(string title, View content, string OK, Action OnOK, string cancel, Action OnDismiss)
        {
            return CreatePopup(title, content, OK, OnOK, cancel, null, OnDismiss);
        }

        public AlertDialog CreatePopup(string title, string message, string OK, Action OnOK, string cancel, Action OnCancel, Action OnDismiss)
        {
            var content = new TextView(this);
            int pad = UIUtil.DpToPx(13.5f);
            content.SetPadding(pad, pad, pad, pad);
            content.Text = message;
            return CreatePopup(title, content, OK, OnOK, cancel, OnCancel, OnDismiss);
        }

        public void ShowPopup(string title, View content, string OK, Action OnOK, string cancel, Action OnDismiss)
        {
            CreatePopup(title, content, OK, OnOK, cancel, OnDismiss).Show();
        }

        public void ShowPopup(string title, string message, string OK, Action OnOK, string cancel, Action OnDismiss)
        {
            CreatePopup(title, message, OK, OnOK, cancel, null, OnDismiss).Show();
        }

        public void PopupOK(string title, View content, Action OnOK, Action OnDismiss)
        {
            ShowPopup(title, content, "OK", OnOK, null, OnDismiss);
        }

        public void PopupOK(string title, string message, Action OnOK, Action OnDismiss)
        {
            ShowPopup(title, message, "OK", OnOK, null, OnDismiss);
        }

        public void PopupOK(string title, string message)
        {
            PopupOK(title, message, null, null);
        }
        public void PopupInfo(string message, Action OnDismiss = null)
        {
            PopupOK(Core.Setting.Language == Language.ID ? "Informasi" : "Information", message, null, OnDismiss);
        }

        public void PopupError(string message, Action OnDismiss = null)
        {
            PopupOK("Error", message, null, OnDismiss);
        }


        public void PopupMenu(string[] menuItems, Action<int> OnClick, View anchor)
        {
            CreateMenuPopup(menuItems, OnClick).ShowAsDropDown(anchor);
        }

        public void PopupMenu(string[] menuItems, Action<int> OnClick, View anchor, int x, int y)
        {
            var popup = CreateMenuPopup(menuItems, OnClick);
            popup.ShowAsDropDown(anchor, UIUtil.DpToPx(x), UIUtil.DpToPx(y));
        }

        public PopupWindow CreateMenuPopup(string[] menuItems, Action<int> OnClick)
        {
            return CreateMenuPopup(menuItems, null, OnClick);
        }
        public PopupWindow CreateMenuPopup(string[] menuItems, Color[] textColors, Action<int> OnClick)
        {
            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;
            layout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            layout.SetBackgroundColor(Color.LightGray);

            var popup = new PopupWindow(layout, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, true);
            popup.SetBackgroundDrawable(new Android.Graphics.Drawables.BitmapDrawable());
            popup.OutsideTouchable = true;
            float fFontSize = (byte)Core.Setting.FontSize * 5 + 13;

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (!string.IsNullOrEmpty(menuItems[i]))
                {
                    var item = new TextView(this);
                    var par = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    par.SetMargins(1, i == 0 ? 1 : 0, 1, 1);
                    item.TextSize = fFontSize;
                    item.Text = menuItems[i];
                    item.SetBackgroundColor(Color.White);
                    var color = textColors == null ? Color.DarkGray : textColors[i];
                    item.SetTextColor(color);
                    item.SetPadding(15, 20, 130, 20);
                    item.Tag = i;
                    item.Clickable = true;
                    item.Click += (object sender, EventArgs e) => { popup.Dismiss(); OnClick.Invoke((int)item.Tag); };
                    layout.AddView(item, par);
                }
            }
            return popup;
        }

        public class MenuIcon : LinearLayout
        {
            public MenuIcon(Context context, string text, int iconResId) : this(context, text, Orientation.Vertical, iconResId, 54) { }

            public MenuIcon(Context context, string text, Orientation textOrientation, int iconResId, int dpiIconSize) : this(context, text, 16, textOrientation, iconResId, dpiIconSize) { }
            public MenuIcon(Context context, string text, float dpiTextSize, Orientation textOrientation, int iconResId, int dpiIconSize) : base(context)
            {
                this.Orientation = textOrientation;
                this.LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                this.SetPadding(10, 0, 10, 0);
                this.SetGravity(textOrientation == Orientation.Vertical ? GravityFlags.CenterHorizontal : GravityFlags.CenterVertical);
                var imageView = new ImageView(base.Context);
                imageView.SetImageResource(iconResId);
                var iconSize = UIUtil.DpToPx(dpiIconSize);
                this.AddView(imageView, iconSize, iconSize);
                if (!string.IsNullOrEmpty(text))
                {
                    var textView = new TextView(base.Context);
                    textView.Gravity = GravityFlags.Center;
                    textView.SetTextColor(Color.Black);
                    textView.SetTextSize(Android.Util.ComplexUnitType.Dip, dpiTextSize);
                    textView.Text = text;
                    this.AddView(textView, ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                }
            }
        }

        public class MenuIconCollection : LinearLayout
        {
            public MenuIconCollection(Context context, params MenuIcon[] menus) : this(context, 20, menus) { }
            public MenuIconCollection(Context context, int verticalPadding, params MenuIcon[] menus) : base(context)
            {
                this.Orientation = Orientation.Horizontal;
                this.LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                this.SetPadding(10, verticalPadding, 10, verticalPadding);
                this.SetGravity(GravityFlags.CenterVertical);

                foreach (var menu in menus)
                    this.AddView(menu, new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, 1));
            }
        }

        public LinearLayout CreateMenuPopup(params MenuIconCollection[] menus)
        {
            var layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;
            layout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            layout.SetBackgroundColor(new Color(0xf3ebeb));

            foreach (var menu in menus)
                layout.AddView(menu, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

            return layout;
        }

        public void ShowPopup(string title, Action<MenuIcon> OnSelect, params MenuIconCollection[] menus)
        {
            var dialog = CreatePopup(title, CreateMenuPopup(menus), null, null, null, null);
            foreach (var menu in menus)
            {
                int childCount = menu.ChildCount;
                for (int i = 0; i < childCount; i++)
                {
                    var menuIcon = menu.GetChildAt(i) as MenuIcon;
                    if (menuIcon != null)
                        menuIcon.Click += (sender, e) =>
                        {
                            dialog.Dismiss();
                            if (OnSelect != null) OnSelect.Invoke(menuIcon);
                        };
                }
            }
            dialog.Show();
        }

    }

    public sealed class OnDismissListener : Java.Lang.Object, IDialogInterfaceOnDismissListener
    {
        public Action Action;
        public OnDismissListener(Action onDismiss)
        {
            Action = onDismiss;
        }
        public void OnDismiss(IDialogInterface dialog)
        {
            if (Action != null)
                Action.Invoke();
        }
    }
}