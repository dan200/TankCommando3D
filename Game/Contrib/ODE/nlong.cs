
#if WINDOWS
#define NATIVE_LONG_ALWAYS_32_BIT
#endif

using System;
using System.Runtime.InteropServices;

namespace ODE
{
    [StructLayout(LayoutKind.Sequential)]
	public struct nlong : IEquatable<nlong>, IEquatable<int>, IEquatable<long>
    {
#if NATIVE_LONG_ALWAYS_32_BIT
        private readonly Int32 m_data;
#else
        private readonly IntPtr m_data;
#endif

		private nlong(int data)
        {
#if NATIVE_LONG_ALWAYS_32_BIT
            m_data = (Int32)data;
#else
            m_data = (IntPtr)data;
#endif
        }

		private nlong(long data)
        {
#if NATIVE_LONG_ALWAYS_32_BIT
            m_data = (Int32)data;
#else
            m_data = (IntPtr)data;
#endif
        }

        private int ToInt32()
        {
            return (int)m_data;
        }

		public long ToInt64()
        {
            return (long)m_data;
        }

        public bool Equals(nlong o)
        {
            return m_data == o.m_data;
        }

        public bool Equals(int o)
        {
            return ToInt64() == (long)o;
        }

        public bool Equals(long o)
        {
            return ToInt64() == o;
        }

        public static bool operator==(nlong a, nlong b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(nlong a, nlong b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(nlong a, int b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(nlong a, int b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(nlong a, long b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(nlong a, long b)
        {
            return !a.Equals(b);
        }

        public static explicit operator int(nlong a)
        {
            return a.ToInt32();
        }

        public static implicit operator long(nlong a)
        {
            return a.ToInt64();
        }

        public static implicit operator nlong(int a)
        {
            return new nlong(a);
        }

        public static explicit operator nlong(long a)
        {
            return new nlong(a);
        }

        public override bool Equals(object obj)
        {
            if(obj is nlong)
            {
                return Equals((nlong)obj);
            }
            else if(obj is int)
            {
                return Equals((int)obj);
            }
            else if (obj is long)
            {
                return Equals((long)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return m_data.GetHashCode();
        }

        public override string ToString()
        {
            return m_data.ToString();
        }
    }
}
