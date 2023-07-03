using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System.Collections.Generic;

namespace Dan200.Core.GUI
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

    internal abstract class ButtonBase : Element, IAreaHolder
    {
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

        public event StructEventHandler<ButtonBase, ButtonHoverEventArgs> OnHoverEnter;
        public event StructEventHandler<ButtonBase, ButtonHoverEventArgs> OnHoverLeave;
        public event StructEventHandler<ButtonBase, ButtonPressEventArgs> OnPressed;
        public event StructEventHandler<ButtonBase, ButtonPressEventArgs> OnReleased;
        public event StructEventHandler<ButtonBase, ButtonPressEventArgs> OnCancelled;

        protected ButtonBase(float width, float height)
        {
            Size = new Vector2(width, height);

            m_shortcut = null;
            m_disabled = false;
			m_hover = false;
            m_currentPress = null;
        }

        public Quad GetSubArea(int i)
        {
            return Area;
        }

        protected override void OnInit()
        {
			m_hover = CheckHover();
            m_currentPress = null;
        }

        protected override void OnUpdate(float dt)
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
                if (!Visible ||
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
			var modalDialog = Screen.ModalDialog;
			if (modalDialog != null)
			{
				Element parent = this;
				do
				{
					if (parent == modalDialog)
					{
						return false;
					}
					parent = parent.Parent;
				}
				while (parent != null);
				return true;
			}
			return false;
        }

        public bool CheckHover()
        {
            if (Visible && !Disabled && !IsBlocked())
            {                
                // Check main area
                if (Area.Contains(Screen.MousePosition))
                {
					return true;
                }
            }
			return false;
        }

        private IPress CheckPress()
        {
            if (Visible && !IsBlocked())
            {
                // Check main area
                Touch touch;               
                if (Screen.CheckTouchPressed(Area, out touch))
                {
                    touch.Claim();
                    if (!Disabled)
                    {
                        return new TouchPress(Screen, touch, this, 0);
                    }
                }
                IMouse mouse;
                if (Screen.CheckMousePressed(Area, MouseButton.Left, out mouse))
                {
                    if (!Disabled)
                    {
                        return new MousePress(Screen, mouse, MouseButton.Left, this, 0);
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
