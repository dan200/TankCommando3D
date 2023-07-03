
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.GUI
{
	internal class Slider : Element
    {
		private ITexture m_barTexture;
		private ITexture m_handleTexture;

		private float m_value;
		private ISpatialPress m_press;

		public float Value
        {
            get
            {
				return m_value;
            }
            set
            {
				if (m_value != value)
				{
					App.Assert(value >= 0.0f && value <= 1.0f);
					m_value = value;
                    FireOnValueChanged();
					RequestRebuild();
				}
            }
        }

		public ITexture BarTexture
		{
			get
			{
				return m_barTexture;
			}
			set
			{
				m_barTexture = value;
				RequestRebuild();
			}
		}

		public ITexture HandleTexture
		{
			get
			{
				return m_handleTexture;
			}
			set
			{
				m_handleTexture = value;
				RequestRebuild();
			}
		}

		public event StructEventHandler<Slider> OnValueChanged;

		public Slider(ITexture barTexture, ITexture handleTexture, float value, float width, float height)
        {
			App.Assert(value >= 0.0f && value <= 1.0f);
            Size = new Vector2(width, height);

			m_barTexture = barTexture;
			m_handleTexture = handleTexture;

			m_value = value;
			m_press = null;
        }

		private float GetValueFromPosition(Vector2 position)
		{
			var handleWidth = ((float)m_handleTexture.Width / (float)m_handleTexture.Height) * Height;
			var x = position.X - Position.X;
			return Mathf.Clamp((x - (0.5f * handleWidth)) / (Width - handleWidth), 0.0f, 1.0f);
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
				if (m_press != null)
				{
					Value = GetValueFromPosition(m_press.CurrentPosition);
				}
			}
			else
			{
				if (!Visible || IsBlocked() || !m_press.Held)
				{
					m_press = null;
				}
				else
				{
					Value = GetValueFromPosition(m_press.CurrentPosition);
				}
			}
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
			// Bar
			float xEdgeWidth = (float)m_barTexture.Width * 0.25f * (Height / (float)m_barTexture.Height);
			builder.AddThreeSlice(Position, Position + Size, xEdgeWidth, xEdgeWidth, m_barTexture);

			// Handle
			var handleWidth = ((float)m_handleTexture.Width / (float)m_handleTexture.Height) * Height;
			var pos = Position + new Vector2(m_value * (Width - handleWidth), 0.0f);
			builder.AddQuad(pos, pos + new Vector2(handleWidth, Height), m_handleTexture);
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

		private ISpatialPress CheckPress()
		{
			if (Visible && !IsBlocked())
			{
				// Check touch
				Touch touch;
				if (Screen.CheckTouchPressed(Area, out touch))
				{
					touch.Claim();
					return new TouchPress(Screen, touch);
				}

                // Check mouse
                IMouse mouse;
				if (Screen.CheckMousePressed(Area, MouseButton.Left, out mouse))
				{
					return new MousePress(Screen, mouse, MouseButton.Left);
				}
			}
			return null;
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