#if SDL
using Dan200.Core.Window.SDL2;
using Dan200.Core.Platform.SDL2;
using Dan200.Core.Util;
using Dan200.Core.Main;
using SDL2;
using System;
using System.Collections.Generic;
using Dan200.Core.Math;

namespace Dan200.Core.Input.SDL2
{
    internal class SDL2Mouse : IMouse
    {
        private SDL2Window m_window;

        private Vector2I m_pos;
        private Vector2I m_delta;
        private Vector2I m_pendingWheel;
        private Vector2I m_pendingDelta;
        private bool m_locked;
        private bool m_showCursor;
        private Input m_wheelUp;
        private Input m_wheelDown;
        private Input m_wheelLeft;
        private Input m_wheelRight;
        private Dictionary<MouseButton, Input> m_buttons;

        public DeviceCategory Category
        {
            get
            {
                return DeviceCategory.Mouse;
            }
        }

        public bool Connected
        {
            get
            {
                return true;
            }
        }

        public IEnumerable<Input> Inputs
        {
            get
            {
                return m_buttons.Values;
            }
        }

        public Vector2I Position
        {
            get
            {
                return m_pos;
            }
        }

        public Vector2I Delta
        {
            get
            {
                return m_delta;
            }
        }

        public bool Locked
        {
            get
            {
                return m_locked;
            }
            set
            {
                if(value)
                {
                    Lock();
                }
                else
                {
                    Unlock();
                }
            }
        }

        public bool ShowCursor
        {
            get
            {
                return m_showCursor;
            }
            set
            {
                if(m_showCursor != value)
                {
                    m_showCursor = value;
                    SDL.SDL_ShowCursor(value ? 1 : 0);
                }
            }
        }

        public SDL2Mouse(SDL2Window window)
        {
            m_window = window;
            m_buttons = new Dictionary<MouseButton, Input>();
            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
            {
                m_buttons.Add(button, new Input(button.ToString(), button.GetPrompt()));
            }
            m_wheelUp = new Input("WheelUp", "[gui/prompts/mouse/scroll_wheel.png]");
            m_wheelDown = new Input("WheelDown", "[gui/prompts/mouse/scroll_wheel.png]");
            m_wheelLeft = new Input("WheelLeft", "[gui/prompts/mouse/scroll_wheel.png]");
            m_wheelRight = new Input("WheelRight", "[gui/prompts/mouse/scroll_wheel.png]");
            m_locked = false;
            m_showCursor = true;
            Update();
        }

        public Input GetInput(string name)
        {
            MouseButton button;
            if (EnumConverter.TryParse(name, out button))
            {
                return GetInput(button);
            }
            else if(name == "WheelUp")
            {
                return m_wheelUp;
            }
            else if(name == "WheelDown")
            {
                return m_wheelDown;
            }
            else if (name == "WheelLeft")
            {
                return m_wheelLeft;
            }
            else if (name == "WheelRight")
            {
                return m_wheelRight;
            }
            return null;
        }

        public Input GetInput(MouseButton button)
        {
            App.Assert(m_buttons.ContainsKey(button));
            return m_buttons[button];
        }

        public Input GetInput(MouseWheelDirection wheelDirection)
        {
            App.Assert(wheelDirection == MouseWheelDirection.Up || wheelDirection == MouseWheelDirection.Down);
            return (wheelDirection == MouseWheelDirection.Up) ? m_wheelUp : m_wheelDown;
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    {
                        // Mouse wheeling
                        if (m_window.MouseFocus)
                        {
                            m_pendingWheel.X += e.wheel.x;
                            m_pendingWheel.Y += e.wheel.y;
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    {
                        // Mouse motion (used when locked)
                        if (m_window.MouseFocus &&
                            e.motion.which != SDL.SDL_TOUCH_MOUSEID)
                        {
                            m_pendingDelta.X += e.motion.xrel;
                            m_pendingDelta.Y += e.motion.yrel;
                        }
                        break;
                    }
            }
        }

        private void Lock()
        {
            if (!m_locked)
            {
                // Lock the mouse, set position and deltas to zero
                SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_TRUE);
                m_pos = Vector2I.Zero;
                m_delta = Vector2I.Zero;
                m_locked = true;
            }
        }

        private void Unlock()
        {
            if (m_locked)
            {
                // Unlock the mouse, get the real position and deltas back
                SDL.SDL_SetRelativeMouseMode(SDL.SDL_bool.SDL_FALSE);
                if (m_window.MouseFocus)
                {
                    int newX, newY;
                    SDL.SDL_GetMouseState(out newX, out newY);
                    m_pos = new Vector2I(newX, newY);
                    m_delta = Vector2I.Zero;
                }
                else
                {
                    m_pos = Vector2I.Zero;
                    m_delta = Vector2I.Zero;
                }
                m_locked = false;
            }
        }

        public void Update()
        {
            int newX, newY;
            bool focus = m_window.Focus;
            uint buttons = SDL.SDL_GetMouseState(out newX, out newY);

            // Position and deltas
            if (m_locked)
            {
                m_delta = m_pendingDelta;
            }
            else
            {
                var newPos = new Vector2I(newX, newY);
                m_delta = newPos - m_pos;
                m_pos = newPos;
            }
            m_pendingDelta = Vector2I.Zero;

            // Mouse wheel
            m_wheelUp.Update((focus && m_pendingWheel.Y > 0) ? 1.0f : 0.0f);
            m_wheelDown.Update((focus && m_pendingWheel.Y < 0) ? 1.0f : 0.0f);
            m_wheelLeft.Update((focus && m_pendingWheel.X < 0) ? 1.0f : 0.0f);
            m_wheelRight.Update((focus && m_pendingWheel.X > 0) ? 1.0f : 0.0f);
            m_pendingWheel = Vector2I.Zero;

            // Buttons
            foreach (var pair in m_buttons)
            {
                var button = pair.Key;
                bool pressed = ((buttons & SDL.SDL_BUTTON((uint)button)) != 0);
                pair.Value.Update((focus && pressed) ? 1.0f : 0.0f);
            }
        }
    }
}
#endif
