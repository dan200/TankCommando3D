using System;
using System.Runtime.InteropServices;
using Dan200.Core.Main;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnitVector2 : IEquatable<UnitVector2>
    {
        public static UnitVector2 ConstructUnsafe(float x, float y)
        {
            return new UnitVector2(x, y);
        }

        public static UnitVector2 ConstructUnsafe(Vector2 v)
        {
            return new UnitVector2(v);
        }

        public readonly float X;
        public readonly float Y;

        internal UnitVector2(float x, float y)
        {
            App.Assert(System.Math.Abs((x * x + y * y) - 1.0f) <= 0.0001f);
            X = x;
            Y = y;
        }

        internal UnitVector2(Vector2 v)
        {
            App.Assert(System.Math.Abs(v.Length - 1.0f) <= 0.0001f);
            X = v.X;
            Y = v.Y;
        }

		public Vector2 WithX(float x)
		{
			return new Vector2(x, Y);
		}

		public Vector2 WithY(float y)
		{
			return new Vector2(X, y);
		}

        public float Dot(Vector2 o)
        {
            return X * o.X + Y * o.Y;
        }

        public override bool Equals(object o)
        {
            if (o is UnitVector2)
            {
                return Equals((UnitVector2)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", X, Y);
        }

        public bool Equals(UnitVector2 o)
        {
            return o.X == X && o.Y == Y;
        }

        public static implicit operator Vector2(UnitVector2 a)
        {
            return new Vector2(a.X, a.Y);
        }

        public static bool operator ==(UnitVector2 a, UnitVector2 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(UnitVector2 a, UnitVector2 b)
        {
            return !a.Equals(b);
        }

        public static Vector2 operator *(UnitVector2 a, float f)
        {
            return new Vector2(
                a.X * f,
                a.Y * f
            );
        }

        public static Vector2 operator *(float f, UnitVector2 a)
        {
            return new Vector2(
                a.X * f,
                a.Y * f
            );
        }

        public static Vector2 operator /(UnitVector2 a, float f)
        {
            var invF = 1.0f / f;
            return new Vector2(
                a.X * invF,
                a.Y * invF
            );
        }

        public static Vector2 operator +(UnitVector2 a, UnitVector2 b)
        {
            return new Vector2(
                a.X + b.X,
                a.Y + b.Y
            );
        }

        public static Vector2 operator -(UnitVector2 a, UnitVector2 b)
        {
            return new Vector2(
                a.X - b.X,
                a.Y - b.Y
            );
        }

        public static UnitVector2 operator -(UnitVector2 v)
        {
            return new UnitVector2(
                -v.X,
                -v.Y
            );
        }
    }
}
