
using System;
using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.GUI
{
	internal class TextBox : Element, IAreaProvider
    {
		private Font m_font;
        private int m_fontSize;
        private Colour m_colour;
        private Colour m_hoverColour;
		private ITexture m_texture;

        private bool m_focus;
        private bool m_hover;
		private string m_text;
		private IPress m_press;
		private bool m_pressIsInsideBox;

		public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
				if (m_text != value)
				{
					m_text = value;
                    FireOnTextChanged();
					RequestRebuild();
				}
            }
        }

        public bool Focus
        {
            get
            {
                return m_focus;
            }
            set
            {
                if (m_focus != value)
                {
                    m_focus = value;
					RequestRebuild();
                }
            }
        }

		public Colour Colour
		{
			get
			{
				return m_colour;
			}
			set
			{
				m_colour = value;
				RequestRebuild();
			}
		}

		public Colour HoverColour
		{
			get
			{
				return m_hoverColour;
			}
			set
			{
				m_hoverColour = value;
				RequestRebuild();
			}
		}

		public event StructEventHandler<TextBox> OnTextChanged;

        public TextBox(Texture texture, Font font, int fontSize, string text, Colour colour, float width, float height)
        {
            Size = new Vector2(width, height);

			m_font = font;
            m_fontSize = fontSize;
            m_colour = colour;
            m_hoverColour = colour;
            m_texture = texture;

            m_focus = false;
            m_hover = false;
			m_text = text;

			m_press = null;
			m_pressIsInsideBox = false;
        }


        private void Backspace()
        {
			if (m_text.Length > 0)
            {
                if (char.IsLowSurrogate(m_text[m_text.Length - 1]))
                {
                    m_text = m_text.Substring(0, m_text.Length - 2);
                }
                else
                {
                    m_text = m_text.Substring(0, m_text.Length - 1);
                }
            }
        }

        private void Char(int codepoint)
        {
			var newText = m_text + char.ConvertFromUtf32(codepoint);
			var width = m_font.Measure(newText, m_fontSize, false).X;
            if (width <= Width - 12.0f)
            {
				m_text = newText;
            }
        }

		protected override void OnInit()
		{
		}

        protected override void OnUpdate(float dt)
        {
			// Update hover
			bool hover = Visible && !IsBlocked() && Area.Contains(Screen.MousePosition);
            if (hover != m_hover)
            {
                m_hover = hover;
				RequestRebuild();
            }

			// Update focus
			if (m_press == null)
			{
				m_press = CheckPress(out m_pressIsInsideBox);
			}
			else
			{
				if (!Visible || IsBlocked())
				{
					m_press = null;
				}
				else if (m_press.Released)
				{
					Focus = m_pressIsInsideBox;
					RequestRebuild();
					m_press = null;
				}
				else if (!m_press.Held)
				{
					m_press = null;
				}
			}

			// Update text input
			if (Focus && Visible && !IsBlocked())
            {
                var text = Screen.InputDevices.Keyboard.Text;
				if (text.Length > 0)
				{
					var oldText = m_text;
					for (int i = 0; i < text.Length; ++i)
					{
						if (text[i] == '\b')
						{
							Backspace();
						}
						else if (char.IsHighSurrogate(text, i) && (i + 1 < text.Length && char.IsLowSurrogate(text, i + 1)))
						{
							Char(char.ConvertToUtf32(text, i));
						}
						else if (!char.IsSurrogate(text, i))
						{
							Char(text[i]);
						}
					}
					if (m_text != oldText)
					{
						FireOnTextChanged();
						RequestRebuild();
					}
				}
            }
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
            // Background
            var origin = Position;
            float xEdgeWidth = (float)m_texture.Width * 0.25f;
            float yEdgeWidth = (float)m_texture.Height * 0.25f;
			builder.AddNineSlice(origin, origin + Size, xEdgeWidth, yEdgeWidth, xEdgeWidth, yEdgeWidth, m_texture, Colour.White);

			// Text
			var text = m_focus ? (m_text + '_') : m_text;
			var colour = (!m_focus && m_hover) ? m_hoverColour : m_colour;
			var pos = Center + new Vector2(0.0f, -0.5f * m_font.GetHeight(m_fontSize));
			builder.AddText(text, pos, m_font, m_fontSize, colour, TextAlignment.Center);
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

		private IPress CheckPress(out bool o_isInsideBox)
		{
			if (Visible && !IsBlocked())
			{
				// Check touch
				Touch touch;
				if (Screen.CheckTouchPressed(Screen.Area, out touch))
				{
					if (GetSubArea(0).Contains(touch.StartPosition.ToVector2()))
					{
						touch.Claim();
						o_isInsideBox = true;
						return new TouchPress(Screen, touch, this);
					}
					else
					{
						o_isInsideBox = false;
						return new TouchPress(Screen, touch);
					}
				}

                // Check mouse
                IMouse mouse;
				if (Screen.CheckMousePressed(Screen.Area, MouseButton.Left, out mouse))
				{
					if (GetSubArea(0).Contains(Screen.MousePosition))
					{
						o_isInsideBox = true;
                        return new MousePress(Screen, mouse, MouseButton.Left, this);
					}
					else
					{
						o_isInsideBox = false;
                        return new MousePress(Screen, mouse, MouseButton.Left);
					}
				}
			}
			o_isInsideBox = false;
			return null;
		}

		private void FireOnTextChanged()
		{
			if (OnTextChanged != null)
			{
				OnTextChanged.Invoke(this, StructEventArgs.Empty);
			}
		}

		public Quad GetSubArea(int index)
		{
			return Area;
		}
	}
}