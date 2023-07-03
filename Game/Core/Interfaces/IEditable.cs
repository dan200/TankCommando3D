using System;
using Dan200.Core.Level;
using Dan200.Core.Lua;

namespace Dan200.Core.Interfaces
{
    internal interface IEditable : IComponentInterface
    {
        void ReInit(LuaTable properties);
    }
}
