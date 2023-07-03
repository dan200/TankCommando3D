using System;
using Dan200.Core.Main;

namespace Dan200.Core.Serialisation
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class RangeAttribute : Attribute
    {
        public double Min;
        public double Max;

        public RangeAttribute()
        {
            Min = double.MinValue;
            Max = double.MaxValue;
        }

        public RangeAttribute(int min, int max)
        {
            App.Assert(max >= min);
            Min = (double)min;
            Max = (double)max;
        }

        public RangeAttribute(float min, float max)
        {
            App.Assert(max >= min);
            Min = (double)min;
            Max = (double)max;
        }
    }
}
