using Dan200.Core.Lua;
using Dan200.Core.Util;
using System;
using System.Reflection;

namespace Dan200.Core.Lua
{
    internal abstract class LuaAPI
    {
        private ByteString m_name;

        public ByteString Name
        {
            get
            {
                return m_name;
            }
        }

        protected LuaAPI(string name)
        {
            m_name = new ByteString(name);
        }

        public virtual void Install(LuaMachine machine)
        {
            machine.SetGlobal(Name, GetMethodTable());
        }

        protected LuaTable GetMethodTable()
        {
            var type = GetType();
            var result = new LuaTable();

            MethodInfo[] methods = type.GetMethods();
            for (int i = 0; i < methods.Length; ++i)
            {
                var method = methods[i];
                var name = method.Name;
                var attribute = method.GetCustomAttribute<LuaMethodAttribute>();
                if (attribute != null)
                {
                    if (attribute.CustomName != null)
                    {
                        name = attribute.CustomName;
                    }
                    result[name] = (LuaCFunction)Delegate.CreateDelegate(typeof(LuaCFunction), this, method);
                }
            }

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; ++i)
            {
                var field = fields[i];
                var name = field.Name;
                var attribute = field.GetCustomAttribute<LuaFieldAttribute>();
                if (attribute != null)
                {
                    if (attribute.CustomName != null)
                    {
                        name = attribute.CustomName;
                    }
                    result[name] = (LuaValue)field.GetValue(this);
                }
            }

            return result;
        }
    }
}
