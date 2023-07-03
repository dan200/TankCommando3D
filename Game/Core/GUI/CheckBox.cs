
using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.GUI
{
	internal class CheckBox : Element, IAreaProvider
    {
		private Font m_font;
        private int m_fontSize;
        private Colour m_colour;
		private ITexture m_tickedTexture;
		private ITexture m_untickedTexture;

		private string m_label;
        private bool m_value;
		private IPress m_press;

		public string Label
		{
			get
			{
				return m_label;
			}
			set
			{
				if (m_label != value)
				{
					m_label = value;
					RequestRebuild();
				}
			}
		}

		public bool Value
        {
            get
            {
				return m_value;
            }
            set
            {
				if (m_value != value)
				{
					m_value = value;
                    FireOnValueChanged();
					RequestRebuild();
				}
            }
        }

		public ITexture TickedTexture
		{
			get
			{
				return m_tickedTexture;
			}
			set
			{
				m_tickedTexture = value;
				RequestRebuild();
			}
		}

		public ITexture UntickedTexture
		{
			get
			{
				return m_untickedTexture;
			}
			set
			{
				m_untickedTexture = value;
				RequestRebuild();
			}
		}

		public event StructEventHandler<CheckBox> OnValueChanged;

		public CheckBox(ITexture offTexture, ITexture onTexture, bool value, string label, Font font, int fontSize, Colour colour, float width, float height)
        {
            Size = new Vector2(width, height);

			m_font = font;
            m_fontSize = fontSize;
            m_colour = colour;
			m_tickedTexture = onTexture;
			m_untickedTexture = offTexture;

			m_value = value;
			m_label = label;
			m_press = null;
        }

		protected override void OnInit()
		{
		}
		       
        protected override void OnUpdate(float dt)
        {
			// Update press
			if (m_press == null)
			{
				m_press = CheckPress();
			}
			else
			{
				if (!Visible || IsBlocked())
				{
					m_press = null;
				}
				else if (m_press.Released)
				{
					m_value = !m_value;
					FireOnValueChanged();
					RequestRebuild();
				}
				else if (!m_press.Held)
				{
					m_press = null;
				}
			}
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
			// Checkbox
			var texture = m_value ? m_tickedTexture : m_untickedTexture;
			float width = ((float)texture.Width / (float)texture.Height) * Height;
			builder.AddQuad(
				Position,
				Position + new Vector2(width, Height),
				texture
			);

			// Label
			var pos = new Vector2(Position.X, Center.Y) + new Vector2(width + 8.0f, -0.5f * m_font.GetHeight(m_fontSize));
			builder.AddText(m_label, pos, m_font, m_fontSize, m_colour);
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

		private IPress CheckPress()
		{
			if (Visible && !IsBlocked())
			{
				// Check touch
				Touch touch;
				if (Screen.CheckTouchPressed(GetSubArea(0), out touch))
				{
					touch.Claim();
					return new TouchPress(Screen, touch, this);
				}

                // Check mouse
                IMouse mouse;
				if (Screen.CheckMousePressed(GetSubArea(0), MouseButton.Left, out mouse))
				{
					return new MousePress(Screen, mouse, MouseButton.Left, this);
				}
			}
			return null;
		}

		public Quad GetSubArea(int index)
		{
			var texture = m_value ? m_tickedTexture : m_untickedTexture;
			float width = ((float)texture.Width / (float)texture.Height) * Height;
			return new Quad(Position, width, Height);
		}

		private void FireOnValueChanged()
		{
			if (OnValueChanged != null)
			{
				OnValueChanged.Invoke(this, StructEventArgs.Empty);
			}
		}
    }
}