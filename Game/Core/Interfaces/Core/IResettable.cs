using System;
using Dan200.Core.Level;
using Dan200.Core.Lua;

namespace Dan200.Core.Interfaces.Core
{
    internal interface IResettable : IComponentInterface
    {
        void Reset(LuaTable properties);
    }
}
