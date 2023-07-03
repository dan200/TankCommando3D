
using System.Globalization;
using Dan200.Core.Assets;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    internal enum TextStyle
    {
        Default,
        UpperCase,
        LowerCase,
    }

    internal static class TextStyleExtensions
    {
        public static string Apply(this TextStyle style, string text, Language language)
        {
			var culture = (language != null) ? language.Culture : CultureInfo.InvariantCulture;
            switch (style)
            {
                case TextStyle.UpperCase:
                    {
						return text.ToUpper(culture);
                    }
                case TextStyle.LowerCase:
                    {
                        return text.ToLower(culture);
                    }
                default:
                    {
                        return text;
                    }
            }
        }
    }

    internal class Label : Element
    {
        private Font m_font;
        private Colour m_colour;
        private TextAlignment m_alignment;
        private TextStyle m_style;
        private bool m_parseImages;
        private string m_text;
        private int m_fontSize;
		private bool m_autoSize;

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
					if (m_autoSize)
					{
						Size = Measure();
					}
                    RequestRebuild();
                }
            }
        }

        public TextStyle Style
        {
            get
            {
                return m_style;
            }
            set
            {
                if (m_style != value)
                {
                    m_style = value;
					if (m_autoSize)
					{
						Size = Measure();
					}
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
            }
        }

        public TextAlignment Alignment
        {
            get
            {
                return m_alignment;
            }
            set
            {
                if (m_alignment != value)
                {
                    m_alignment = value;
                    RequestRebuild();
                }
            }
        }

        public bool ParseImages
        {
            get
            {
                return m_parseImages;
            }
            set
            {
                if (m_parseImages != value)
                {
                    m_parseImages = value;
					if (m_autoSize)
					{
						Size = Measure();
					}
                    RequestRebuild();
                }
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
                if (m_font != value)
                {
                    m_font = value;
					if (m_autoSize)
					{
						Size = Measure();
					}
                    RequestRebuild();
                }
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
                if (m_fontSize != value)
                {
                    m_fontSize = value;
					if (m_autoSize)
					{
						Size = Measure();
					}
                    RequestRebuild();
                }
            }
        }

		public bool AutoSize
		{
			get
			{
				return m_autoSize;
			}
			set
			{
				if (m_autoSize != value)
				{
					m_autoSize = value;
					if (m_autoSize)
					{
						Size = Measure();
					}
				}
			}
		}

		public Label(Font font, int fontSize, string text, Colour colour)
		{
			m_font = font;
			m_text = text;
			m_style = TextStyle.Default;
			m_colour = colour;
			m_alignment = TextAlignment.Left;
			m_parseImages = true;
			m_fontSize = fontSize;
			m_autoSize = true;
			Size = Measure();
		}

		public Label(Font font, int fontSize, string text, Colour colour, TextAlignment alignment, float width)
        {
            m_font = font;
            m_text = text;
            m_style = TextStyle.Default;
            m_colour = colour;
			m_alignment = alignment;
            m_parseImages = true;
            m_fontSize = fontSize;
			m_autoSize = false;
			Size = new Vector2(width, Measure().Y);
        }

        protected override void OnInit()
        {
			if (m_autoSize)
			{
				Size = Measure();
			}
        }

        protected override void OnUpdate(float dt)
        {
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
            var styledString = m_style.Apply(m_text, Screen.Language);
			var position = Position;
			var width = Width;
			switch (m_alignment)
			{
				case TextAlignment.Left:
				default:
					break;
				case TextAlignment.Center:
					position.X += 0.5f * width;
					break;
				case TextAlignment.Right:
					position.X += width;
					break;
			}
			builder.AddText(styledString, position, m_font, m_fontSize, m_colour, m_alignment, m_parseImages, width);
        }

		private Vector2 Measure()
		{
			var styledString = m_style.Apply(m_text, (Screen != null) ? Screen.Language : null);
			return m_font.Measure(styledString, m_fontSize, m_parseImages);
		}
    }
}
