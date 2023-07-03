using System;
namespace Dan200.Game.Options
{
	internal class FOVOption : IOption<float>
	{
		private readonly Game.Game m_game;

		public float Value
		{
			get
			{
				return m_game.User.Settings.FOV;
			}
			set
			{
				m_game.User.Settings.FOV = value;
				m_game.User.Settings.Save();
			}
		}

		public FOVOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
