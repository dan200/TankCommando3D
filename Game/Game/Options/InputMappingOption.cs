using System;
using Dan200.Core.Input;

namespace Dan200.Game.Options
{
    internal class InputMappingOption : IOption<InputOrigin>
    {
        private Game.Game m_game;
        private string m_inputName;
        private int m_index;

        public InputMappingOption(Game.Game game, string inputName, int index)
        {
            m_game = game;
            m_inputName = inputName;
            m_index = index;
        }

        public InputOrigin Value
        {
            get
            {
                InputOrigin[] origins;
                if( m_game.User.Settings.InputMappings.TryGetValue(m_inputName, out origins) &&
                    origins.Length > m_index )
                {
                    return origins[m_index];
                }
                return InputOrigin.Invalid;
            }
            set
            {
                InputOrigin[] origins;
                if (m_game.User.Settings.InputMappings.TryGetValue(m_inputName, out origins))
                {
                    if (m_index >= origins.Length)
                    {
                        if (value.IsValid)
                        {
                            Array.Resize(ref origins, m_index + 1);
                            origins[m_index] = value;
                            m_game.User.Settings.InputMappings[m_inputName] = origins;
                        }
                    }
                    else
                    {
                        origins[m_index] = value;
                    }
                }
                else
                {
                    if (value.IsValid)
                    {
                        origins = new InputOrigin[m_index + 1];
                        origins[m_index] = value;
                        m_game.User.Settings.InputMappings[m_inputName] = origins;
                    }
                }
                m_game.User.Settings.Save();
            }
        }
    }
}
