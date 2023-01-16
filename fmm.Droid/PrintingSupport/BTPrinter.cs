using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Android.Graphics;

namespace fmm.PrintingSupport
{
    internal static class BTPrinter
    {

        private const string hexStr = "0123456789ABCDEF";
        private static string[] binaryArray = { "0000", "0001", "0010", "0011",
            "0100", "0101", "0110", "0111", "1000", "1001", "1010", "1011",
            "1100", "1101", "1110", "1111" };

        public static byte[] ESC_ALIGN_CENTER = new byte[] { 0x1b, (byte)'a', 0x01 };
        public static byte[] FEED_LINE = { 10 };

        public static void Print(Bitmap bitmap, Stream outputStream, int scaleWidth)
        {
            scaleWidth = scaleWidth & (int.MaxValue - 7);
            var b2 = UIUtil.ResizeImage(bitmap, scaleWidth, (scaleWidth * bitmap.Height) / bitmap.Width);
            if (bitmap != b2)
            {
                bitmap.Recycle();
                bitmap = b2;
            }
            Print(bitmap, outputStream);
        }

        public static void Print(Bitmap bitmap, Stream outputStream)
        {
            outputStream.Write(ESC_ALIGN_CENTER, 0, ESC_ALIGN_CENTER.Length);
            int y = 0;
            int count = bitmap.Height;
            while (count > 0)
            {
                int h = Math.Min(count, 64);
                byte[] command = decodeBitmap(bitmap, y, h);
                outputStream.Write(command, 0, command.Length);
                outputStream.Flush();
                System.Threading.Thread.Sleep(450);
                y += h;
                count -= h;
            }
            NewLine(outputStream, 5);
            outputStream.Flush();
        }

        private static void NewLine(Stream outputStream, int count)
        {
            while (count-- > 0)
                outputStream.Write(FEED_LINE, 0, FEED_LINE.Length);
        }

        private static byte[] decodeBitmap(Bitmap bmp, int y, int height)
        {
            int bmpWidth = bmp.Width;
            List<string> list = new List<string>();
            int zeroCount = bmpWidth & 7;
            StringBuilder sb = new StringBuilder();
            int count = height;
            do
            {
                for (int j = 0; j < bmpWidth; j++)
                {
                    int color = bmp.GetPixel(j, y);
                    int r = (color >> 16) & 0xff;
                    int g = (color >> 8) & 0xff;
                    int b = color & 0xff;
                    if (r > 160 && g > 160 && b > 160) sb.Append('0');
                    else sb.Append('1');
                }
                if (zeroCount > 0) sb.Append('0', 8 - zeroCount);
                list.Add(sb.ToString());
                sb.Clear();
                y++;
            } while (--count != 0);

            List<string> bmpHexList = binaryListToHexStringList(list);
            string commandHexString = "1D763000";
            string widthHexString = (bmpWidth % 8 == 0 ? bmpWidth / 8 : (bmpWidth / 8 + 1)).ToString("X2") + "00";
            string heightHexString = height.ToString("X2") + "00";

            List<string> commandList = new List<string>();
            commandList.Add(commandHexString + widthHexString + heightHexString);
            commandList.AddRange(bmpHexList);
            return hexList2Byte(commandList);
        }

        public static List<string> binaryListToHexStringList(List<string> list)
        {
            List<string> hexList = new List<string>();
            foreach (string binaryStr in list)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < binaryStr.Length; i += 8)
                {
                    string str = binaryStr.Substring(i, 8);

                    string hexString = strToHexString(str);
                    sb.Append(hexString);
                }
                hexList.Add(sb.ToString());
            }
            return hexList;
        }

        public static string strToHexString(string binaryStr)
        {
            string hex = string.Empty;
            string f4 = binaryStr.Substring(0, 4);
            string b4 = binaryStr.Substring(4, 4);
            for (int i = 0; i < binaryArray.Length; i++)
            {
                if (f4 == binaryArray[i])
                    hex += hexStr.Substring(i, 1);
            }
            for (int i = 0; i < binaryArray.Length; i++)
            {
                if (b4 == binaryArray[i])
                    hex += hexStr.Substring(i, 1);
            }

            return hex;
        }

        public static byte[] hexList2Byte(List<string> list)
        {
            var commandList = new List<byte[]>();

            foreach (string hexStr in list)
            {
                commandList.Add(hexStringToBytes(hexStr));
            }
            return sysCopy(commandList); ;
        }

        public static byte[] hexStringToBytes(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
            {
                return null;
            }
            hexString = hexString.ToUpper();
            int length = hexString.Length / 2;
            byte[] d = new byte[length];
            for (int i = 0; i < length; i++)
            {
                int pos = i * 2;
                d[i] = (byte)(charToByte(hexString[pos]) << 4 | charToByte(hexString[pos + 1]));
            }
            return d;
        }

        public static byte[] sysCopy(List<byte[]> srcArrays)
        {
            int len = 0;
            foreach (byte[] srcArray in srcArrays)
            {
                len += srcArray.Length;
            }
            byte[] destArray = new byte[len];
            int destLen = 0;
            foreach (byte[] srcArray in srcArrays)
            {
                Array.Copy(srcArray, 0, destArray, destLen, srcArray.Length);
                destLen += srcArray.Length;
            }
            return destArray;
        }

        private static int charToByte(char c)
        {
            return hexStr.IndexOf(c);
        }
    }
}