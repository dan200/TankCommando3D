using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal class TextButton : ButtonBase
    {
        private ITexture m_texture;
        private ITexture m_hoverTexture;
        private ITexture m_disabledTexture;
        private ITexture m_heldTexture;
        private string m_text;
        private Font m_font;
        private int m_fontSize;

        public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
                m_text = value;
                RequestRebuild();
            }
        }

        public Font Font
        {
            get
            {
                return m_font;
            }
            set
            {
                m_font = value;
                RequestRebuild();
            }
        }

        public int FontSize
        {
            get
            {
                return m_fontSize;
            }
            set
            {
                m_fontSize = value;
                RequestRebuild();
            }
        }
        
        public ITexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                m_texture = value;
            }
        }

        public ITexture HoverTexture
        {
            get
            {
                return m_hoverTexture;
            }
            set
            {
                m_hoverTexture = value;
            }
        }

        public ITexture DisabledTexure
        {
            get
            {
                return m_disabledTexture;
            }
            set
            {
                m_disabledTexture = value;
            }
        }

        public ITexture HeldTexture
        {
            get
            {
                return m_heldTexture;
            }
            set
            {
                m_heldTexture = value;
            }
        }

        public TextButton(Texture texture, Font font, int fontSize, string text, Colour colour, float width, float height) : base(width, height)
        {
            m_texture = texture;
            m_hoverTexture = texture;
            m_disabledTexture = texture;
            m_heldTexture = texture;

            m_text = text;
            m_font = font;
            m_fontSize = fontSize;

            OnHoverEnter += delegate
            {
                if (!Held)
                {
                    PlayHoverSound();
                }
				RequestRebuild();
            };
			OnHoverLeave += delegate
			{
				RequestRebuild();
			};
            OnPressed += delegate
            {
                PlayDownSound();
                RequestRebuild();
            };
            OnReleased += delegate
            {
                PlayUpSound();
                RequestRebuild();
            };
			OnCancelled += delegate
			{
				RequestRebuild();
			};
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
            // Draw background
			ITexture texture;
            var mouse = false;
            if (Disabled || Blocked)
            {
                texture = m_disabledTexture;
            }
            else if (Held)
            {
                texture = m_heldTexture;
            }
            else if (mouse && Hover)
            {
                texture = m_hoverTexture;
            }
            else
            {
                texture = m_texture;
            }

			var origin = Position;
			float xEdgeWidth = (float)m_texture.Width * 0.25f;
			float yEdgeWidth = (float)m_texture.Height * 0.25f;
			builder.AddNineSlice(origin, origin + Size, xEdgeWidth, yEdgeWidth, xEdgeWidth, yEdgeWidth, texture, Colour.White);

            // Draw text
            var center = origin + 0.5f * Size;
            builder.AddText(m_text, center + new Vector2(0.0f, -0.5f * m_font.GetHeight(m_fontSize)), m_font, m_fontSize, Colour.White, TextAlignment.Center);
        }

        private void PlayHoverSound()
        {
//            Screen.Audio.PlaySound(Sound.Get("sound/menu_highlight.wav"));
        }

        private void PlayDownSound()
        {
  //          Screen.Audio.PlaySound(Sound.Get("sound/menu_down.wav"));
        }

        private void PlayUpSound()
        {
    //        Screen.Audio.PlaySound(Sound.Get("sound/menu_up.wav"));
        }
    }
}
