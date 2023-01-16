using System;

namespace ChatAPI
{
    public class BaseQueue<T>
    {
        protected T[] m_array;
        protected int m_size;
        protected int m_head;
        protected int m_tail;

        public int Count
        {
            get
            {
                return m_size;
            }
        }

        public BaseQueue()
        {
            m_array = new T[4];
        }

        public virtual bool Dequeue(ref T item)
        {
            if (m_size == 0)
            {
                return false;
            }
            item = m_array[m_head];
            m_array[m_head] = default(T);
            m_head = (m_head + 1) % m_array.Length;
            m_size--;
            return true;
        }

        public virtual bool Enqueue(T item)
        {
            if (m_size == m_array.Length)
            {
                SetCapacity(m_array.Length * 2);
            }
            m_array[m_tail] = item;
            m_tail = (m_tail + 1) % m_array.Length;
            m_size++;
            return true;
        }

        protected void SetCapacity(int capacity)
        {
            if (capacity < 4)
            {
                capacity = 4;
            }
            T[] array = new T[capacity];
            if (m_size > 0)
            {
                if (m_head < m_tail)
                {
                    Array.Copy(m_array, m_head, array, 0, m_size);
                }
                else
                {
                    Array.Copy(m_array, m_head, array, 0, m_array.Length - m_head);
                    Array.Copy(m_array, 0, array, m_array.Length - m_head, m_tail);
                }
            }
            m_array = array;
            m_head = 0;
            m_tail = ((m_size == capacity) ? 0 : m_size);
        }
    }
}
