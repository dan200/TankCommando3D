using System;
using Dan200.Core.Audio;

namespace Dan200.Game.Options
{
	internal class VolumeOption : IOption<float>
	{
		private readonly Game.Game m_game;
		private readonly AudioCategory m_category;

		public AudioCategory Category
		{
			get
			{
				return m_category;
			}
		}

		public float Value
		{
			get
			{
				return m_game.User.Settings.Volume[m_category];
			}
			set
			{
				m_game.Audio.SetVolume(m_category, value);
				m_game.User.Settings.Volume[m_category] = value;
				m_game.User.Settings.Save();
			}
		}

		public VolumeOption(Game.Game game, AudioCategory category)
		{
			m_game = game;
			m_category = category;
		}
	}
}
