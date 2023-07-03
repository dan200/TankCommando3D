using System;
using System.Runtime.InteropServices;
using Dan200.Core.Main;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnitVector4 : IEquatable<UnitVector4>
    {
        public static UnitVector4 ConstructUnsafe(float x, float y, float z, float w)
        {
            return new UnitVector4(x, y, z, w);
        }

        public static UnitVector4 ConstructUnsafe(Vector4 v)
        {
            return new UnitVector4(v);
        }

        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;

        internal UnitVector4(float x, float y, float z, float w)
        {
            App.Assert(System.Math.Abs((x * x + y * y + z * z + w * w) - 1.0f) <= 0.0001f);
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        internal UnitVector4(Vector4 v)
        {
            App.Assert(System.Math.Abs(v.Length - 1.0f) <= 0.0001f);
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = v.W;
        }

		public Vector4 WithX(float x)
		{
			return new Vector4(x, Y, Z, W);
		}

		public Vector4 WithY(float y)
		{
			return new Vector4(X, y, Z, W);
		}

		public Vector4 WithZ(float z)
		{
			return new Vector4(X, Y, z, W);
		}

		public Vector4 WithW(float w)
		{
			return new Vector4(X, Y, Z, w);
		}

		public float Dot(Vector4 o)
        {
            return X * o.X + Y * o.Y + Z * o.Z + W * o.W;
        }

        public override bool Equals(object o)
        {
            if (o is UnitVector4)
            {
                return Equals((UnitVector4)o);
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

        public bool Equals(UnitVector4 o)
        {
            return o.X == X && o.Y == Y && o.Z == Z && o.W == W;
        }

        public static implicit operator Vector4(UnitVector4 a)
        {
            return new Vector4(a.X, a.Y, a.Z, a.W);
        }

        public static bool operator ==(UnitVector4 a, UnitVector4 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UnitVector4 a, UnitVector4 b)
        {
            return !a.Equals(b);
        }

        public static Vector4 operator *(UnitVector4 a, float f)
        {
            return new Vector4(
                a.X * f,
                a.Y * f,
                a.Z * f,
                a.W * f
            );
        }

        public static Vector4 operator *(float f, UnitVector4 a)
        {
            return new Vector4(
                a.X * f,
                a.Y * f,
                a.Z * f,
                a.W * f
            );
        }

        public static Vector4 operator /(UnitVector4 a, float f)
        {
            var invF = 1.0f / f;
            return new Vector4(
                a.X * invF,
                a.Y * invF,
                a.Z * invF,
                a.W * invF
            );
        }

        public static Vector4 operator +(UnitVector4 a, UnitVector4 b)
        {
            return new Vector4(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z,
                a.W + b.W
            );
        }

        public static Vector4 operator -(UnitVector4 a, UnitVector4 b)
        {
            return new Vector4(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z,
                a.W - b.W
            );
        }

        public static UnitVector4 operator -(UnitVector4 v)
        {
            return new UnitVector4(
                -v.X,
                -v.Y,
                -v.Z,
                -v.W
            );
        }
    }
}
