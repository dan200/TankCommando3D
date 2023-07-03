using System;
using System.Collections.Generic;
using System.Reflection;
using Dan200.Core.Lua;
using Dan200.Core.Main;

namespace Dan200.Core.Serialisation
{
    internal class StructLayout
    {
        private static Dictionary<Type, StructLayout> s_layouts = new Dictionary<Type, StructLayout>();

        public static StructLayout Get(Type type)
        {
            StructLayout result;
            lock (s_layouts)
            {
                if (!s_layouts.TryGetValue(type, out result))
                {
                    result = new StructLayout(type);
                    s_layouts.Add(type, result);
                }
            }
            return result;
        }

        internal struct Property
        {
            public FieldInfo Field;
            public PropertyOptions Options;
        }
        public Dictionary<string, Property> Properties;

        private StructLayout(Type type)
        {
            var fields = type.GetFields();
            var properties = new Dictionary<string, Property>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var name = field.Name;
                var property = new Property();
                property.Field = field;

                // Determine if the field is an array or singular
                Type elementType;
                if(field.FieldType.IsArray && field.FieldType.GetArrayRank() == 1)
                {
                    property.Options.IsArray = true;
                    elementType = field.FieldType.GetElementType();
                }
                else
                {
                    elementType = field.FieldType;
                }

                // Determine the type of the field or array element
                if (elementType == typeof(bool))
                {
                    property.Options.ElementType = PropertyType.Bool;
                }
                else if (elementType == typeof(byte))
                {
                    property.Options.ElementType = PropertyType.Byte;
                }
                else if (elementType == typeof(int))
                {
                    property.Options.ElementType = PropertyType.Int;
                }
                else if (elementType == typeof(float))
                {
                    property.Options.ElementType = PropertyType.Float;
                }
                else if (elementType == typeof(string))
                {
                    property.Options.ElementType = PropertyType.String;
                }
                else if (elementType == typeof(LuaTable))
                {
                    property.Options.ElementType = PropertyType.LuaTable;
                }
                else if (elementType.IsEnum)
                {
                    property.Options.ElementType = PropertyType.Enum;
                    property.Options.InnerType = elementType;
                }
                else if (elementType.IsValueType && !elementType.IsPrimitive)
                {
                    property.Options.ElementType = PropertyType.Struct;
                    property.Options.InnerType = elementType;
                }
                else
                {
                    throw new Exception(string.Format(
                        "Unsupported type {0} for field {1}", field.FieldType.Name, field.Name
                    ));
                }

                // Determine if the field is optional
                var optional = field.GetCustomAttribute<OptionalAttribute>();
                if (optional != null)
                {
                    property.Options.Optional = true;
                    property.Options.CustomDefault = optional.Default;
                    App.Assert(
                        property.Options.CustomDefault == null || field.FieldType.IsAssignableFrom(property.Options.CustomDefault.GetType()),
                        "The default value on optional field " + field.Name + " is of the wrong type"
                    );
                }

                // Determine the range of the field
                var range = field.GetCustomAttribute<RangeAttribute>();
                if (range != null)
                {
                    App.Assert(range.Max >= range.Min);
                    property.Options.Min = range.Min;
                    property.Options.Max = range.Max;
                    switch (property.Options.ElementType)
                    {
                        case PropertyType.Int:
                            if (property.Options.Optional)
                            {
                                var defaultValue = (property.Options.CustomDefault != null) ? (int)property.Options.CustomDefault : 0;
                                App.Assert(
                                    defaultValue >= property.Options.Min && defaultValue <= property.Options.Max,
                                    "The default value on field " + field.Name + " is outside the allowed range"
                                );
                            }
                            break;
                        case PropertyType.Float:
                            if (property.Options.Optional)
                            {
                                var defaultValue = (property.Options.CustomDefault != null) ? (float)property.Options.CustomDefault : 0.0f;
                                App.Assert(
                                    defaultValue >= property.Options.Min && defaultValue <= property.Options.Max,
                                    "The default value on attribute " + field.Name + " is outside the allowed range"
                                );
                            }
                            break;
                        default:
                            App.Assert(
                                false,
                                "Range attributes are only valid on float or int fields"
                            );
                            break;
                    }
                }
                else
                {
                    property.Options.Min = double.MinValue;
                    property.Options.Max = double.MaxValue;
                }

                properties.Add(name, property);
            }
            Properties = properties;
        }
    }
}
