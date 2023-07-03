using System;
using Dan200.Core.Main;

namespace Dan200.Core.Input
{
    internal class Axis
    {
        private Input m_positive;
        private Input m_negative;

        public float Value
        {
            get
            {
                return m_positive.Value - m_negative.Value;
            }
        }

        public string Prompt
        {
            get
            {
                var promptA = m_positive.Prompt;
                var promptB = m_negative.Prompt;
                if (promptA == promptB)
                {
                    return promptA;
                }
                else
                {
                    return promptA + '/' + promptB;
                }
            }
        }

        public Axis(Input positive, Input negative)
        {
            App.Assert(positive != null);
            App.Assert(negative != null);
            m_positive = positive;
            m_negative = negative;
        }
    }
}
