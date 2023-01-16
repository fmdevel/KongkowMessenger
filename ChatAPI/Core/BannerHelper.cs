#if BUILD_PARTNER
using System.IO;

namespace ChatAPI
{
    public static partial class Core
    {
        internal static string GetBannerFile(int index)
        {
            return Path.Combine(m_rootDB, "banner" + index.ToString());
        }

        private static int[] m_bannerHash;
        private static string[] m_bannerUrl;
        private unsafe static void LoadBanner()
        {
            const int MAX_BANNER = 5;
            m_bannerHash = new int[MAX_BANNER];
            m_bannerUrl = new string[MAX_BANNER];

            for (int i = 0; i < MAX_BANNER; i++)
            {
                var infoFile = GetBannerFile(i);
                if (File.Exists(infoFile))
                {
                    var raw = File.ReadAllBytes(infoFile);
                    if (raw.Length > 4)
                    {
                        fixed (byte* pRaw = raw)
                            m_bannerHash[i] = *((int*)pRaw);

                        m_bannerUrl[i] = StringEncoder.UTF8.GetString(raw, 4, raw.Length - 4);
                    }
                }
            }
        }

        internal unsafe static bool SetBanner(int index, string url, byte[] rawImage)
        {
            const int MAX_BANNER = 5;
            if (index < 0 || index >= MAX_BANNER)
                return false; // No new banner

            var infoFile = GetBannerFile(index);
            var imageFile = infoFile + "img";
            if (string.IsNullOrEmpty(url))
            {
                if (File.Exists(infoFile))
                    File.Delete(infoFile);
                if (File.Exists(imageFile))
                    File.Delete(imageFile);

                m_bannerHash[index] = 0;
                m_bannerUrl[index] = null;
                return false; // No new banner
            }

            bool update = false;
            if (rawImage != null && rawImage.Length > 0)
            {
                m_bannerHash[index] = Crypto.hashV2(rawImage);
                File.WriteAllBytes(imageFile, rawImage);
                update = true;
            }

            m_bannerUrl[index] = url;
            var raw = new byte[4 + url.Length];
            fixed (byte* pRaw = raw)
                *((int*)pRaw) = m_bannerHash[index];

            StringEncoder.UTF8.Copy(url, 0, raw, 4, url.Length);
            File.WriteAllBytes(infoFile, raw);

            return update;
        }

        private static void SyncBanner()
        {
            if (ContactPOS.Current == null)
                return;

            var respBuf = new NSerializer(TypeHeader.BANNER_SYNC);
            respBuf.Add(ContactPOS.Current.ID);
            for (int i = 0; i < m_bannerUrl.Length; i++)
            {
                respBuf.Add(Util.GuardValue(m_bannerUrl[i]));
                respBuf.Add(m_bannerHash[i]);
            }
            Network.Send(respBuf);
        }

        public class Banner
        {
            public string Url;
            public string FileName;
        }

        public static Banner[] GetActiveBanners()
        {
            if (m_bannerUrl == null)
                return null;

            int i, count = 0;
            for (i = 0; i < m_bannerUrl.Length; i++)
            {
                if (!string.IsNullOrEmpty(m_bannerUrl[i]) && m_bannerHash[i] != 0)
                    count++;
            }
            if (count == 0)
                return null;

            var banners = new Banner[count];
            count = 0;
            for (i = 0; i < m_bannerUrl.Length; i++)
            {
                if (!string.IsNullOrEmpty(m_bannerUrl[i]) && m_bannerHash[i] != 0)
                {
                    var b = new Banner();
                    b.Url = m_bannerUrl[i];
                    b.FileName = GetBannerFile(i) + "img";
                    banners[count] = b;
                    count++;
                }
            }
            return banners;
        }
    }
}

#endif