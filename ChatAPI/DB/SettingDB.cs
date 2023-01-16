using System;

#if __ANDROID__ // Android
using Color = Android.Graphics.Color;
#else // Desktop
using Color = System.Drawing.Color;
#endif

namespace ChatAPI
{
    public class SettingDB : StringDB
    {
        public SettingDB(string fileName) : base(fileName, StringEncoder.UTF16)
        {
#if BUILD_PARTNER
            ThemesDefault = UIUtil.CreateColor(-14342816);
#else
            ThemesDefault = UIUtil.CreateColor(-2117632);
#endif
            var c = Read("themes"); // Cached for fast read
            m_themes = string.IsNullOrEmpty(c) ? ThemesDefault : UIUtil.CreateColor(Convert.ToInt32(c));
        }

        private new int IndexOf(string key)
        {
            int index = 0;
            int count = Count - 1;
            while (index < count)
            {
                if (string.Equals(key, this[index]))
                    return index;
                index += 2;
            }
            return -1;
        }

        public string Read(string key)
        {
            int index = IndexOf(key);
            if (index < 0)
                return null;

            return base[index + 1];
        }

        public void Write(string key, string value)
        {
            int index = IndexOf(key);
            if (index < 0)
                index = (Count & -2);

            UpdateOrAdd(index, key);
            UpdateOrAdd(index + 1, value);
        }

        public new void Remove(string key)
        {
            int index = IndexOf(key);
            if (index >= 0)
            {
                RemoveAt(index + 1);
                RemoveAt(index);
            }
        }

        public Language Language
        {
            get
            {
                return Read("Language") == "1" ? Language.EN : Language.ID;
            }
            set
            {
                Write("Language", value == Language.EN ? "1" : "0");
            }
        }

        public bool IsLanguageSpecified
        {
            get
            {
                return !string.IsNullOrEmpty(Read("Language"));
            }
        }

        public int FontSize
        {
            get
            {
                var sz = Read("FontSize");
                uint szInt;
                if (string.IsNullOrEmpty(sz) || !uint.TryParse(sz, out szInt) || szInt > 3)
                    return 1; // DefaultValue=MEDIUM
                return (int)szInt;
            }
            set
            {
                if (value < 0 || value > 3)
                    value = 1; // DefaultValue=MEDIUM
                Write("FontSize", value.ToString());
            }
        }

        public readonly Color ThemesDefault;
        private Color m_themes;
        public Color Themes
        {
            get
            {
                return m_themes;
            }
            set
            {
                m_themes = value;
                Write("themes", value.ToArgb().ToString());
            }
        }

        public Color ColorWallpaper
        {
            get
            {
                var c = Read("ColorWallpaper");
                return string.IsNullOrEmpty(c) ? Color.WhiteSmoke : UIUtil.CreateColor(Convert.ToInt32(c));
            }
            set
            {
                Write("ColorWallpaper", value.ToArgb().ToString());
            }
        }

        public string ChatWallpaper
        {
            get
            {
                return Read("ChatWallpaper");
            }
            set
            {
                Write("ChatWallpaper", value);
            }
        }

        public bool ChatSecure
        {
            get
            {
                var v = Read("ChatSecure");
                return ((object)v == null || v.Length != 1 || v[0] != '0');
            }
            set
            {
                Write("ChatSecure", value ? "1" : "0");
            }
        }

#if __ANDROID__ // Android
        public bool Vibrate
        {
            get
            {
                var v = Read("Vibrate");
                return ((object)v == null || v.Length != 1 || v[0] != '0');
            }
            set
            {
                Write("Vibrate", value ? "1" : "0");
            }
        }
#endif
        public ActivationState ActivationState
        {
            get
            {
                var v = Read("ActivationState");
                byte b;
                byte.TryParse(v, out b);
                return (ActivationState)b;
            }
            set
            {
                Write("ActivationState", ((byte)value).ToString());
            }
        }

        public DateTime LastAskPassword
        {
            get
            {
                var v = Read("LAP");
                long t;
                long.TryParse(v, out t);
                return new DateTime(t);
            }
            set
            {
                Write("LAP", value.Ticks.ToString());
            }
        }
        public bool ShouldAskPassword(TimeSpan duration)
        {
            return (ActivationState == ActivationState.ACTIVATED && Core.Owner != null
                && Core.CryptoKeyPassword != null && Core.CryptoKeyPassword.Length > 0
                && LastAskPassword + duration < DateTime.Now);
        }

        public DateTime LastRemindSecurityQuestion
        {
            get
            {
                var v = Read("LSQ");
                long t;
                long.TryParse(v, out t);
                return new DateTime(t);
            }
            set
            {
                Write("LSQ", value.Ticks.ToString());
            }
        }
        public bool ShouldRemindSecurityQuestion()
        {
            return (ActivationState == ActivationState.ACTIVATED && Core.Owner != null
                && Core.CryptoKeyPassword != null && Core.CryptoKeyPassword.Length > 0
                && LastRemindSecurityQuestion + TimeSpan.FromDays(27) < DateTime.Now);
        }
    }
}