using System;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
    internal class StructComparer<T> : IEqualityComparer<T> where T : struct, IEquatable<T>
    {
        public static StructComparer<T> Instance = new StructComparer<T>();

        private StructComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(T x)
        {
            return x.GetHashCode();
        }
    }
}
