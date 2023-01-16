using System;

namespace ChatAPI
{
    public class DP
    {
        public readonly string Full;
        public readonly int Hash;
#if __ANDROID__
        public int Update;
#endif

        internal DP(string dpFile, int hash)
        {
            this.Full = dpFile;
            this.Hash = hash;
        }

        public string Thumbnail
        {
            get
            {
                return Full + "t";
            }
        }

#if !__ANDROID__
        private System.Drawing.Image m_image;
        public System.Drawing.Image Image
        {
            get
            {
                if (m_image == null && System.IO.File.Exists(this.Thumbnail))
                    m_image = UIUtil.FromFile(this.Thumbnail);
                return m_image;
            }
        }
#endif

    }
}