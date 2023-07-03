using System;
using System.Reflection;

namespace Dan200.Core.Lua
{
    [LuaType("object")]
    internal abstract class LuaObject : IDisposable
    {
        public static string GetTypeName(Type t)
        {
            var attribute = t.GetCustomAttribute<LuaTypeAttribute>();
            if (attribute != null && attribute.CustomName != null)
            {
                return attribute.CustomName;
            }
            return t.Name;
        }

        private int m_refCount;

        public string TypeName
        {
            get
            {
                return GetTypeName(GetType());
            }
        }

        protected LuaObject()
        {
            m_refCount = 0;
        }

        public abstract void Dispose();

        public int Ref()
        {
            return ++m_refCount;
        }

        public int UnRef()
        {
            return --m_refCount;
        }

        public override string ToString()
        {
            return string.Format("{0}: 0x{1:x8}", TypeName, GetHashCode());
        }

        [LuaMethod]
        public LuaArgs getType(in LuaArgs args)
        {
            return new LuaArgs(TypeName);
        }
    }
}

