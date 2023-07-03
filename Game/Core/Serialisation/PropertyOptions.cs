using System;
namespace Dan200.Core.Serialisation
{
    internal struct PropertyOptions
    {
        // Shared
        public PropertyType ElementType;
        public bool IsArray;
        public bool Optional;
        public object CustomDefault;

        // Int/Float
        public double Min;
        public double Max;

        // Enum/Struct
        public Type InnerType;
    }
}
