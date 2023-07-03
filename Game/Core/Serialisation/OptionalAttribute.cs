using System;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.Serialisation
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class OptionalAttribute : Attribute
    {
        public object Default;

        public OptionalAttribute()
        {
            Default = null;
        }

        public OptionalAttribute(float x, float y)
        {
            Default = new Vector2(x, y);
        }

        public OptionalAttribute(float x, float y, float z)
        {
            Default = new Vector3(x, y, z);
        }

        public OptionalAttribute(byte r, byte g, byte b, byte a=255)
        {
            Default = new Colour(r, g, b, a);
        }
    }
}
