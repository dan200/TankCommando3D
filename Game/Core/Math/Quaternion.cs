using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Quaternion : IEquatable<Quaternion>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override bool Equals(object o)
        {
            if (o is Quaternion)
            {
                return Equals((Quaternion)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", X, Y, Z, W);
        }

        public bool Equals(Quaternion o)
        {
            return o.X == X && o.Y == Y && o.Z == Z && o.W == W;
        }

        public static bool operator ==(Quaternion a, Quaternion b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Quaternion a, Quaternion b)
        {
            return !a.Equals(b);
        }
    }
}
