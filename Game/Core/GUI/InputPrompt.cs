using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal class InputPrompt : Element
    {
        private string m_text;
        private Input.Input m_input;
        private string m_lastPrompt;

        private Font m_font;
        private int m_fontSize;
        private Colour m_colour;
        private TextAlignment m_alignment;

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
        }

        public InputPrompt(string text, Input.Input input, Font font, int fontSize, Colour colour, TextAlignment alignment)
        {
            m_text = text;
            m_input = input;
            m_lastPrompt = input.Prompt;

            m_font = font;
            m_fontSize = fontSize;
            m_colour = colour;
            m_alignment = alignment;
            Size = font.Measure(m_text, m_fontSize, true);
        }

        protected override void OnInit()
        {
            Size = m_font.Measure(BuildString(), m_fontSize, true);
        }

        protected override void OnUpdate(float dt)
        {
            if(m_input.Prompt != m_lastPrompt)
            {
                m_lastPrompt = m_input.Prompt;
                RequestRebuild();
                Size = m_font.Measure(BuildString(), m_fontSize, true);
            }
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
            builder.AddText(BuildString(), Position, m_font, m_fontSize, m_colour, m_alignment, true);
        }

        private string BuildString()
        {
            return m_lastPrompt + ' ' + m_text;
        }
    }
}

