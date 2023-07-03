using System;
using Dan200.Core.Audio;
using Dan200.Core.Render;

namespace Dan200.Game.Options
{
	internal class VSyncOption : IOption<bool>
	{
		private readonly Game.Game m_game;

		public bool Value
		{
			get
			{
				return m_game.User.Settings.VSync;
			}
			set
			{
                m_game.Window.VSync = value;
				m_game.User.Settings.VSync = value;
				m_game.User.Settings.Save();
			}
		}

		public VSyncOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
