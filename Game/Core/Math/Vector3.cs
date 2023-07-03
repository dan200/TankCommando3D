using System;
using System.Runtime.InteropServices;
using Dan200.Core.Main;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector3 : IEquatable<Vector3>
    {
        public static readonly Vector3 Zero = new Vector3(0.0f, 0.0f, 0.0f);
        public static readonly Vector3 One = new Vector3(1.0f, 1.0f, 1.0f);
        public static readonly UnitVector3 XAxis = UnitVector3.ConstructUnsafe(1.0f, 0.0f, 0.0f);
        public static readonly UnitVector3 YAxis = UnitVector3.ConstructUnsafe(0.0f, 1.0f, 0.0f);
        public static readonly UnitVector3 ZAxis = UnitVector3.ConstructUnsafe(0.0f, 0.0f, 1.0f);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float f)
        {
            return a + (b - a) * f;
        }

        public float X;
        public float Y;
        public float Z;

        public Vector2 XY
        {
            get
            {
                return new Vector2(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y + Z * Z;
            }
        }

        public float Length
        {
            get
            {
                return Mathf.Sqrt(LengthSquared);
            }
        }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector2 xy, float z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        public UnitVector3 Normalise()
        {
            var len = Length;
			App.Assert(len > 0.0f);
            return UnitVector3.ConstructUnsafe(
                X / len,
                Y / len,
                Z / len
            );
        }

        public UnitVector3 SafeNormalise(UnitVector3 _default)
        {
            var len = Length;
            if (len > 0.0f)
            {
                return UnitVector3.ConstructUnsafe(
                    X / len,
                    Y / len,
                    Z / len
                );
            }
            return _default;
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
            if (o is Vector3)
            {
                return Equals((Vector3)o);
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

        public bool Equals(Vector3 o)
        {
            return o.X == X && o.Y == Y && o.Z == Z;
        }

        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !a.Equals(b);
        }

        public static Vector3 operator *(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z
            );
        }

        public static Vector3 operator *(Vector3 a, float f)
        {
            return new Vector3(
                a.X * f,
                a.Y * f,
                a.Z * f
            );
        }

        public static Vector3 operator *(float f, Vector3 a)
        {
            return new Vector3(
                a.X * f,
                a.Y * f,
                a.Z * f
            );
        }

        public static Vector3 operator /(Vector3 a, float f)
        {
            App.Assert(f != 0.0f);
            return new Vector3(
                a.X / f,
                a.Y / f,
                a.Z / f
            );
        }

        public static Vector3 operator /(Vector3 a, Vector3 b)
        {
            App.Assert(b.X != 0.0f);
            App.Assert(b.Y != 0.0f);
            App.Assert(b.Z != 0.0f);
            return new Vector3(
                a.X / b.X,
                a.Y / b.Y,
                a.Z / b.Z
            );
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z
            );
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z
            );
        }

        public static Vector3 operator -(Vector3 v)
        {
            return new Vector3(
                -v.X,
                -v.Y,
                -v.Z
            );
        }
    }
}
