using System;
using Dan200.Core.Lua;
using Dan200.Core.Math;

namespace Dan200.Core.Serialisation
{
    internal static class LONSerialiser
    {
        private static LuaValue SaveProperty(PropertyOptions options, object obj)
        {
            if (options.IsArray)
            {
                var array = new LuaTable();
                switch (options.ElementType)
                {
                    case PropertyType.Bool:
                        var boolArray = (bool[])obj;
                        for (int i = 0; i < boolArray.Length; ++i)
                        {
                            array[i + 1] = boolArray[i];
                        }
                        break;
                    case PropertyType.Byte:
                        var byteArray = (bool[])obj;
                        for (int i = 0; i < byteArray.Length; ++i)
                        {
                            array[i + 1] = byteArray[i];
                        }
                        break;
                    case PropertyType.Int:
                        var intArray = (int[])obj;
                        for (int i = 0; i < intArray.Length; ++i)
                        {
                            array[i + 1] = intArray[i];
                        }
                        break;
                    case PropertyType.Float:
                        var floatArray = (float[])obj;
                        for (int i = 0; i < floatArray.Length; ++i)
                        {
                            array[i + 1] = floatArray[i];
                        }
                        break;
                    case PropertyType.String:
                        var stringArray = (string[])obj;
                        for (int i = 0; i < stringArray.Length; ++i)
                        {
                            array[i + 1] = stringArray[i];
                        }
                        break;
                    case PropertyType.LuaTable:
                        var luaTableArray = (LuaTable[])obj;
                        for (int i = 0; i < luaTableArray.Length; ++i)
                        {
                            array[i + 1] = luaTableArray[i];
                        }
                        break;
                    case PropertyType.Enum:
                        var enumArray = (Array)obj;
                        for (int i = 0; i < enumArray.Length; ++i)
                        {
                            array[i + 1] = enumArray.GetValue(i).ToString();
                        }
                        break;
                    case PropertyType.Struct:
                        var structArray = (Array)obj;
                        for (int i = 0; i < structArray.Length; ++i)
                        {
                            array[i + 1] = Save(options.InnerType, structArray.GetValue(i));
                        }
                        break;
                    default:
                        throw new Exception("Unhandled type");
                }
                return array;
            }
            else
            {
                switch (options.ElementType)
                {
                    case PropertyType.Bool:
                        return (bool)obj;
                    case PropertyType.Byte:
                        return (byte)obj;
                    case PropertyType.Int:
                        return (int)obj;
                    case PropertyType.Float:
                        return (float)obj;
                    case PropertyType.String:
                        return (string)obj;
                    case PropertyType.LuaTable:
                        return (LuaTable)obj;
                    case PropertyType.Enum:
                        return obj.ToString();
                    case PropertyType.Struct:
                        return Save(options.InnerType, obj);
                    default:
                        throw new Exception("Unhandled type");
                }
            }
        }

        public static LuaTable Save<T>(in T value) where T : struct
        {
            return Save(typeof(T), value);
        }

        public static LuaTable Save(Type type, object value)
        {
            var layout = StructLayout.Get(type);
            var result = new LuaTable();
            foreach(var pair in layout.Properties)
            {
                var name = pair.Key;
                var property = pair.Value;
                var obj = property.Field.GetValue(value);
                result[name] = SaveProperty(property.Options, obj);
            }
            return result;
        }

        public static LuaValue MakeDefault(PropertyOptions options)
        {
            if (options.CustomDefault != null)
            {
                return SaveProperty(options, options.CustomDefault);
            }
            else if (options.IsArray)
            {
                return new LuaTable();
            }
            else
            {
                switch (options.ElementType)
                {
                    case PropertyType.Bool:
                        return false;
                    case PropertyType.Byte:
                        return 0;
                    case PropertyType.Int:
                        int intMin = (int)System.Math.Max(options.Min, int.MinValue);
                        int intMax = (int)System.Math.Min(options.Max, int.MaxValue);
                        return System.Math.Min(System.Math.Max(0, intMin), intMax);
                    case PropertyType.Float:
                        float floatMin = (float)System.Math.Max(options.Min, float.MinValue);
                        float floatMax = (float)System.Math.Min(options.Max, float.MaxValue);
                        return Mathf.Clamp(0.0f, floatMin, floatMax);
                    case PropertyType.String:
                        return "";
                    case PropertyType.LuaTable:
                        return new LuaTable();
                    case PropertyType.Enum:
                        return Enum.GetValues(options.InnerType).GetValue(0).ToString();
                    case PropertyType.Struct:
                        return MakeDefault(options.InnerType);
                    default:
                        throw new Exception("Unhandled type");
                }
            }
        }

        public static LuaTable MakeDefault<T>() where T : struct
        {
            return MakeDefault(typeof(T));
        }

        public static LuaTable MakeDefault(Type type)
        {
            var layout = StructLayout.Get(type);
            var result = new LuaTable();
            foreach (var pair in layout.Properties)
            {
                var propertyName = pair.Key;
                var property = pair.Value;
                if (!property.Options.Optional)
                {
                    result[propertyName] = MakeDefault(property.Options);
                }
            }
            return result;
        }

