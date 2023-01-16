using System;
using System.Threading;

namespace ChatAPI
{
    public class SyncWait
    {
        private ManualResetEvent m_event = new ManualResetEvent(false);
        private EventWaitHandle m_syncEvent;
        private volatile int m_state; // 1=Active

        public void Set()
        {
            m_event.Set();
            var e = m_syncEvent;
            if (e != null)
            {
                m_syncEvent = null;
                e.Set();
            }
        }

        public void SetSync(EventWaitHandle wait) { m_syncEvent = wait; }

        public int State
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
            }
        }

        public bool Active
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get
            {
                return m_state == 1;
            }
            set
            {
                if (value)
                {
                    m_event.Reset();
                    m_state = 1;
                }
                else
                    m_state = 0;
            }
        }

        public bool Wait(int millisecods)
        {
            if (m_state != 1) return true;
            return m_event.WaitOne(millisecods, false);
        }
    }
}
