using System;

namespace Dan200.Core.Lua
{
    internal class LuaError : Exception
    {
        public LuaValue Value; // TODO: Make me readonly again!
        public readonly int Level;

        public LuaError(string message, int level = 1) : base(message)
        {
            Value = new LuaValue(message);
            Level = level;
        }

        public LuaError(LuaValue message, int level = 1) : base(message.ToString())
        {
            Value = message;
            Level = level;
        }
    }
}
