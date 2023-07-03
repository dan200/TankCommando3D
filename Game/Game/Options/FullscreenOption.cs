using System;
using Dan200.Core.Audio;

namespace Dan200.Game.Options
{
	internal class FullscreenOption : IOption<bool>
	{
		private readonly Game.Game m_game;

		public bool Value
		{
			get
			{
				return m_game.User.Settings.Fullscreen;
			}
			set
			{
				m_game.Window.Fullscreen = value;
				m_game.User.Settings.Fullscreen = value;
				m_game.User.Settings.Save();
			}
		}

		public FullscreenOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
