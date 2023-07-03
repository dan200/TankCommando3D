using Dan200.Core.Math;
using System.Collections.Generic;
using Dan200.Core.Util;
using System;

namespace Dan200.Core.Audio
{
    internal class AudioEmitter : IDisposable
    {
        private IAudio m_audio;
        private Vector3 m_position;
        private Vector3 m_velocity;
        private float m_minRange;
        private float m_maxRange;
        private List<IStoppablePlayback> m_sounds;

        public Vector3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                if (m_position != value)
                {
                    m_position = value;
                    foreach(var playback in m_sounds)
                    {
                        if(!playback.Stopped && playback is ISpatialPlayback)
                        {
                            var spatial = (ISpatialPlayback)playback;
                            spatial.Position = value;
                        }
                    }
                }
            }
        }

        public Vector3 Velocity
        {
            get
            {
                return m_velocity;
            }
            set
            {
                if (m_velocity != value)
                {
                    m_velocity = value;
                    foreach (var playback in m_sounds)
                    {
                        if (!playback.Stopped && playback is ISpatialPlayback)
                        {
                            var spatial = (ISpatialPlayback)playback;
                            spatial.Velocity = value;
                        }
                    }
                }
            }
        }

        public float MinRange
        {
            get
            {
                return m_minRange;
            }
            set
            {
                if(m_minRange != value)
                {
                    m_minRange = value;
                    foreach (var playback in m_sounds)
                    {
                        if (!playback.Stopped && playback is ISpatialPlayback)
                        {
                            var spatial = (ISpatialPlayback)playback;
                            spatial.MinRange = value;
                        }
                    }
                }
            }
        }

        public float MaxRange
        {
            get
            {
                return m_maxRange;
            }
            set
            {
                if (m_maxRange != value)
                {
                    m_maxRange = value;
                    foreach (var playback in m_sounds)
                    {
                        if (!playback.Stopped && playback is ISpatialPlayback)
                        {
                            var spatial = (ISpatialPlayback)playback;
                            spatial.MaxRange = value;
                        }
                    }
                }
            }
        }

        public AudioEmitter(IAudio audio)
        {
            m_audio = audio;
            m_position = Vector3.Zero;
            m_velocity = Vector3.Zero;
            m_minRange = 5.0f;
            m_maxRange = 15.0f;
            m_sounds = new List<IStoppablePlayback>();
        }

        public void Dispose()
        {
            foreach(var playback in m_sounds)
            {
                if (!playback.Stopped)
                {
                    playback.Stop();
                }
            }
            m_sounds = null;
        }

		public ISoundPlayback PlaySound(Sound sound, bool looping = false, AudioCategory category = AudioCategory.Sound)
        {
            var result = m_audio.PlaySound(sound, looping, category);
            StorePlayback(result);
            return result;
        }

        public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category = AudioCategory.Sound)
        {
			var result = m_audio.PlayCustom(source, channels, sampleRate, category);
            StorePlayback(result);
            return result;
        }

        public void Update()
        {
            for (int i = m_sounds.Count - 1; i >= 0; --i)
            {
                var playback = m_sounds[i];
                if (playback.Stopped)
                {
                    m_sounds.UnorderedRemoveAt(i);
                }
            }
        }

        private void StorePlayback(IStoppablePlayback playback)
        {
            if (playback != null)
            {
                m_sounds.Add(playback);
                if(playback is ISpatialPlayback)
                {
                    var spatial = (ISpatialPlayback)playback;
                    spatial.Position = m_position;
                    spatial.Velocity = m_velocity;
                    spatial.MinRange = m_minRange;
                    spatial.MaxRange = m_maxRange;
                }
            }
        }
    }
}

