using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ChatAPI
{
    public partial class Client
    {
        internal class TcpSocket : IDisposable
        {
            private Socket m_sock;
            private bool m_isConnected;

            private void DoConnect(object obj)
            {
                var param = obj as object[];
                try
                {
                    m_sock.Connect((IPAddress)param[0], (int)param[1]);
                    if (m_sock.Connected)
                        ((AutoResetEvent)param[2]).Set();

                    return;
                }
                catch { }
                Dispose();
            }

            private void DoConnect(IPAddress address, int port, int timeout, AutoResetEvent wait)
            {
                if (wait == null) wait = new AutoResetEvent(false);
                m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    var t = new Thread(DoConnect);
                    t.IsBackground = true;
                    t.Start(new object[] { address, port, wait });

                    if (wait.WaitOne(timeout, true) && m_sock.Connected)
                    {
                        m_sock.NoDelay = true;
                        m_sock.ReceiveBufferSize = 65535;
                        m_sock.SendTimeout = 2700; //5500;
                        m_isConnected = true;
                        return;
                    }
                }
                catch { }
                Dispose();
            }

            internal int Read(byte[] buffer)
            {
                return m_sock.Receive(buffer);
            }

            //internal void Write(byte[] buffer, int index, int count)
            //{
            //    while (count > 0)
            //    {
            //        SocketError error;
            //        int num = m_sock.Send(buffer, index, Math.Min(count, 8192), SocketFlags.None, out error);
            //        if (error != SocketError.Success)
            //        {
            //            throw new SocketException();
            //        }
            //        index += num;
            //        count -= num;
            //    }
            //}

            internal void Write(byte[] buffer, int index, int count, IProgress progress)
            {
                int interval = 0;
                int pCount = count;
                while (count > 0)
                {
                    SocketError error;
                    int num = m_sock.Send(buffer, index, Math.Min(count, 8192), SocketFlags.None, out error);
                    if (error != SocketError.Success)
                    {
                        throw new SocketException();
                    }
                    index += num;
                    count -= num;

                    if (progress != null)
                    {
                        if ((interval & 3) == 0 || index == pCount)
                        {
                            try
                            {
                                progress.SetProgress(true, index, pCount);
                            }
                            catch { }
                        }
                        interval++;
                    }
                }
            }

            internal bool IsConnected()
            {
                return m_isConnected && m_sock != null;
            }

            internal bool Connect(IPAddress address, int port, int timeout, AutoResetEvent wait)
            {
                try
                {
                    DoConnect(address, port, timeout, wait);
                }
                catch
                {
                    Dispose();
                }
                return IsConnected();
            }

            public void Dispose()
            {
                var sck = m_sock;
                if (sck != null)
                {
                    m_sock = null;
                    try
                    {
                        sck.Close();
                    }
                    catch { }
                }
            }

            ~TcpSocket()
            {
                Dispose();
            }
        }
    }
}
