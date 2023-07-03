using Dan200.Core.Math;


namespace Dan200.Core.Render
{
    internal struct Quad
    {
		public static readonly Quad Zero = new Quad(0.0f, 0.0f, 0.0f, 0.0f);
		public static readonly Quad UnitSquare = new Quad(0.0f, 0.0f, 1.0f, 1.0f);

        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Vector2 Size
        {
            get
            {
                return new Vector2(Width, Height);
            }
        }

        public float AspectRatio
        {
            get
            {
                return Width / Height;
            }
        }

        public Vector2 TopLeft
        {
            get
            {
                return new Vector2(X, Y);
            }
        }

        public Vector2 TopRight
        {
            get
            {
                return new Vector2(X + Width, Y);
            }
        }

        public Vector2 BottomLeft
        {
            get
            {
                return new Vector2(X, Y + Height);
            }
        }

        public Vector2 BottomRight
        {
            get
            {
                return new Vector2(X + Width, Y + Height);
            }
        }

        public Vector2 Center
        {
            get
            {
                return new Vector2(X + 0.5f * Width, Y + 0.5f * Height);
            }
        }

        public Quad(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Quad(Vector2 pos, float width, float height)
        {
            X = pos.X;
            Y = pos.Y;
            Width = width;
            Height = height;
        }

        public Quad(Vector2 pos, Vector2 size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public Vector2 Interpolate(float xFraction, float yFraction)
        {
            return new Vector2(X + xFraction * Width, Y + yFraction * Height);
        }

        public Quad Sub(float xFraction, float yFraction, float widthFraction, float heightFraction)
        {
            return new Quad(
                X + xFraction * Width, Y + yFraction * Height,
                widthFraction * Width, heightFraction * Height
            );
        }

        public bool Contains(Vector2 pos)
        {
            return
                pos.X >= X && pos.X < X + Width &&
                pos.Y >= Y && pos.Y < Y + Height;
        }

        public static Quad operator +(Quad quad, Vector2 offset)
        {
            return new Quad(quad.TopLeft + offset, quad.Width, quad.Height);
        }
    }
}

