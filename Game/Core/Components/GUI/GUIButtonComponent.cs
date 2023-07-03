using System;
using Dan200.Core.Components.Misc;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;

namespace Dan200.Core.Components.GUI
{
    internal enum ButtonState
    {
        Idle,
        Hover,
        Held,
    }

    internal struct ButtonPressEventArgs
    {
    }

    internal struct ButtonHoverEventArgs
    {
    }

    internal struct GUIButtonComponentData
    {
        [Optional()]
        public string ShortcutInput;
    }

    [RequireComponentOnAncestor(typeof(GUIScreenComponent))]
    [RequireComponent(typeof(GUIElementComponent))]
    internal class GUIButtonComponent : Component<GUIButtonComponentData>, IUpdate
    {
        private GUIScreenComponent m_screen;
        private GUIElementComponent m_element;

        private Input.Input m_shortcut;
        private bool m_disabled;
        private bool m_hover;
        private IPress m_currentPress;

        public bool Held
        {
            get
            {
                return m_currentPress != null;
            }
        }

        public bool Disabled
        {
            get
            {
                return m_disabled;
            }
            set
            {
                m_disabled = value;
            }
        }

        public Input.Input Shortcut
        {
            get
            {
                return m_shortcut;
            }
            set
            {
                m_shortcut = value;
            }
        }

        public bool Blocked
        {
            get
            {
                return IsBlocked();
            }
        }

        public ButtonState State
        {
            get
            {
                if (m_currentPress != null)
                {
                    return ButtonState.Held;
                }
                else if (m_hover)
                {
                    return ButtonState.Hover;
                }
                else
                {
                    return ButtonState.Idle;
                }
            }
        }

        public bool Hover
        {
            get
            {
                return m_hover;
            }
        }

        public event StructEventHandler<GUIButtonComponent, ButtonHoverEventArgs> OnHoverEnter;
        public event StructEventHandler<GUIButtonComponent, ButtonHoverEventArgs> OnHoverLeave;
        public event StructEventHandler<GUIButtonComponent, ButtonPressEventArgs> OnPressed;
        public event StructEventHandler<GUIButtonComponent, ButtonPressEventArgs> OnReleased;
        public event StructEventHandler<GUIButtonComponent, ButtonPressEventArgs> OnCancelled;

        protected override void OnInit(in GUIButtonComponentData properties)
        {
            m_screen = Entity.GetComponentOnAncestor<GUIScreenComponent>();
            m_element = Entity.GetComponent<GUIElementComponent>();
            if (properties.ShortcutInput != null)
            {
                m_shortcut = m_screen.Input.Mapper.GetInput(properties.ShortcutInput);
            }
            m_disabled = false;
            m_hover = CheckHover();
            m_currentPress = null;
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            // Update hover
            var hover = CheckHover();
            if (hover != m_hover)
            {
                m_hover = hover;
                if (hover)
                {
                    if (OnHoverEnter != null)
                    {
                        OnHoverEnter.Invoke(this, new ButtonHoverEventArgs());
                    }
                }
                else
                {
                    if (OnHoverLeave != null)
                    {
                        OnHoverLeave.Invoke(this, new ButtonHoverEventArgs());
                    }
                }
            }

            // Update press
            if (m_currentPress == null)
            {
                var press = CheckPress();
                if (press != null)
                {
                    m_currentPress = press;
                    if (OnPressed != null)
                    {
                        OnPressed.Invoke(this, new ButtonPressEventArgs());
                    }
                }
            }
            else
            {
                var press = m_currentPress;
                if (!Entity.Visible ||
                    Disabled ||
                    IsBlocked())
                {
                    m_currentPress = null;
                    if (OnCancelled != null)
                    {
                        OnCancelled.Invoke(this, new ButtonPressEventArgs());
                    }
                }
                else if (m_currentPress.Released)
                {
                    m_currentPress = null;
                    if (OnReleased != null)
                    {
                        OnReleased.Invoke(this, new ButtonPressEventArgs());
                    }
                }
                else if (!m_currentPress.Held)
                {
                    m_currentPress = null;
                    if (OnCancelled != null)
                    {
                        OnCancelled.Invoke(this, new ButtonPressEventArgs());
                    }
                }
            }
        }

        private bool IsBlocked()
        {
            return false; // TODO
        }

        public bool CheckHover()
        {
            if (Entity.Visible && !Disabled && !IsBlocked())
            {
                IMouse mouse;
                return m_screen.CheckMouseHover(m_element.Area, out mouse);
            }
            return false;
        }

        private IPress CheckPress()
        {
            if (Entity.Visible && !IsBlocked())
            {
                // Check touches
                Touch touch;
                if (m_screen.CheckTouchPressed(m_element.Area, out touch))
                {
                    touch.Claim();
                    if (!Disabled)
                    {
                        return new TouchPress(m_screen, touch, m_element);
                    }
                }
                IMouse mouse;
                if (m_screen.CheckMousePressed(m_element.Area, MouseButton.Left, out mouse))
                {
                    if (!Disabled)
                    {
                        return new MousePress(m_screen, mouse, MouseButton.Left, m_element);
                    }
                }

                // Check shortcut keys
                if (m_shortcut != null && m_shortcut.Pressed)
                {
                    if (!Disabled)
                    {
                        return new InputPress(m_shortcut);
                    }
                }
            }
            return null;
        }
    }
}
