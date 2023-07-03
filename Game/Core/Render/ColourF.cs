using Dan200.Core.Math;
using System;

namespace Dan200.Core.Render
{
    internal struct ColourF : IEquatable<ColourF>
    {
        public static readonly ColourF Black = new ColourF(0.0f, 0.0f, 0.0f, 1.0f);
        public static readonly ColourF White = new ColourF(1.0f, 1.0f, 1.0f, 1.0f);
        public static readonly ColourF Red = new ColourF(1.0f, 0.0f, 0.0f, 1.0f);
        public static readonly ColourF Green = new ColourF(0.0f, 1.0f, 0.0f, 1.0f);
        public static readonly ColourF Blue = new ColourF(0.0f, 0.0f, 1.0f, 1.0f);
        public static readonly ColourF Cyan = new ColourF(0.0f, 1.0f, 1.0f, 1.0f);
        public static readonly ColourF Magenta = new ColourF(1.0f, 0.0f, 1.0f, 1.0f);
        public static readonly ColourF Yellow = new ColourF(1.0f, 1.0f, 0.0f, 1.0f);
		public static readonly ColourF Transparent = new ColourF(0.0f, 0.0f, 0.0f, 0.0f);

        public float R;
        public float G;
        public float B;
        public float A;

        public ColourF(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ColourF ToSRGB()
        {
            return new ColourF(
                ColourSpaceUtils.LinearToSRGB(R),
                ColourSpaceUtils.LinearToSRGB(G),
                ColourSpaceUtils.LinearToSRGB(B),
                A
            );
        }

        public ColourF ToLinear()
        {
            return new ColourF(
                ColourSpaceUtils.SRGBToLinear(R),
                ColourSpaceUtils.SRGBToLinear(G),
                ColourSpaceUtils.SRGBToLinear(B),
                A
            );
        }

        public override bool Equals(object o)
        {
            if (o is ColourF)
            {
                return Equals((ColourF)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", R, G, B, A);
        }

        public bool Equals(ColourF o)
        {
            return o.R == R && o.G == G && o.B == B && o.A == A;
        }

        public static bool operator ==(ColourF a, ColourF b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ColourF a, ColourF b)
        {
            return !a.Equals(b);
        }

        public static ColourF operator *(ColourF a, ColourF b)
        {
            return new ColourF(
                a.R * b.R,
                a.G * b.G,
                a.B * b.B,
                a.A * b.A
            );
        }

        public static ColourF operator *(ColourF a, float f)
        {
            return new ColourF(
                a.R * f,
                a.G * f,
                a.B * f,
                a.A * f
            );
        }

        public static ColourF operator *(float f, ColourF a)
        {
            return new ColourF(
                a.R * f,
                a.G * f,
                a.B * f,
                a.A * f
            );
        }

        public static ColourF operator +(ColourF a, ColourF b)
        {
            return new ColourF(
                a.R + b.R,
                a.G + b.G,
                a.B + b.B,
                a.A + b.A
            );
        }

        public Colour ToColour()
        {
            return new Colour(
                (byte)(Mathf.Clamp(R, 0.0f, 1.0f) * 255.0f),
                (byte)(Mathf.Clamp(G, 0.0f, 1.0f) * 255.0f),
                (byte)(Mathf.Clamp(B, 0.0f, 1.0f) * 255.0f),
                (byte)(Mathf.Clamp(A, 0.0f, 1.0f) * 255.0f)
            );
        }
    }
}
