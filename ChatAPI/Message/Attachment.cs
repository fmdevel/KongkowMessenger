using System;
using System.IO;

namespace ChatAPI
{
    public class Attachment
    {
        public readonly string FileName;
        public static readonly Attachment Empty = new Attachment(null); // Empty Attachment

        public Attachment(string fileName)
        {
            this.FileName = Util.GuardValue(fileName);
        }

        public Attachment(string fileType, byte[] content)
        {
            if (content != null && content.Length > 0)
            {
                this.FileName = Path.Combine(Core.PublicDataDir, (DateTime.Now.Ticks + 1).ToString() + fileType);
                File.WriteAllBytes(this.FileName, content);
            }
            else
                this.FileName = Util.GuardValue(fileType);
        }

        //public Attachment(string fileType, Stream content)
        //{
        //    if (content != null)
        //    {
        //        this.FileName = Path.Combine(Core.PublicDataDir, (DateTime.Now.Ticks + 1).ToString() + fileType);
        //        using (var fs = new FileStream(FileName, FileMode.Create))
        //        {
        //            const int bufferSize = 4 * 1024;
        //            var buffer = new byte[bufferSize];
        //            while (true)
        //            {
        //                var count = content.Read(buffer, 0, bufferSize);
        //                if (count <= 0)
        //                    break;

        //                fs.Write(buffer, 0, count);
        //            }
        //        }
        //    }
        //    else
        //        this.FileName = Util.GuardValue(fileType);
        //}

        public static Attachment Deserialize(Deserializer s)
        {
            string fileType = null;
            s.Extract(ref fileType);
            byte[] content = null;
            s.Extract(ref content);
            if (!string.IsNullOrEmpty(fileType))
                return new Attachment(fileType, content);

            return Attachment.Empty;
        }
    }
}