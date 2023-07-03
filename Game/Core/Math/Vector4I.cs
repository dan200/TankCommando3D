using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector4I : IEquatable<Vector4I>
    {
        public static readonly Vector4I Zero = new Vector4I(0, 0, 0, 0);
        public static readonly Vector4I One = new Vector4I(1, 1, 1, 1);
        public static readonly Vector4I XAxis = new Vector4I(1, 0, 0, 0);
        public static readonly Vector4I YAxis = new Vector4I(0, 1, 0, 0);
        public static readonly Vector4I ZAxis = new Vector4I(0, 0, 1, 0);
        public static readonly Vector4I WAxis = new Vector4I(0, 0, 0, 1);

        public int X;
        public int Y;
        public int Z;
        public int W;

        public Vector3I XYZ
        {
            get
            {
                return new Vector3I(X, Y, Z);
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public Vector4I(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4I(Vector3I xyz, int w)
        {
            X = xyz.X;
            Y = xyz.Y;
            Z = xyz.Z;
            W = w;
        }

		public Vector4I WithX(int x)
		{
			return new Vector4I(x, Y, Z, W);
		}

		public Vector4I WithY(int y)
		{
			return new Vector4I(X, y, Z, W);
		}

		public Vector4I WithZ(int z)
		{
			return new Vector4I(X, Y, z, W);
		}

		public Vector4I WithW(int w)
		{
			return new Vector4I(X, Y, Z, w);
		}

        public Vector4 ToVector4()
        {
            return new Vector4(
                (float)X,
                (float)Y,
                (float)Z,
                (float)Y
            );
        }

        public override bool Equals(object o)
        {
            if (o is Vector4I)
            {
                return Equals((Vector4I)o);
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

        public bool Equals(Vector4I o)
        {
            return o.X == X && o.Y == Y && o.Z == Z && o.W == W;
        }

        public static bool operator ==(Vector4I a, Vector4I b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector4I a, Vector4I b)
        {
            return !a.Equals(b);
        }

        public static Vector4I operator *(Vector4I a, Vector4I b)
        {
            return new Vector4I(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z,
                a.W * b.W
            );
        }

        public static Vector4I operator *(Vector4I a, int n)
        {
            return new Vector4I(
                a.X * n,
                a.Y * n,
                a.Z * n,
                a.W * n
            );
        }

        public static Vector4I operator *(int n, Vector4I a)
        {
            return new Vector4I(
                a.X * n,
                a.Y * n,
                a.Z * n,
                a.W * n
            );
        }

        public static Vector4I operator +(Vector4I a, Vector4I b)
        {
            return new Vector4I(
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z,
                a.W + b.W
            );
        }

        public static Vector4I operator -(Vector4I a, Vector4I b)
        {
            return new Vector4I(
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z,
                a.W - b.W
            );
        }

        public static Vector4I operator -(Vector4I v)
        {
            return new Vector4I(
                -v.X,
                -v.Y,
                -v.Z,
                -v.W
            );
        }
    }
}
