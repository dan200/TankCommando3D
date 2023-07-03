using System;
using System.Threading;
using Dan200.Core.Main;

namespace Dan200.Core.Util
{
    // A fixed size queue that is thread safe and doesn't lock,
    // so long as there is only one reader thread and one writer thread
    internal class LockFreeQueue<T>
    {
        private readonly T[] m_queue;
        private volatile int m_queueStart;
        private volatile int m_queueEnd;

        public LockFreeQueue(int capacity)
        {
            App.Assert(capacity > 0);
            m_queue = new T[capacity];
            m_queueStart = 0;
            m_queueEnd = 0;
        }

        public bool Enqueue(in T value)
        {
            int start = m_queueStart;
            int end = m_queueEnd;
            int capacity = m_queue.Length;
            if (end - start < capacity)
            {
                m_queue[end % capacity] = value;
                Interlocked.Increment(ref m_queueEnd);
                return true;
            }
            return false;
        }

        public bool Peek(out T o_value)
        {
            int start = m_queueStart;
            int end = m_queueEnd;
            if (end > start)
            {
                int capacity = m_queue.Length;
                o_value = m_queue[start % capacity];
                return true;
            }
            o_value = default(T);
            return false;
        }

        public bool Dequeue()
        {
            int start = m_queueStart;
            int end = m_queueEnd;
            if(end > start)
            {
                Interlocked.Increment(ref m_queueStart);
                return true;
            }
            return false;
        }

        public bool Dequeue(out T o_value)
        {
            int start = m_queueStart;
            int end = m_queueEnd;
            if (end > start)
            {
                int capacity = m_queue.Length;
                o_value = m_queue[start % capacity];
                Interlocked.Increment(ref m_queueStart);
                return true;
            }
            o_value = default(T);
            return false;
        }
    }
}
