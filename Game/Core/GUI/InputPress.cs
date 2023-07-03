using Dan200.Core.Input;

namespace Dan200.Core.GUI
{
	internal class InputPress : IPress
    {
        private Input.Input m_input;

        public bool Held
        {
            get
            {
                return m_input.Held;
            }
        }

        public bool Released
        {
            get
            {
                return m_input.Released;
            }
        }

        public InputPress(Input.Input input)
        {
            m_input = input;
        }
    }
}
