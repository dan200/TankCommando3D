using System;
using Dan200.Core.Audio;
using Dan200.Core.Render;

namespace Dan200.Game.Options
{
	internal class MouseSensitivityOption : IOption<float>
	{
		private readonly Game.Game m_game;

		public float Value
		{
			get
			{
				return m_game.User.Settings.MouseSensitivity;
			}
			set
			{
				m_game.User.Settings.MouseSensitivity = value;
				m_game.User.Settings.Save();
			}
		}

		public MouseSensitivityOption(Game.Game game)
		{
			m_game = game;
		}
	}
}
