using System;
using Dan200.Core.Components.Misc;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.Components.GUI
{
    internal struct GUIScreenComponentData
    {
    }

    [RequireComponent(typeof(InputComponent))]
    [RequireComponent(typeof(GUIElementComponent))]
    internal class GUIScreenComponent : Component<GUIScreenComponentData>, IScreen
    {
        private InputComponent m_input;
        private GUIElementComponent m_rootElement;
        private Vector2I m_windowSize;

        public InputComponent Input
        {
            get
            {
                return m_input;
            }
        }

        public GUIElementComponent RootElement
        {
            get
            {
                return m_rootElement;
            }
        }

        public Vector2I WindowSize
        {
            get
            {
                return m_windowSize;
            }
            set
            {
                m_windowSize = value;
            }
        }

        protected override void OnInit(in GUIScreenComponentData properties)
        {
            m_input = Entity.GetComponent<InputComponent>();
            m_rootElement = Entity.GetComponent<GUIElementComponent>();
        }

        protected override void OnShutdown()
        {
        }

        public void Draw(IRenderer renderer, ScreenEffectHelper effectHelper)
        {
            // Draw 2D content
            effectHelper.ScreenSize = m_rootElement.Size;
            renderer.CurrentEffect = effectHelper.Instance;
            renderer.BlendMode = BlendMode.Alpha;
            try
            {
                m_rootElement.Draw(renderer, effectHelper);
            }
            finally
            {
                renderer.BlendMode = BlendMode.Overwrite;
            }
        }

        public Vector2 WindowToScreen(Vector2I pos)
        {
            return new Vector2(
                ((float)pos.X / (float)m_windowSize.X) * RootElement.Width,
                ((float)pos.Y / (float)m_windowSize.Y) * RootElement.Height
            );
        }

        public bool CheckTouchPressed(Quad area, out Touch o_touch)
        {
            var touchscreen = m_input.Devices.Touchscreen;
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

        public bool CheckMouseHover(Quad area, out IMouse o_mouse)
        {
            var mouse = m_input.Devices.Mouse;
            if (mouse != null &&
                !mouse.Locked &&
                area.Contains(WindowToScreen(mouse.Position)))
            {
                o_mouse = mouse;
                return true;
            }
            o_mouse = default(IMouse);
            return false;
        }

        public bool CheckMousePressed(Quad area, MouseButton button, out IMouse o_mouse)
        {
            var mouse = m_input.Devices.Mouse;
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
    }
}
