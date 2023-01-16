using System;

namespace ChatAPI
{
    public static partial class Network
    {
        public class TransmitQueue
        {
            public NSerializer buf;
            public int timeOut;
            public IProgress progress;

            public TransmitQueue(NSerializer buf, int timeOut, IProgress progress)
            {
                this.timeOut = timeOut;
                this.buf = buf;
                this.progress = progress;
            }
        }
    }
}