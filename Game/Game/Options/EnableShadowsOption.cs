using System;
namespace Dan200.Game.Options
{
	internal class EnableShadowsOption : IOption<bool>
	{
		private readonly Game.Game m_game;

		public bool Value
		{
			get
			{
				return m_game.User.Settings.EnableShadows;
			}
			set
			{
				m_game.User.Settings.EnableShadows = value;
				m_game.User.Settings.Save();
			}
		}

		public EnableShadowsOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
