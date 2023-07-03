using System;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.Window.Headless
{
	internal class HeadlessWindow : IWindow
	{
		private string m_title;
		private int m_width;
		private int m_height;
        private DeviceCollection m_devices;

		public string Title
		{
			get
			{
				return m_title;
			}
			set
			{
				m_title = value;
				if (!App.Debug)
				{
					Console.Title = value;
				}
			}
		}

		public int Width
		{
			get
			{
				return m_width;
			}
		}

		public int Height 
		{
			get
			{
				return m_height;
			}
		}

        public Vector2I Size
        {
            get
            {
                return new Vector2I(m_width, m_height);
            }
        }

        public bool Closed
		{
			get
			{
				return false;
			}
		}

		public bool Fullscreen 
		{
			get
			{
				return false;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public bool Maximised
		{
			get
			{
				return false;
			}				
		}

		public bool VSync
		{
			get
			{
				return true;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public bool Focus
		{
			get
			{
				return true;
			}
		}

		public DisplayType DisplayType 
		{
			get
			{
				return DisplayType.Unknown;
			}
		}

        public DeviceCollection InputDevices
		{
			get
			{
                return m_devices;
			}
		}

		public IRenderer Renderer
		{
			get
			{
                return null;
			}
		}

		public event StructEventHandler<IWindow> OnClosed
        {
            add {}
            remove {}
        }

		public event StructEventHandler<IWindow> OnResized
        {
            add {}
            remove {}
        }

		public HeadlessWindow(string title, int width, int height)
		{
			m_title = title;
			m_width = width;
			m_height = height;
			if (!App.Debug)
			{
				Console.Title = title;
			}
            m_devices = new DeviceCollection();
		}

		public void Dispose()
		{
		}

		public void SetIcon(Bitmap bitmap)
		{
		}
	}
}
