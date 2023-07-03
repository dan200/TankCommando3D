using System;
namespace Dan200.Core.Render
{
	internal struct Rect : IEquatable<Rect>
	{
		public int X;
		public int Y;
		public int Width;
		public int Height;

		public Rect(int x, int y, int w, int h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

        public bool Equals(Rect other)
        {
            return
                other.X == X &&
                other.Y == Y &&
                other.Width == Width &&
                other.Height == Height;
        }

        public override bool Equals(object other)
        {
            if(other is Rect)
            {
                return Equals((Rect)other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X ^ Y ^ Width ^ Height;
        }

        public override string ToString()
        {
            return string.Format("[{0} {1} {2} {3}]", X, Y, Width, Height);
        }

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !left.Equals(right);
        }
    }
}
