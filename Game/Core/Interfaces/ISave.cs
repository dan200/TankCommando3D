using Dan200.Core.Level;
using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Interfaces
{
    internal interface ISave : IInterface
    {
        void Save(LuaTable properties);
    }
}
