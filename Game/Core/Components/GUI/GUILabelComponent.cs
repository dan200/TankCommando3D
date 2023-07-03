using System;
using System.Globalization;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Level;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.GUI
{
    internal struct GUILabelComponentData
    {
        [Optional(Default = "")]
        public string Text;

        public string Font;
        public int FontSize;

        [Optional(Default = TextAlignment.Left)]
        public TextAlignment Alignment;

        [Optional(Default = VerticalTextAlignment.Top)]
        public VerticalTextAlignment VerticalAlignment;

        [Optional(Default = TextStyle.Default)]
        public TextStyle Style;

        [Optional(255, 255, 255, 255)]
        public Colour Colour;

        [Optional(Default = false)]
        public bool ParseImages;

        [Optional(Default = false)]
        public bool Wrap;
    }

    [RequireComponent(typeof(GUIElementComponent))]
    [AfterComponent(typeof(GUIImageComponent))]
    [AfterComponent(typeof(GUINineSliceComponent))]
    internal class GUILabelComponent : Component<GUILabelComponentData>, IGUIRebuild
    {
        private GUIElementComponent m_element;

        private Font m_font;
        private Colour m_colour;
        private TextAlignment m_alignment;
        private VerticalTextAlignment m_verticalAlignment;
        private TextStyle m_style;
        private bool m_parseImages;
        private string m_text;
        private int m_fontSize;
        private bool m_wrap;

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
                    m_element.RequestRebuild();
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
                    m_element.RequestRebuild();
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
                if (m_colour != value)
                {
                    m_colour = value;
                    m_element.RequestRebuild();
                }
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
                    m_element.RequestRebuild();
                }
            }
        }

        public VerticalTextAlignment VerticalAlignment
        {
            get
            {
                return m_verticalAlignment;
            }
            set
            {
                if (m_verticalAlignment != value)
                {
                    m_verticalAlignment = value;
                    m_element.RequestRebuild();
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
                    m_element.RequestRebuild();
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
                    m_element.RequestRebuild();
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
                    m_element.RequestRebuild();
                }
            }
        }

        public bool Wrap
        {
            get
            {
                return m_wrap;
            }
            set
            {
                if(m_wrap != value)
                {
                    m_wrap = value;
                    m_element.RequestRebuild();
                }
            }
        }

        protected override void OnInit(in GUILabelComponentData properties)
        {
            m_element = Entity.GetComponent<GUIElementComponent>();
            m_font = Font.Get(properties.Font);
            m_text = properties.Text;
            m_style = properties.Style;
            m_colour = properties.Colour;
            m_alignment = properties.Alignment;
            m_verticalAlignment = properties.VerticalAlignment;
            m_parseImages = properties.ParseImages;
            m_fontSize = properties.FontSize;
            m_wrap = properties.Wrap;
        }

        protected override void OnShutdown()
        {
        }

        private string ApplyTextStyle(string text, TextStyle style, Language language)
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

        public void Rebuild(GUIBuilder builder)
        {
            var styledText = ApplyTextStyle(m_text, m_style, null); // TODO
            if(styledText.Length == 0)
            {
                return;
            }

            var position = m_element.Position;
            var width = m_element.Width;
            var height = m_element.Height;
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

            if(m_verticalAlignment != VerticalTextAlignment.Top)
            {
                var textHeight = m_wrap ? m_font.Measure(styledText, m_fontSize, m_parseImages, width).Y : m_font.GetHeight(m_fontSize);
                switch(m_verticalAlignment)
                {
                    case VerticalTextAlignment.Center:
                        position.Y += 0.5f * (height - textHeight);
                        break;
                    case VerticalTextAlignment.Bottom:
                        position.Y += height - textHeight;
                        break;
                }
            }

            builder.AddText(styledText, position, m_font, m_fontSize, m_colour, m_alignment, m_parseImages, m_wrap ? width : float.MaxValue);
        }
    }
}
