using System;

namespace Dan200.Core.Lua
{
    internal struct LuaObjectRef<TObject> : IDisposable where TObject : LuaObject
    {
        private TObject m_value;

        public bool HasValue
        {
            get
            {
                return m_value != null;
            }
        }

        public TObject Value
        {
            get
            {
                return m_value;
            }
            set
            {
                if (m_value != value)
                {
                    var oldValue = m_value;
                    m_value = value;
                    if (m_value != null)
                    {
                        m_value.Ref();
                    }
                    if (oldValue != null)
                    {
                        if (oldValue.UnRef() == 0)
                        {
                            oldValue.Dispose();
                        }
                    }
                }
            }
        }

        public LuaObjectRef(TObject value)
        {
            m_value = value;
            if (m_value != null)
            {
                m_value.Ref();
            }
        }

        public void Dispose()
        {
            if (m_value != null)
            {
                if (m_value.UnRef() == 0)
                {
                    m_value.Dispose();
                }
                m_value = null;
            }
        }
    }
}
