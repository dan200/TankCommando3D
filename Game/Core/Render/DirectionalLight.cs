
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class DirectionalLight
    {
        public ColourF Colour;
        public UnitVector3 Direction;
		public bool CastShadows;

		public DirectionalLight(UnitVector3 direction, ColourF colour, bool castShadows)
        {
            Direction = direction;
            Colour = colour;
			CastShadows = castShadows;
        }
    }
}
