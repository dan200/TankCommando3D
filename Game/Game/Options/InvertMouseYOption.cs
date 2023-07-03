using System;
using Dan200.Core.Audio;
using Dan200.Core.Render;

namespace Dan200.Game.Options
{
	internal class InvertMouseYOption : IOption<bool>
	{
		private readonly Game.Game m_game;

		public bool Value
		{
			get
			{
                return m_game.User.Settings.InvertMouseY;
			}
			set
			{
                m_game.User.Settings.InvertMouseY = value;
				m_game.User.Settings.Save();
			}
		}

		public InvertMouseYOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
