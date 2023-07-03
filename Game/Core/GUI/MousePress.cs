using Dan200.Core.Input;
using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
	internal class MousePress : ISpatialPress
    {
        private Screen m_screen;
        private IMouse m_mouse;
        private MouseButton m_button;
        private IAreaHolder m_areaHolder;
        private int m_subArea;

        public bool Held
        {
            get
            {
                return m_mouse.GetInput(m_button).Held;
            }
        }

        public bool Released
        {
            get
            {
                if (m_mouse.GetInput(m_button).Released)
                {
					if (m_areaHolder != null)
					{
						var quad = m_areaHolder.GetSubArea(m_subArea);
						return quad.Contains(m_screen.MousePosition);
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
				return m_screen.MousePosition;
			}
		}

        public MousePress(Screen screen, IMouse mouse, MouseButton button, IAreaHolder areaHolder = null, int subArea = 0)
        {
            m_screen = screen;
            m_mouse = mouse;
            m_button = button;
            m_areaHolder = areaHolder;
            m_subArea = subArea;
        }
    }
}
