using System;

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using ChatAPI;

namespace fmm
{
    [Activity()]
    public class ColorPickerActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Kongkow_ColorPicker);
            var mode = Intent.GetIntExtra("mode", 0);
            ThemedResId = new[] { Resource.Id.ActivityHeader };
            var TableColorSolid = FindViewById<LinearLayout>(Resource.Id.TableColor);
            var par = new LinearLayout.LayoutParams(-1, -1, 1.0f);
            var colorTable = CreateTable();
            for (int r = 0; r < 24; r++)
            {
                var tr = new LinearLayout(this);
                tr.Orientation = Orientation.Horizontal;
                for (int c = 0; c < 9; c++)
                {
                    var tv = new View(this);
                    tv.Click += tv_click;
                    var color = colorTable[c][r];
                    if (mode == 0)
                        color = UIUtil.Highlight(color);
                    tv.SetBackgroundColor(color);
                    tv.Tag = color.ToArgb();
                    tr.AddView(tv, par);
                }
                TableColorSolid.AddView(tr, par);
            }
        }

        private static Color[][] CreateTable()
        {
            const int min = 32;
            const int max = 192;
            const int deltaY = 16;
            const int gradientCount = 5;

            var row = new Color[(int)Math.Ceiling((double)(max - (min * 2) + 1) / deltaY)][];
            int Y = max, rowIndex = 0;
            while (Y >= min * 2)
            {
                var col = new Color[(gradientCount - 1) * 6];
                float d = (float)(Y - min) / (gradientCount - 1);
                int i, colIndex = 0;
                for (i = 0; i < gradientCount; i++)
                    col[colIndex++] = new Color(Y, min + (int)Math.Round(d * i), min);

                for (i = gradientCount - 2; i >= 1; i--)
                    col[colIndex++] = new Color(min + (int)Math.Round(d * i), Y, min);

                for (i = 0; i < gradientCount; i++)
                    col[colIndex++] = new Color(min, Y, min + (int)Math.Round(d * i));

                for (i = gradientCount - 2; i >= 1; i--)
                    col[colIndex++] = new Color(min, min + (int)Math.Round(d * i), Y);

                for (i = 0; i < gradientCount; i++)
                    col[colIndex++] = new Color(min + (int)Math.Round(d * i), min, Y);

                for (i = gradientCount - 2; i >= 1; i--)
                    col[colIndex++] = new Color(Y, min, min + (int)Math.Round(d * i));

                row[rowIndex++] = col;
                Y -= deltaY;
            }
            return row;
        }

        private void tv_click(object sender, EventArgs e)
        {
            var v = (View)sender;
            var color = new Color((int)(v.Tag));
            if (Intent.GetIntExtra("mode", 0) == 0)
            {
                Core.Setting.ColorWallpaper = color;
                Core.Setting.ChatWallpaper = string.Empty;
                Toast.MakeText(this, Resources.GetString(Resource.String.WallpaperSet), ToastLength.Short).Show();
            }
            else
            {
                Core.Setting.Themes = color;
                Toast.MakeText(this, Resources.GetString(Resource.String.ThemesSet), ToastLength.Short).Show();
            }
            Finish();
        }
    }
}