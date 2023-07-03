using Dan200.Core.Main;
using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector2 : IEquatable<Vector2>
    {
        public static readonly Vector2 Zero = new Vector2(0.0f, 0.0f);
        public static readonly Vector2 One = new Vector2(1.0f, 1.0f);
        public static readonly UnitVector2 XAxis = new UnitVector2(1.0f, 0.0f);
        public static readonly UnitVector2 YAxis = new UnitVector2(0.0f, 1.0f);

        public static Vector2 Lerp(Vector2 a, Vector2 b, float f)
        {
            return a + (b - a) * f;
        }

        public float X;
        public float Y;

        public float LengthSquared
        {
            get
            {
                return X * X + Y * Y;
            }
        }

        public float Length
        {
            get
            {
                return Mathf.Sqrt(LengthSquared);
            }
        }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2 Normalise()
        {
            var len = Length;
            return new Vector2(
                X / len,
                Y / len
            );
        }

        public Vector2 SafeNormalise(Vector2 _default)
        {
            var len = Length;
            if (len > 0.0f)
            {
                return new Vector2(
                    X / len,
                    Y / len
                );
            }
            return _default;
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
            if (o is Vector2)
            {
                return Equals((Vector2)o);
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

        public bool Equals(Vector2 o)
        {
            return o.X == X && o.Y == Y;
        }

        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return !a.Equals(b);
        }

        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.X * b.X,
                a.Y * b.Y
            );
        }

        public static Vector2 operator *(Vector2 a, float f)
        {
            return new Vector2(
                a.X * f,
                a.Y * f
            );
        }

        public static Vector2 operator *(float f, Vector2 a)
        {
            return new Vector2(
                a.X * f,
                a.Y * f
            );
        }

        public static Vector2 operator /(Vector2 a, float f)
        {
            App.Assert(f != 0.0f);
            return new Vector2(
                a.X / f,
                a.Y / f
            );
        }

        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            App.Assert(b.X != 0.0f);
            App.Assert(b.Y != 0.0f);
            return new Vector2(
                a.X / b.X,
                a.Y / b.Y
            );
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.X + b.X,
                a.Y + b.Y
            );
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.X - b.X,
                a.Y - b.Y
            );
        }

        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(
                -v.X,
                -v.Y
            );
        }
    }
}
