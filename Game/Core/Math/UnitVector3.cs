using System;
using System.Runtime.InteropServices;
using Dan200.Core.Main;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnitVector3 : IEquatable<UnitVector3>
    {
        public static UnitVector3 ConstructUnsafe(float x, float y, float z)
        {
            return new UnitVector3(x, y, z);
        }

        public static UnitVector3 ConstructUnsafe(Vector3 v)
        {
            return new UnitVector3(v);
        }

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        private UnitVector3(float x, float y, float z)
        {
			App.Assert(Mathf.Abs(Mathf.Sqrt(x * x + y * y + z * z) - 1.0f) <= 0.001f);
            X = x;
            Y = y;
            Z = z;
        }

        internal UnitVector3(Vector3 vec)
        {
            App.Assert(Mathf.Abs(vec.Length - 1.0f) <= 0.001f);
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }

		public Vector3 WithX(float x)
		{
			return new Vector3(x, Y, Z);
		}

		public Vector3 WithY(float y)
		{
			return new Vector3(X, y, Z);
		}

		public Vector3 WithZ(float z)
		{
			return new Vector3(X, Y, z);
		}

        public float Dot(Vector3 o)
        {
            return X * o.X + Y * o.Y + Z * o.Z;
        }

        public Vector3 Cross(Vector3 o)
        {
            return new Vector3(
                Y * o.Z - Z * o.Y,
                Z * o.X - X * o.Z,
                X * o.Y - Y * o.X
            );
        }

        public override bool Equals(object o)
        {
            if (o is UnitVector3)
            {
                return Equals((UnitVector3)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", X, Y, Z);
        }

        public bool Equals(UnitVector3 o)
        {
            return o.X == X && o.Y == Y && o.Z == Z;
        }

        public static implicit operator Vector3(UnitVector3 a)
        {
            return new Vector3(a.X, a.Y, a.Z);
        }

        public static bool operator ==(UnitVector3 a, UnitVector3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UnitVector3 a, UnitVector3 b)
        {
            return !a.Equals(b);
        }

        public static Vector3 operator *(UnitVector3 a, float f)
        {
            return new Vector3(
                a.X * f,
                a.Y * f,
                a.Z * f
            );
        }

        public static Vector3 operator *(float f, UnitVector3 a)
        {
            return new Vector3(
                a.X * f,
                a.Y * f,
                a.Z * f
            );
        }

        public static Vector3 operator /(UnitVector3 a, float f)
        {
            var invF = 1.0f / f;
            return new Vector3(
                a.X * invF,
                a.Y * invF,
                a.Z * invF
            );
        }

        public static Vector3 operator +(UnitVector3 a, UnitVector3 b)
        {
            return new Vector3(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z
            );
        }

        public static Vector3 operator -(UnitVector3 a, UnitVector3 b)
        {
            return new Vector3(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z
            );
        }

        public static UnitVector3 operator -(UnitVector3 v)
        {
            return new UnitVector3(
                -v.X,
                -v.Y,
                -v.Z
            );
        }
    }
}
