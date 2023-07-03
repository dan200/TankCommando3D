using System;
namespace Dan200.Game.Options
{
	internal class GammaOption : IOption<float>
	{
		private readonly Game.Game m_game;

		public float Value
		{
			get
			{
				return m_game.User.Settings.Gamma;
			}
			set
			{
				m_game.MainView.PostProcessSettings.Gamma = value;
				m_game.User.Settings.Gamma = value;
				m_game.User.Settings.Save();
			}
		}

		public GammaOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
