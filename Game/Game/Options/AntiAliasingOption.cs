using System;
using Dan200.Core.Audio;
using Dan200.Core.Render;

namespace Dan200.Game.Options
{
	internal class AntiAliasingOption : IOption<AntiAliasingMode>
	{
		private readonly Game.Game m_game;

		public AntiAliasingMode Value
		{
			get
			{
				return m_game.User.Settings.AntiAliasingMode;
			}
			set
			{
				m_game.User.Settings.AntiAliasingMode = value;
				m_game.User.Settings.Save();
			}
		}

		public AntiAliasingOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
