using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector3I : IEquatable<Vector3I>
    {
        public static readonly Vector3I Zero = new Vector3I(0, 0, 0);
        public static readonly Vector3I One = new Vector3I(1, 1, 1);
        public static readonly Vector3I XAxis = new Vector3I(1, 0, 0);
        public static readonly Vector3I YAxis = new Vector3I(0, 1, 0);
        public static readonly Vector3I ZAxis = new Vector3I(0, 0, 1);

        public int X;
        public int Y;
        public int Z;

        public Vector2I XY
        {
            get
            {
                return new Vector2I(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I(Vector2I xy, int z)
        {
            X = xy.X;
            Y = xy.Y;
            Z = z;
        }

        public Vector3I WithX(int x)
        {
            return new Vector3I(x, Y, Z);
        }

        public Vector3I WithY(int y)
        {
            return new Vector3I(X, y, Z);
        }

        public Vector3I WithZ(int z)
        {
            return new Vector3I(X, Y, z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(
                (float)X,
                (float)Y,
                (float)Z
            );
        }

        public override bool Equals(object o)
        {
            if (o is Vector3I)
            {
                return Equals((Vector3I)o);
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

        public bool Equals(Vector3I o)
        {
            return o.X == X && o.Y == Y && o.Z == Z;
        }

        public static bool operator ==(Vector3I a, Vector3I b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector3I a, Vector3I b)
        {
            return !a.Equals(b);
        }

        public static Vector3I operator *(Vector3I a, Vector3I b)
        {
            return new Vector3I(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z
            );
        }

        public static Vector3I operator *(Vector3I a, int b)
        {
            return new Vector3I(
                a.X * b,
                a.Y * b,
                a.Z * b
            );
        }

        public static Vector3I operator *(int a, Vector3I b)
        {
            return new Vector3I(
                a * b.X,
                a * b.Y,
                a * b.Z
            );
        }

        public static Vector3I operator +(Vector3I a, Vector3I b)
        {
            return new Vector3I(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z
            );
        }

        public static Vector3I operator -(Vector3I a, Vector3I b)
        {
            return new Vector3I(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z
            );
        }

        public static Vector3I operator -(Vector3I a)
        {
            return new Vector3I(
                -a.X,
                -a.Y,
                -a.Z
            );
        }
    }
}
