using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Util
{
    internal class Pool<T> where T : class, new()
    {
        internal struct Handle : IDisposable
        {
            private Pool<T> m_owner;
            private int m_index;

            public T Value
            {
                get
                {
                    App.Assert(m_owner.m_pool[m_index].InUse);
                    return m_owner.m_pool[m_index].Value;
                }
            }

            public Handle(Pool<T> owner, int index)
            {
                m_owner = owner;
                m_index = index;
            }

            public void Dispose()
            {
                m_owner.ReleaseEntry(m_index);
            }
        }

        private struct Entry
        {
            public bool InUse;
            public T Value;
        }

        private Entry[] m_pool;
        private int m_count;
        private int m_pos;

        public Pool() : this(16)
        {
        }

        public Pool(int capacity)
        {
            m_pool = new Entry[System.Math.Max(capacity, 1)];
            m_count = 0;
            m_pos = 0;
        }

        public Handle Fetch()
        {
            var index = FindFreeEntry();
            return new Handle(this, index);
        }

        private int FindFreeEntry()
        {
            // Expand to fit
            if(m_count == m_pool.Length)
            {
                Array.Resize(ref m_pool, m_count * 2);
            }

            // Starting at pos, find an entry not in-use
            var freeIndex = m_pos;
            var pool = m_pool;
            while (pool[freeIndex].InUse)
            {
                freeIndex = (freeIndex + 1) % pool.Length;
            }

            // Mark the entry as in use, allocating the object if necessary
            pool[freeIndex].InUse = true;
            if(pool[freeIndex].Value == null)
            {
                try
                {
                    pool[freeIndex].Value = new T();
                }
                catch (TargetInvocationException e)
                {
                    throw App.Rethrow(e.InnerException);
                }
            }
            m_count = m_count + 1;
            m_pos = freeIndex + 1;
            return freeIndex;
        }

        private void ReleaseEntry(int index)
        {
            // Mark the entry as no longer in use
            App.Assert(m_pool[index].InUse);
            m_pool[index].InUse = false;
            m_count = m_count - 1;
            m_pos = index;
        }
    }
}
