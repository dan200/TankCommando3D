using System;
using Dan200.Core.Render;

namespace Dan200.Game.Options
{
	internal class ResolutionOption : IOption<Resolution>
	{
		private Game.Game m_game;

		public Resolution Value
		{
			get
			{
				return new Resolution(
					m_game.User.Settings.FullscreenWidth,
					m_game.User.Settings.FullscreenHeight
				);
			}
			set
			{
				m_game.User.Settings.FullscreenWidth = value.Width;
				m_game.User.Settings.FullscreenHeight = value.Height;
				m_game.User.Settings.Save();
				m_game.Resize();
			}
		}

		public ResolutionOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
