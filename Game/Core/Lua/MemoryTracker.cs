using Dan200.Core.Main;
using System;

namespace Dan200.Core.Lua
{
    internal class MemoryTracker
    {
        private long m_totalMemory;
        private long m_usedMemory;
        private Action m_gcAction;

        public long TotalMemory
        {
            get
            {
                return m_totalMemory;
            }
            set
            {
				App.Assert(value >= 0);
                m_totalMemory = value;
            }
        }

        public long UsedMemory
        {
            get
            {
                return m_usedMemory;
            }
        }

        public long FreeMemory
        {
            get
            {
				return System.Math.Max(m_totalMemory - m_usedMemory, 0);
            }
        }

        public MemoryTracker(long totalMemory, Action gcAction = null)
        {
			App.Assert(totalMemory >= 0);
            m_totalMemory = totalMemory;
            m_usedMemory = 0;
            m_gcAction = gcAction;
        }

        public void ForceAlloc(long bytes)
        {
            App.Assert(bytes >= 0);
            m_usedMemory += bytes;
        }

        public bool Alloc(long bytes, bool gc = true)
        {
            App.Assert(bytes >= 0);
            if (m_usedMemory + bytes <= m_totalMemory)
            {
                m_usedMemory += bytes;
                return true;
            }
            else
            {
                if (gc && m_gcAction != null)
                {
                    m_gcAction.Invoke();
                    if (m_usedMemory + bytes <= m_totalMemory)
                    {
                        m_usedMemory += bytes;
                        return true;
                    }
                }
                return false;
            }
        }

        public void Free(long bytes)
        {
            App.Assert(bytes >= 0 && bytes <= m_usedMemory);
            m_usedMemory = m_usedMemory - bytes;
        }
    }
}
