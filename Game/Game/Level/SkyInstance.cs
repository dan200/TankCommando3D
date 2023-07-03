using Dan200.Core.Animation;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    internal class SkyInstance
    {
        private Sky m_sky;

        public Sky Sky
        {
            get
            {
                return m_sky;
            }
        }

        public ColourF BackgroundColour
        {
            get
            {
				return m_sky.BackgroundColour;
            }
        }

        public ColourF AmbientColour
        {
            get
            {
				return m_sky.AmbientColour;
            }
        }

        public SkyInstance(Sky sky)
        {
            m_sky = sky;
        }
    }
}

