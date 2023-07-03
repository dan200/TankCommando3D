
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class PointLight
    {
        public Vector3 Position;
        public ColourF Colour;
        public float Range;
		public bool CastShadows;

		public PointLight(Vector3 position, ColourF colour, float range, bool castShadows)
        {
            Position = position;
            Colour = colour;
			Range = range;
			CastShadows = castShadows;
        }
    }
}

