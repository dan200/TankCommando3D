using System;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
    internal class IntPtrComparer : IEqualityComparer<IntPtr>
    {
        public static IntPtrComparer Instance = new IntPtrComparer();

        private IntPtrComparer()
        {
        }

        public bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public int GetHashCode(IntPtr x)
        {
            return x.GetHashCode();
        }
    }
}
