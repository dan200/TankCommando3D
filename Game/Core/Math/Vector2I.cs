using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector2I : IEquatable<Vector2I>
    {
        public static readonly Vector2I Zero = new Vector2I(0, 0);
        public static readonly Vector2I One = new Vector2I(1, 1);
        public static readonly Vector2I XAxis = new Vector2I(1, 0);
        public static readonly Vector2I YAxis = new Vector2I(0, 1);

        public int X;
        public int Y;

        public Vector2I(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2I WithX(int x)
        {
            return new Vector2I(x, Y);
        }

        public Vector2I WithY(int y)
        {
            return new Vector2I(X, y);
        }

        public Vector2 ToVector2()
        {
            return new Vector2(
                (float)X,
                (float)Y
            );
        }

        public override bool Equals(object o)
        {
            if (o is Vector2I)
            {
                return Equals((Vector2I)o);
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

        public bool Equals(Vector2I o)
        {
            return o.X == X && o.Y == Y;
        }

        public static bool operator ==(Vector2I a, Vector2I b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector2I a, Vector2I b)
        {
            return !a.Equals(b);
        }

        public static Vector2I operator *(Vector2I a, Vector2I b)
        {
            return new Vector2I(
                a.X * b.X,
                a.Y * b.Y
            );
        }

        public static Vector2I operator *(Vector2I a, int b)
        {
            return new Vector2I(
                a.X * b,
                a.Y * b
            );
        }

        public static Vector2I operator *(int a, Vector2I b)
        {
            return new Vector2I(
                a * b.X,
                a * b.Y
            );
        }

        public static Vector2I operator +(Vector2I a, Vector2I b)
        {
            return new Vector2I(
                a.X + b.X,
                a.Y + b.Y
            );
        }

        public static Vector2I operator -(Vector2I a, Vector2I b)
        {
            return new Vector2I(
                a.X - b.X,
                a.Y - b.Y
            );
        }

        public static Vector2I operator -(Vector2I a)
        {
            return new Vector2I(
                -a.X,
                -a.Y
            );
        }
    }
}
