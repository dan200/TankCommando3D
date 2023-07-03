using System;
using Dan200.Core.Lua;

namespace Dan200.Core.Util
{
    internal static class LONMacros
    {
        public static LuaValue Vector2(in LuaArgs args)
        {
            var table = new LuaTable(2);
            table["X"] = args.GetFloat(0);
            table["Y"] = args.GetFloat(1);
            return table;
        }

        public static LuaValue Vector3(in LuaArgs args)
        {
            var table = new LuaTable(3);
            table["X"] = args.GetFloat(0);
            table["Y"] = args.GetFloat(1);
            table["Z"] = args.GetFloat(2);
            return table;
        }

        public static LuaValue Colour(in LuaArgs args)
        {
            var table = new LuaTable(3);
            table["R"] = args.GetByte(0);
            table["G"] = args.GetByte(1);
            table["B"] = args.GetByte(2);
            if (!args.IsNil(3))
            {
                table["A"] = args.GetByte(3);
            }
            return table;
        }

        public static LuaValue Property(in LuaArgs args)
        {
            var table = new LuaTable(2);
            table["__property"] = args.GetString(0);
            table["__default"] = args[1];
            return table;
        }
    }
}
