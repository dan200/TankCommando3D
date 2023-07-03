using Dan200.Core.Input;
using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
	internal class TouchPress : ISpatialPress
    {
        private Screen m_screen;
        private Touch m_touch;
        private IAreaHolder m_areaHolder;
        private int m_subArea;

        public bool Pressed
        {
            get
            {
                return m_touch.Pressed;
            }
        }

        public bool Held
        {
            get
            {
                return m_touch.Held;
            }
        }

        public bool Released
        {
            get
            {
                if (m_touch.Released)
                {
					if (m_areaHolder != null)
					{
						var quad = m_areaHolder.GetSubArea(m_subArea);
						return quad.Contains(m_screen.WindowToScreen(m_touch.LatestPosition));
					}
					else
					{
						return true;
					}
                }
                return false;
            }
        }

        public int SubArea
        {
            get
            {
                return m_subArea;
            }
        }

		public Vector2 CurrentPosition
        {
            get
            {
				return m_screen.WindowToScreen(m_touch.LatestPosition);
            }
        }

        public TouchPress(Screen screen, Touch touch, IAreaHolder areaHolder = null, int subArea = 0)
        {
            m_screen = screen;
            m_touch = touch;
            m_areaHolder = areaHolder;
            m_subArea = subArea;
        }
    }
}
