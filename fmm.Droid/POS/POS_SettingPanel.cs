using System;

using Android.OS;
using Android.Views;
using Android.Widget;

namespace fmm
{
    public abstract class POS_SettingPanel : Activity 
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.POS_SettingPanel);
            FindViewById(Resource.Id.Send).Click += (sender, e) => Process();
        }

        protected void Initialize(string title, int iconResId, int contentResId)
        {
            FindViewById<TextView>(Resource.Id.Title).Text = title;
            FindViewById<ImageView>(Resource.Id.HeaderImage).SetImageResource(iconResId);
            FindViewById<LinearLayout>(Resource.Id.POS_PanelContent).AddView(LayoutInflater.From(this).Inflate(contentResId, null));
        }

        protected abstract void Process();
    }
}