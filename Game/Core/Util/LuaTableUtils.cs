using System;
using System.Collections.Generic;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.Util
{
	internal static class LuaTableUtils
	{
		public static LuaTable Merge(LuaTable parent, LuaTable child)
		{
			if (parent.Count == 0)
			{
				return child;
			}
			else if (child.Count == 0)
			{
				return parent;
			}
			else
			{
				var copy = parent.Copy();
				foreach (var pair in child)
				{
					copy[pair.Key] = pair.Value;
				}
				return copy;
			}
		}

        private static bool ContainsProperties(LuaTable table, out string o_rootPropertyName)
		{
			o_rootPropertyName = table.GetOptionalString("__property");
			if (o_rootPropertyName != null)
			{
				return true;
			}
			string unused;
			foreach (var pair in table)
			{
				var v = pair.Value;
				if (v.IsTable() && ContainsProperties(v.GetTable(), out unused))
				{
					return true;
				}
			}
			return false;
		}

		private static LuaValue InjectProperties(LuaValue value, LuaTable properties)
		{
			if (value.IsTable())
			{
				var table = value.GetTable();
				string rootPropertyName;
				if (ContainsProperties(table, out rootPropertyName))
				{
					if (rootPropertyName != null)
					{
						// Property
						var propertyValue = properties[rootPropertyName];
						if (propertyValue.IsNil())
						{
							return table["__default"];
						}
						return propertyValue;
					}
					else
					{
						// Table containing properties
						var copy = new LuaTable(table.Count);
						foreach (var pair in table)
						{
                            copy[pair.Key] = InjectProperties(pair.Value, properties);
						}
						return copy;
					}
				}
			}

			// A value which doesn't need modification
			return value;
		}

		public static LuaTable InjectProperties(LuaTable table, LuaTable properties)
		{
			if (table.Count > 0)
			{
				return InjectProperties(new LuaValue(table), properties).GetTable();
			}
			return table;
		}

        public static void FindProperties(LuaTable table, Dictionary<string, LuaValue> o_properties)
        {
            string rootPropertyName = table.GetOptionalString("__property");
            if (rootPropertyName != null)
            {
                // This table is a property
                o_properties[rootPropertyName] = table["__default"];
            }
            else
            {
                // This table may contain properties
                foreach (var pair in table)
                {
                    if (pair.Value.IsTable())
                    {
                        FindProperties(pair.Value.GetTable(), o_properties);
                    }
                }
            }
        }

        public static LuaValue ToLuaValue(this Vector2 vec)
        {
            var table = new LuaTable(2);
            table["X"] = vec.X;
            table["Y"] = vec.Y;
            return table;
        }

        public static LuaValue ToLuaValue(this Vector3 vec)
		{
			var table = new LuaTable(3);
			table["X"] = vec.X;
			table["Y"] = vec.Y;
            table["Z"] = vec.Z;
			return table;
		}

		public static LuaValue ToLuaValue<TEnum>(this TEnum e) where TEnum : struct, IConvertible
		{
			App.Assert(typeof(TEnum).IsEnum);
			App.Assert(Enum.IsDefined(typeof(TEnum), e));
			return EnumConverter.ToString(e);
		}
	}
}
