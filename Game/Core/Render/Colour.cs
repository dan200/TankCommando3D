using System;
using System.Runtime.InteropServices;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Render
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Colour : IEquatable<Colour>
    {
        private static byte[] SRGB_TO_LINEAR = new byte[256];
        private static byte[] LINEAR_TO_SRGB = new byte[256];

        static Colour()
        {
            for(int i=0; i<=255; ++i)
            {
                var f = (float)i / 255.0f;
                var linear = (byte)(ColourSpaceUtils.SRGBToLinear(f) * 255.0f);
                var srgb = (byte)(ColourSpaceUtils.LinearToSRGB(f) * 255.0f);
                SRGB_TO_LINEAR[i] = linear;
                LINEAR_TO_SRGB[i] = srgb;
            }
        }

        public static readonly Colour Black = new Colour(0, 0, 0, 255);
        public static readonly Colour White = new Colour(255, 255, 255, 255);
        public static readonly Colour Red = new Colour(255, 0, 0, 255);
        public static readonly Colour Green = new Colour(0, 255, 0, 255);
        public static readonly Colour Blue = new Colour(0, 0, 255, 255);
        public static readonly Colour Cyan = new Colour(0, 255, 255, 255);
        public static readonly Colour Magenta = new Colour(255, 0, 255, 255);
        public static readonly Colour Yellow = new Colour(255, 255, 0, 255);
		public static readonly Colour Transparent = new Colour(0, 0, 0, 0);

        public byte R;
        public byte G;
        public byte B;

        [Serialisation.Optional(Default = (byte)255)]
        public byte A;

        public uint RGBA
        {
            get
            {
                return
                    (uint)(R << 24) +
                    (uint)(G << 16) +
                    (uint)(B << 8) +
                    (uint)A;
            }
            set
            {
                R = (byte)((value >> 24) & 0xff);
                G = (byte)((value >> 16) & 0xff);
                B = (byte)((value >> 8) & 0xff);
                A = (byte)(value & 0xff);
            }
        }

		public Colour(byte r, byte g, byte b, byte a=255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Colour(uint rgba)
        {
            R = (byte)((rgba >> 24) & 0xff);
            G = (byte)((rgba >> 16) & 0xff);
            B = (byte)((rgba >> 8) & 0xff);
            A = (byte)(rgba & 0xff);
        }

        public Colour ToSRGB()
        {
            return new Colour(
                LINEAR_TO_SRGB[R],
                LINEAR_TO_SRGB[G],
                LINEAR_TO_SRGB[B],
                A
            );
        }

        public Colour ToLinear()
        {
            return new Colour(
                SRGB_TO_LINEAR[R],
                SRGB_TO_LINEAR[G],
                SRGB_TO_LINEAR[B],
                A
            );
        }

        public override bool Equals(object o)
        {
            if (o is Colour)
            {
                return Equals((Colour)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)RGBA;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", R, G, B, A);
        }

        public bool Equals(Colour o)
        {
            return o.R == R && o.G == G && o.B == B && o.A == A;
        }

        public static bool operator ==(Colour a, Colour b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Colour a, Colour b)
        {
            return !a.Equals(b);
        }

        public ColourF ToColourF()
        {
            return new ColourF(
                (float)R / 255.0f,
                (float)G / 255.0f,
                (float)B / 255.0f,
                (float)A / 255.0f
            );
        }
    }
}
