using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ChatAPI;
namespace fmm
{
    [Activity]
    public class SearchableContactActivity : Activity
    {
        public static List<SelectableContact> Items;
        private SearchableContactAdapter m_adapter;
        private bool m_selectAll;
        private bool m_pendingLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_SearchableContact);
            ThemedResId = new[] { Resource.Id.ActivityHeader };

            var selectAll = FindViewById<ImageView>(Resource.Id.SelectAll);
            if (Intent != null)
                selectAll.Visibility = Intent.GetBooleanExtra("select_all_visible", true) ? ViewStates.Visible : ViewStates.Gone;
            selectAll.Click += SelectAll_Click;
            if (savedInstanceState != null)
                m_selectAll = savedInstanceState.GetBoolean("select_all", false);
            selectAll.SetImageResource(m_selectAll ? Resource.Drawable.select_all : Resource.Drawable.unselect_all);

            FindViewById(Resource.Id.Send).Click += Send;
            FindViewById<EditText>(Resource.Id.tbSearch).TextChanged += TextChanged;

            var del = FindViewById<ImageView>(Resource.Id.Delete);
            del.Click += Delete_Click;
            del.SetImageResource(Resource.Drawable.ic_search);
            del.SetBackgroundColor(Core.Setting.Themes);

            var listView = FindViewById<ListView>(Resource.Id.ListViewer);
            listView.Divider = new Android.Graphics.Drawables.ColorDrawable(new Android.Graphics.Color(150, 150, 150));
            listView.DividerHeight = 1;
            m_adapter = new SearchableContactAdapter(Items);
            listView.Adapter = m_adapter;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean("select_all", m_selectAll);
            base.OnSaveInstanceState(outState);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            FindViewById<EditText>(Resource.Id.tbSearch).Text = null;
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            m_pendingLayout = true;
            m_selectAll = !m_selectAll;
            FindViewById<ImageView>(Resource.Id.SelectAll).SetImageResource(m_selectAll ? Resource.Drawable.select_all : Resource.Drawable.unselect_all);
            var tbSearch = FindViewById<EditText>(Resource.Id.tbSearch);
            tbSearch.Text = null;

            foreach (var item in Items)
                item.Selected = m_selectAll;

            m_pendingLayout = false;
            RefreshHeader(tbSearch);
        }

        private void Send(object sender, EventArgs e)
        {
            SetResult(Result.Ok, this.Intent);
            Finish();
        }

        private void RefreshHeader(EditText tbSearch)
        {
            var del = FindViewById<ImageView>(Resource.Id.Delete);
            var text = tbSearch.Text.Trim();
            if (text.Length == 0)
            {
                del.SetImageResource(Resource.Drawable.ic_search);
                del.SetBackgroundColor(Core.Setting.Themes);
            }
            else
            {
                del.SetImageResource(Resource.Drawable.ic_cross);
                del.SetBackgroundDrawable(null);
            }
            m_adapter.Search(text);
        }

        private void TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (!m_pendingLayout)
                RefreshHeader((EditText)sender);
        }

        public static void CreateItems()
        {
            if (Items == null)
            {
                Items = new List<SelectableContact>();
                var contacts = Core.GetSortedContacts();
                foreach (var c in contacts)
                    if (c.IsActive && !c.IsServerPOS)
                        Items.Add(new SelectableContact(c));
            }
        }
    }
}