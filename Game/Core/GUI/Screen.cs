using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Window;
using System;

namespace Dan200.Core.GUI
{
    internal class Screen : IDisposable, IScreen
    {
        private DeviceCollection m_inputDevices;
        private IAudio m_audio;
        private Language m_language;
        private IWindow m_window;
        private Element m_rootElement;
        private ScreenEffectHelper m_screenEffect;

        public Element ModalDialog;

        public float Width
        {
            get
            {
                return m_rootElement.Width;
            }
            set
            {
                m_rootElement.Width = value;
            }
        }

        public float Height
        {
            get
            {
                return m_rootElement.Height;
            }
            set
            {
                m_rootElement.Height = value;
            }
        }

        public Vector2 Size
        {
            get
            {
                return m_rootElement.Size;
            }
            set
            {
                m_rootElement.Size = value;
            }
        }

        public float AspectRatio
        {
            get
            {
                return m_rootElement.AspectRatio;
            }
        }

        public Quad Area
        {
            get
            {
                return m_rootElement.Area;
            }
        }

        public Vector2 Center
        {
            get
            {
                return m_rootElement.Center;
            }
        }

        public Language Language
        {
            get
            {
                return m_language;
            }
            set
            {
                m_language = value;
            }
        }

        public DeviceCollection InputDevices
        {
            get
            {
                return m_inputDevices;
            }
        }

        public IAudio Audio
        {
            get
            {
                return m_audio;
            }
        }

        public Vector2 MousePosition
        {
            get
            {
                var mouse = InputDevices.Mouse;
                if (mouse != null)
                {
                    return WindowToScreen(mouse.Position);
                }
                return Vector2.Zero;
            }
        }

        public DisplayType DisplayType
        {
            get
            {
                return m_window.DisplayType;
            }
        }

        public ElementSet Elements
        {
            get
            {
                return m_rootElement.Elements;
            }
        }

        public Screen(DeviceCollection inputDevices, Language language, IWindow window, IAudio audio, float width, float height)
        {
            m_inputDevices = inputDevices;
            m_audio = audio;
            m_language = language;
            m_window = window;
            m_screenEffect = new ScreenEffectHelper(window.Renderer);

			m_rootElement = new Container(width, height);
            m_rootElement.Anchor = Anchor.TopLeft;
            m_rootElement.LocalPosition = Vector2.Zero;
            m_rootElement.Init(this);
        }

        public void Dispose()
        {
            m_rootElement.Dispose();
            m_screenEffect.Dispose();
        }

        public void Update(float dt)
        {
            m_rootElement.Update(dt);
        }

        public void Draw(IRenderer renderer)
        {
            // Draw 2D content
			m_screenEffect.ScreenSize = Size;
			renderer.CurrentEffect = m_screenEffect.Instance;
			renderer.BlendMode = BlendMode.Alpha;
			try
			{
				m_rootElement.Draw(renderer, m_screenEffect);
			}
			finally
			{
				renderer.BlendMode = BlendMode.Overwrite;
			}
        }

        public bool CheckTouchPressed(Quad area, out Touch o_touch)
        {
            var touchscreen = m_inputDevices.Touchscreen;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.Touches)
                {
                    if (touch.Pressed &&
                        !touch.Claimed &&
                        area.Contains(WindowToScreen(touch.StartPosition)))
                    {
                        o_touch = touch;
                        return true;
                    }
                }
            }
            o_touch = null;
            return false;
        }

        public bool CheckMousePressed(Quad area, MouseButton button, out IMouse o_mouse)
        {
            var mouse = m_inputDevices.Mouse;
            if (mouse != null &&
                !mouse.Locked &&
                mouse.GetInput(button).Pressed &&
                area.Contains(WindowToScreen(mouse.Position)))
            {
                o_mouse = mouse;
                return true;
            }
            o_mouse = default(IMouse);
            return false;
        }

        public Vector2 WindowToScreen(Vector2I pos)
        {
            return new Vector2(
                ((float)pos.X / (float)m_window.Width) * Width,
                ((float)pos.Y / (float)m_window.Height) * Height
            );
        }
    }
}

