using Dan200.Core.Input;
using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
	internal class TouchPress : ISpatialPress
    {
        private IScreen m_screen;
        private Touch m_touch;
        private IAreaProvider m_areaProvider;

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
					if (m_areaProvider != null)
					{
                        var area = m_areaProvider.Area;
						return area.Contains(m_screen.WindowToScreen(m_touch.LatestPosition));
					}
					else
					{
						return true;
					}
                }
                return false;
            }
        }

		public Vector2 CurrentPosition
        {
            get
            {
				return m_screen.WindowToScreen(m_touch.LatestPosition);
            }
        }

        public TouchPress(IScreen screen, Touch touch, IAreaProvider areaProvider=null)
        {
            m_screen = screen;
            m_touch = touch;
            m_areaProvider = areaProvider;
        }
    }
}
