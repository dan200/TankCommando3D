using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dan200.Core.Interfaces;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;

namespace Dan200.Core.Level
{
    internal abstract class Component<TComponentData> : ComponentBase
        where TComponentData : struct
    {
        protected override sealed void OnInit(LuaTable properties)
        {
            OnInit(LONSerialiser.Parse<TComponentData>(properties));
        }

        protected abstract void OnInit(in TComponentData properties);
    }

    internal abstract class EditableComponent<TComponentData> : Component<TComponentData>, IEditable
        where TComponentData : struct
    {
        public void ReInit(LuaTable properties)
        {
            ReInit(LONSerialiser.Parse<TComponentData>(properties));
        }

        protected abstract void ReInit(in TComponentData properties);
    }
}
