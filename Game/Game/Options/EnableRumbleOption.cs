using System;
namespace Dan200.Game.Options
{
	internal class EnableRumbleOption : IOption<bool>
	{
		private readonly Game.Game m_game;

		public bool Value
		{
			get
			{
				return m_game.User.Settings.EnableGamepadRumble;
			}
			set
			{
				m_game.User.Settings.EnableGamepadRumble = value;
				m_game.User.Settings.Save();
			}
		}

		public EnableRumbleOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
