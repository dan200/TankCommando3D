using Dan200.Core.Math;
using Dan200.Core.Main;

namespace Dan200.Core.Input
{
    internal class Input
    {
        private const float HOLD_THRESHOLD = 0.51f;

        private string m_name;
        private string m_prompt;
        private float m_previousValue;
        private float m_value;

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public string Prompt
        {
            get
            {
                return m_prompt;
            }
            set
            {
                m_prompt = value;
            }
        }

        public float Value
        {
            get
            {
                return m_value;
            }
        }

        public float PreviousValue
        {
            get
            {
                return m_previousValue;
            }
        }

        public bool Held
        {
            get
            {
                return m_value >= HOLD_THRESHOLD;
            }
        }

        public bool Pressed
        {
            get
            {
                return m_value >= HOLD_THRESHOLD && m_previousValue < HOLD_THRESHOLD;
            }
        }

        public bool Released
        {
            get
            {
                return m_value < HOLD_THRESHOLD && m_previousValue >= HOLD_THRESHOLD;
            }
        }

        public Input(string name, string prompt)
        {
            m_name = name;
            m_prompt = prompt;
            m_previousValue = 0.0f;
            m_value = 0.0f;
        }

        public void Update(float value)
        {
            App.Assert(value >= 0.0f && value <= 1.0f);
            m_previousValue = m_value;
            m_value = value;
        }
    }
}

