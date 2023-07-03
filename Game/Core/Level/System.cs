using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;

namespace Dan200.Core.Level
{
    internal abstract class System<TSystemData> : SystemBase where TSystemData : struct
    {
        protected sealed override void OnInit(LuaTable properties)
        {
            OnInit(LONSerialiser.Parse<TSystemData>(properties));
        }

        protected abstract void OnInit(in TSystemData properties);
    }
}
