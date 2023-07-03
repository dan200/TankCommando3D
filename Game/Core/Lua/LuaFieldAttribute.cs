using System;

namespace Dan200.Core.Lua
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class LuaFieldAttribute : Attribute
    {
        public readonly string CustomName;

        public LuaFieldAttribute(string customName = null)
        {
            CustomName = customName;
        }
    }
}

