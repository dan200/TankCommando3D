using System;

namespace Dan200.Core.Lua
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class LuaMethodAttribute : Attribute
    {
        public readonly string CustomName;

        public LuaMethodAttribute(string customName = null)
        {
            CustomName = customName;
        }
    }
}

