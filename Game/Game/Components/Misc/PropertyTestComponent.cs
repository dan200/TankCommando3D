using System;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Serialisation;

namespace Dan200.Game.Components.Misc
{
    internal struct SubStruct
    {
        [Range(1,10)]
        public int RequiredRangedInt;
        [Optional(Default = 7)]
        [Range(1, 10)]
        public int OptionalRangedInt;
    }

    internal struct PropertyTestComponentData
    {
        public bool RequiredBool;
        public byte RequiredByte;
        public int RequiredInt;
        [Range(100, 200)]
        public int RequiredRangedInt;
        public float RequiredFloat;
        [Range(1.0f, 2.0f)]
        public float RequiredRangedFloat;
        public string RequiredString;
        public LuaTable RequiredLuaTable;
        public PropertyType RequiredEnum;
        public SubStruct RequiredStruct;
        public PropertyType[] EnumArray;
    }


    internal class PropertyTestComponent : EditableComponent<PropertyTestComponentData>
    {
        protected override void OnInit(in PropertyTestComponentData properties)
        {
        }

        protected override void Reset(in PropertyTestComponentData properties)
        {
        }

        protected override void OnShutdown()
        {
        }
    }
}