        public static T Parse<T>(LuaTable table) where T : struct
        {
            return (T)Parse(typeof(T), table);
        }

        public static object Parse(Type type, LuaTable table)
        {
            var result = Activator.CreateInstance(type);
            var layout = StructLayout.Get(type);
            foreach (var pair in layout.Properties)
            {
                var name = pair.Key;
                var property = pair.Value;
                var value = ParseValue(name, table[name], property.Options);
                if (value != null)
                {
                    property.Field.SetValue(result, value);
                }
            }
            return result;
        }

        public static object ParseValue(string name, LuaValue value, PropertyOptions options)
        {
            object result;
            if (options.IsArray)
            {
                // Array property
                if (!value.IsNil())
                {
                    Array array;
                    var table = value.GetTable();
                    switch (options.ElementType)
                    {
                        case PropertyType.Bool:
                            array = Array.CreateInstance(typeof(bool), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var boolValue = table.GetBool(i + 1);
                                array.SetValue(boolValue, i);
                            }
                            break;
                        case PropertyType.Byte:
                            array = Array.CreateInstance(typeof(byte), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var byteValue = table.GetByte(i + 1);
                                array.SetValue(byteValue, i);
                            }
                            break;
                        case PropertyType.Int:
                            array = Array.CreateInstance(typeof(int), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var intValue = table.GetInt(i + 1);
                                if (intValue < options.Min || intValue > options.Max)
                                {
                                    throw new LuaError("Entry " + (i + 1) + " in array field " + name + " is out of range");
                                }
                                array.SetValue(intValue, i);
                            }
                            break;
                        case PropertyType.Float:
                            array = Array.CreateInstance(typeof(float), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var floatValue = table.GetFloat(i + 1);
                                if (floatValue < options.Min || floatValue > options.Max)
                                {
                                    throw new LuaError("Entry " + (i + 1) + " in array field " + name + " is out of range");
                                }
                                array.SetValue(floatValue, i);
                            }
                            break;
                        case PropertyType.String:
                            array = Array.CreateInstance(typeof(string), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var stringValue = table.GetString(i + 1);
                                array.SetValue(stringValue, i);
                            }
                            break;
                        case PropertyType.LuaTable:
                            array = Array.CreateInstance(typeof(LuaTable), table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var tableValue = table.GetTable(i + 1);
                                array.SetValue(tableValue, i);
                            }
                            break;
                        case PropertyType.Enum:
                            array = Array.CreateInstance(options.InnerType, table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var str = table.GetString(i + 1);
                                var enumValue = Enum.Parse(options.InnerType, str);
                                array.SetValue(enumValue, i);
                            }
                            break;
                        case PropertyType.Struct:
                            array = Array.CreateInstance(options.InnerType, table.ArrayLength);
                            for (int i = 0; i < table.ArrayLength; ++i)
                            {
                                var subTable = table.GetTable(i + 1);
                                var structValue = Parse(options.InnerType, subTable);
                                array.SetValue(structValue, i);
                            }
                            break;
                        default:
                            throw new Exception("Unhandled type");
                    }
                    result = array;
                }
                else if (options.Optional)
                {
                    result = options.CustomDefault;
                }
                else
                {
                    throw new LuaError(string.Format(
                        "Missing field {0}",
                        name
                    ));
                }
            }
            else
            {
                // Singular property
                if (!value.IsNil())
                {
                    switch (options.ElementType)
                    {
                        case PropertyType.Bool:
                            result = value.GetBool();
                            break;
                        case PropertyType.Byte:
                            result = value.GetByte();
                            break;
                        case PropertyType.Int:
                            var intValue = value.GetInt();
                            if (intValue < options.Min || intValue > options.Max)
                            {
                                throw new LuaError("Field " + name + " is out of range");
                            }
                            result = intValue;
                            break;
                        case PropertyType.Float:
                            var floatValue = value.GetFloat();
                            if (floatValue < options.Min || floatValue > options.Max)
                            {
                                throw new LuaError("Field " + name + " is out of range");
                            }
                            result = floatValue;
                            break;
                        case PropertyType.String:
                            result = value.GetString();
                            break;
                        case PropertyType.LuaTable:
                            result = value.GetTable();
                            break;
                        case PropertyType.Enum:
                            var strValue = value.GetString();
                            result = Enum.Parse(options.InnerType, strValue);
                            break;
                        case PropertyType.Struct:
                            var tableValue = value.GetTable();
                            result = Parse(options.InnerType, tableValue);
                            break;
                        default:
                            throw new Exception("Unhandled type");
                    }
                }
                else if (options.Optional)
                {
                    result = options.CustomDefault;
                }
                else
                {
                    throw new LuaError(string.Format(
                        "Missing field {0}",
                        name
                    ));
                }
            }
            return result;
        }
    }
}
