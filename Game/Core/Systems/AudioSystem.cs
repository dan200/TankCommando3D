using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Script;
using Dan200.Core.Main;
using Dan200.Core.Assets;
using Dan200.Core.Audio;

namespace Dan200.Core.Systems
{
    internal struct AudioSystemData
    {
    }

    internal class AudioSystem : System<AudioSystemData>
    {
        private IAudio m_audio;
		private IMusicPlayback m_music;

        public IAudio Audio
        {
            get
            {
                return m_audio;
            }
        }

        public AudioSystem(IAudio audio)
        {
            m_audio = audio;
			m_music = null;
        }

        protected override void OnInit(in AudioSystemData properties)
        {
			if (Level.Data.MusicPath != null)
			{
				var music = Music.Get(Level.Data.MusicPath);
				PlayMusic(music, 0.0f, true);
			}
        }

        protected override void OnShutdown()
        {
			if (m_music != null)
			{
				m_music.Stop();
				m_music = null;
			}
        }

		public void PlayMusic(Music music, float transitionTime, bool looping = true)
		{
			if (music == null)
			{
				// Stop music
				if (m_music != null)
				{
					m_music.FadeToVolume(0.0f, transitionTime, true);
					m_music = null;
				}
			}
			else
			{
				// Switch or continue music
				if (m_music == null || m_music.Music != music || m_music.Stopped)
				{
					if (m_music != null)
					{
						m_music.FadeToVolume(0.0f, transitionTime, true);
					}
					m_music = m_audio.PlayMusic(music, looping, transitionTime);
				}
			}
		}
	}
}
