using Dan200.Core.Main;
using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector4 : IEquatable<Vector4>
    {
        public static readonly Vector4 Zero = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        public static readonly Vector4 One = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public static readonly UnitVector4 XAxis = new UnitVector4(1.0f, 0.0f, 0.0f, 0.0f);
        public static readonly UnitVector4 YAxis = new UnitVector4(0.0f, 1.0f, 0.0f, 0.0f);
        public static readonly UnitVector4 ZAxis = new UnitVector4(0.0f, 0.0f, 1.0f, 0.0f);
        public static readonly UnitVector4 WAxis = new UnitVector4(0.0f, 0.0f, 0.0f, 1.0f);

        public static Vector4 Lerp(Vector4 a, Vector4 b, float f)
        {
            return a + (b - a) * f;
        }

        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vector3 XYZ
        {
            get
            {
                return new Vector3(X, Y, Z);
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y + Z * Z + W * W;
            }
        }

        public float Length
        {
            get
            {
                return Mathf.Sqrt(LengthSquared);
            }
        }

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 xyz, float w)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
            W = w;
        }

        public UnitVector4 Normalise()
        {
            var len = Length;
            return new UnitVector4(
                X / len,
                Y / len,
                Z / len,
                W / len
            );
        }

        public UnitVector4 SafeNormalise(UnitVector4 _default)
        {
            var len = Length;
            if (len > 0.0f)
            {
                return new UnitVector4(
                    X / len,
                    Y / len,
                    Z / len,
                    W / len
                );
            }
            return _default;
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
            if (o is Vector4)
            {
                return Equals((Vector4)o);
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

        public bool Equals(Vector4 o)
        {
            return o.X == X && o.Y == Y && o.Z == Z && o.W == W;
        }

        public static bool operator ==(Vector4 a, Vector4 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector4 a, Vector4 b)
        {
            return !a.Equals(b);
        }

        public static Vector4 operator *(Vector4 a, Vector4 b)
        {
            return new Vector4(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z,
                a.W * b.W
            );
        }

        public static Vector4 operator *(Vector4 a, float f)
        {
            return new Vector4(
                a.X * f,
                a.Y * f,
                a.Z * f,
                a.W * f
            );
        }

        public static Vector4 operator *(float f, Vector4 a)
        {
            return new Vector4(
                a.X * f,
                a.Y * f,
                a.Z * f,
                a.W * f
            );
        }

        public static Vector4 operator /(Vector4 a, float f)
        {
            App.Assert(f != 0.0f);
            return new Vector4(
                a.X / f,
                a.Y / f,
                a.Z / f,
                a.W / f
            );
        }

        public static Vector4 operator /(Vector4 a, Vector4 b)
        {
            App.Assert(b.X != 0.0f);
            App.Assert(b.Y != 0.0f);
            App.Assert(b.Z != 0.0f);
            App.Assert(b.W != 0.0f);
            return new Vector4(
                a.X / b.X,
                a.Y / b.Y,
                a.Z / b.Z,
                a.W / b.W
            );
        }

        public static Vector4 operator +(Vector4 a, Vector4 b)
        {
            return new Vector4(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z,
                a.W + b.W
            );
        }

        public static Vector4 operator -(Vector4 a, Vector4 b)
        {
            return new Vector4(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z,
                a.W - b.W
            );
        }

        public static Vector4 operator -(Vector4 v)
        {
            return new Vector4(
                -v.X,
                -v.Y,
                -v.Z,
                -v.W
            );
        }
    }
}
