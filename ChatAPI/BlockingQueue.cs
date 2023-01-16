using System;
using System.Threading;

namespace ChatAPI
{
    public class BlockingQueue<T> : BaseQueue<T>
    {
        private bool m_quit;

        public bool IsQuitted
        {
            get
            {
                return m_quit;
            }
        }

        public override bool Dequeue(ref T item)
        {
            lock (this)
            {
                while (!m_quit && m_size == 0)
                {
                    Monitor.Wait(this);
                }
                if (base.Dequeue(ref item))
                {
                    Monitor.PulseAll(this);
                    return true;
                }
                return false;
            }
        }

        public override bool Enqueue(T item)
        {
            lock (this)
            {
                if (m_quit)
                {
                    return false;
                }
                base.Enqueue(item);
                Monitor.PulseAll(this);
            }
            return true;
        }

        public bool PeekAll(ref T[] items)
        {
            lock (this)
            {
                while (!m_quit && m_size == 0)
                {
                    Monitor.Wait(this);
                }
                if (m_size == 0)
                {
                    return false;
                }
                items = new T[m_size];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = m_array[(i + m_head) % m_array.Length];
                }
            }
            return true;
        }

        public bool InternalRemove(int count)
        {
            lock (this)
            {
                if (m_size < count)
                {
                    return false;
                }
                while (count > 0)
                {
                    m_array[m_head] = default(T);
                    m_head = (m_head + 1) % m_array.Length;
                    m_size--;
                    count--;
                }
                Monitor.PulseAll(this);
            }
            return true;
        }

        public bool WaitEmpty()
        {
            lock (this)
            {
                while (!m_quit && m_size > 0)
                {
                    Monitor.Wait(this);
                }
            }
            return !m_quit;
        }

        public void Quit()
        {
            lock (this)
            {
                m_quit = true;
                Monitor.PulseAll(this);
            }
        }
    }
}
