using System;
using Android.Widget;

namespace fmm
{
    public abstract class IListAdapter<T> : BaseAdapter<T>
    {
        public static readonly T[] Empty = new T[0];
        public System.Collections.Generic.IList<T> Items = Empty;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override T this[int index]
        {
            get { return this.Items[index]; }
        }

        public override int Count
        {
            get { return this.Items.Count; }
        }
    }
}