using System;
using Android.Views;
using Android.Widget;
using ChatAPI;

namespace fmm
{
    public class SimpleSelectedContactAdapter : IListAdapter<SelectableContact>
    {
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.Kongkow_SimpleSelectedItem, null);

            convertView.FindViewById<TextView>(Resource.Id.Text).Text = this.Items[position].Contact.Name;
            var del = convertView.FindViewById<ImageView>(Resource.Id.Delete);
            del.Tag = position;
            del.Click -= Delete_Click;
            del.Click += Delete_Click;
            return convertView;
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var position = (int)(((View)sender).Tag);
            var item = base.Items[position];
            item.Selected = false;
            base.Items.RemoveAt(position);
            NotifyDataSetChanged();
        }
    }
}