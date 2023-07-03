using Dan200.Core.Input;
using Dan200.Core.Math;

namespace Dan200.Core.GUI
{
	internal class MousePress : ISpatialPress
    {
        private IScreen m_screen;
        private IMouse m_mouse;
        private MouseButton m_button;
        private IAreaProvider m_areaProvider;

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
					if (m_areaProvider != null)
					{
                        var area = m_areaProvider.Area;
						return area.Contains(m_screen.WindowToScreen(m_mouse.Position));
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
				return m_screen.WindowToScreen(m_mouse.Position);
			}
		}

        public MousePress(IScreen screen, IMouse mouse, MouseButton button, IAreaProvider areaProvider = null)
        {
            m_screen = screen;
            m_mouse = mouse;
            m_button = button;
            m_areaProvider = areaProvider;
        }
    }
}
