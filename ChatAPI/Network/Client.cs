using System;
using System.Net;
using System.Threading;

namespace ChatAPI
{
    public partial class Client
    {
        internal class Queue
        {
            internal byte[] data;
            internal int count;
            internal IProgress progress;

            internal Queue(byte[] data, int count, IProgress progress)
            {
                this.data = data;
                this.count = count;
                this.progress = progress;
            }

            internal Queue(TypeHeader type)
            {
                this.data = new byte[] { 0, 0, 0, 0, (byte)type }; // Alloc first 4-bytes for NetworkHeader
                Client.SetHeader(this.data, 1); // real data count
                this.count = 5;
                this.progress = null;
            }

            internal void Write(TcpSocket sock)
            {
                sock.Write(data, 0, count, progress);
            }
        }

        public delegate void DataReceived(byte[] data, int count);

        private TcpSocket m_sock;
        private BlockingQueue<Queue> m_qSend;
        private PacketBuffer m_packetBuffer;
        private DataReceived m_receiver;
        public Action<bool> OnConnectionStateChanged;

        public Client(DataReceived receiver)
        {
            m_sock = new TcpSocket();
            m_packetBuffer = new PacketBuffer(1024 * 32);
            m_receiver = receiver;
        }

        public bool IsConnected
        {
            get
            {
                return m_sock.IsConnected();
            }
        }

        public void Connect(IPAddress address, int port, int timeout, AutoResetEvent wait)
        {
            Connect(address, port, timeout, true, wait);
        }

        internal void Connect(IPAddress address, int port, int timeout, bool queueSend, AutoResetEvent wait)
        {
            if (!IsConnected)
            {
                m_packetBuffer.count = 0; // reset count
                if (queueSend) m_qSend = new BlockingQueue<Queue>();

                if (m_sock.Connect(address, port, timeout, wait))
                {
                    Util.StartThread(DoReceive);
                    if (queueSend) Util.StartThread(DoSend);
                    if (OnConnectionStateChanged != null)
                        OnConnectionStateChanged.Invoke(true);
                }
            }
        }

        private unsafe void DoReceive()
        {
            byte[] buffer = new byte[1024 * 32];
            int remainBytes = 0;
            try
            {
                for (;;)
                {
                    int count = m_sock.Read(buffer);
                    if ((count <= 0) || (!IsConnected))
                        break;

                    var packet = m_packetBuffer;
                    packet.AddRange(buffer, count);
                    if (remainBytes > 0)
                        goto Parse_Remain;

                    if (packet.count < 4)
                        continue; // Buffer is too small

                    Parse_New_Packet:
                    int packetLength;
                    fixed (byte* pData = packet.data)
                        packetLength = *(int*)(pData);

                    packet.RemoveRange(4);
                    remainBytes = -packetLength;

                Parse_Remain:
                    if (packet.count >= remainBytes)
                    {
                        if (m_receiver != null)
                            m_receiver.Invoke(packet.data, remainBytes);
                        packet.RemoveRange(remainBytes);
                        remainBytes = 0;
                        if (packet.count >= 4)
                            goto Parse_New_Packet;
                    }
                }
            }
            catch { }
            Close();
        }

        private Queue[] m_allQueue;
        int m_qIndex;

        private void DoSend()
        {
            try
            {
                while (IsConnected)
                {
                    m_allQueue = null;
                    m_qIndex = 0;
                    if (!m_qSend.PeekAll(ref m_allQueue)) // Waiting data entering the queue...
                        break; // Another thread Quit the Queue, got Zero size or Empty Queue

                    m_qSend.InternalRemove(m_allQueue.Length); // Remove data that we got
                    if (!IsConnected) // Is socket still connected after long wait?
                        break; // Socket closed, damn!

                    do
                    {
                        m_allQueue[m_qIndex].Write(m_sock);
                        m_qIndex++;
                    } while (IsConnected && m_qIndex < m_allQueue.Length);
                }
            }
            catch { }
            Close();
        }

        internal static unsafe void SetHeader(byte[] data, int count)
        {
            fixed (byte* pData = data)
                *(int*)(pData) = -count;
        }

        internal bool Enqueue(Queue q)
        {
            return m_qSend.Enqueue(q);
        }

        internal void Write(Queue q)
        {
            q.Write(m_sock);
        }

        public void Close()
        {
            if (OnConnectionStateChanged != null)
                OnConnectionStateChanged.Invoke(false);

            if (IsConnected)
                m_sock.Dispose();

            if (m_qSend != null)
            {
                if (!m_qSend.IsQuitted)
                    m_qSend.Quit();

                NotifyQueueFailure();
                if (m_qSend.PeekAll(ref m_allQueue)) // Any remaining queue?
                    NotifyQueueFailure();
            }
        }

        private void NotifyQueueFailure()
        {
            var allQueue = m_allQueue;
            m_allQueue = null;
            var index = m_qIndex;
            m_qIndex = 0;
            if (allQueue != null && index < allQueue.Length)
            {
                do
                {
                    var q = allQueue[index];
                    if (q.progress != null)
                    {
                        try
                        {
                            q.progress.SetProgress(false, 0, 0);
                        }
                        catch { }
                    }
                } while (++index < allQueue.Length);
            }
        }
    }

}
