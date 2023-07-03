using Dan200.Core.Math;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using Dan200.Core.Util;
using Dan200.Core.Main;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALAudio : IAudio, IDisposable
    {
        private const int NUM_SOUND_SOURCES = 16;
        private const int NUM_MUSIC_SOURCES = 2;
        private const int NUM_CUSTOM_SOURCES = 16;
        private const float SPEED_OF_SOUND = 343.0f;

        public static OpenALAudio Instance
        {
            get;
            private set;
        }

        private AudioContext m_context;
        private XRamExtension m_xram;

        private OpenALSoundSource[] m_sound;
        private OpenALMusicPlayback[] m_music;
        private OpenALCustomPlayback[] m_custom;

		private float[] m_volume;
        private Matrix4 m_listenerTransform;
        private Vector3 m_listenerVelocity;

        public XRamExtension XRam
		{
			get
			{
				return m_xram;
			}
		}        

        public Matrix4 ListenerTransform
        {
            get
            {
                return m_listenerTransform;
            }
            set
            {
                m_listenerTransform = value;
                UpdateListenerTransform();
            }
        }

        public Vector3 ListenerVelocity
        {
            get
            {
                return m_listenerVelocity;
            }
            set
            {
                if (m_listenerVelocity != value)
                {
                    m_listenerVelocity = value;
                    UpdateListenerVelocity();
                }
            }
        }

        public OpenALAudio()
        {
            Instance = this;

			m_volume = new float[EnumConverter.GetValues<AudioCategory>().Length];
            m_listenerTransform = Matrix4.Identity;
            m_listenerVelocity = Vector3.Zero;

            // Init context
            m_context = new AudioContext();
            m_xram = new XRamExtension();

            // Create some sources
            m_sound = new OpenALSoundSource[NUM_SOUND_SOURCES];
            for (int i = 0; i < m_sound.Length; ++i)
            {
                m_sound[i] = new OpenALSoundSource();
				m_sound[i].UpdateVolume();
            }
            m_music = new OpenALMusicPlayback[NUM_MUSIC_SOURCES];
            m_custom = new OpenALCustomPlayback[NUM_CUSTOM_SOURCES];

            // Configure OpenAL
            AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);
            AL.DopplerFactor(1.0f);
            AL.DopplerVelocity(SPEED_OF_SOUND);
            ALUtils.CheckError();

			// Set initial state
			UpdateListenerTransform();
            UpdateListenerVelocity();
            ALUtils.CheckError();
        }

		public float GetVolume(AudioCategory e)
		{
			return m_volume[EnumConverter.ToInt(e)];
		}

		public void SetVolume(AudioCategory category, float volume)
		{
			App.Assert(volume >= 0.0f && volume <= 1.0f);
			m_volume[EnumConverter.ToInt(category)] = volume;
			UpdateVolume(category);
		}

        public void Dispose()
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                source.Dispose();
            }
            for (int i = 0; i < m_music.Length; ++i)
            {
                var source = m_music[i];
                if (source != null)
                {
                    source.Dispose();
                }
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var source = m_custom[i];
                if (source != null)
                {
                    source.Dispose();
                }
            }
            m_context.Dispose();

            Instance = null;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                source.Update(dt);
            }
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback != null)
                {
                    playback.Update(dt);
                    if (playback.Stopped)
                    {
                        playback.Dispose();
                        m_music[i] = null;
                    }
                }
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var playback = m_custom[i];
                if (playback != null)
                {
                    playback.Update(dt);
                    if (playback.Stopped)
                    {
                        playback.Dispose();
                        m_custom[i] = null;
                    }
                }
            }
        }

		public ISoundPlayback PlaySound(Sound sound, bool looping, AudioCategory category)
        {
            // Find a free source and play the sound on it
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                if (source.CurrentPlayback == null || source.CurrentPlayback.Stopped)
                {
					return source.Play((OpenALSound)sound, looping, category);
                }
            }
            return null;
        }

        public void StopSound(Sound sound)
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                if (source.CurrentPlayback != null && source.CurrentPlayback.Sound == sound)
                {
                    source.CurrentPlayback.Stop();
                }
            }
        }

        public IMusicPlayback PlayMusic(Music music, bool looping, float fadeInTime, AudioCategory category)
        {
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback == null || playback.Stopped)
                {
                    if (playback != null)
                    {
                        playback.Dispose();
                    }
					m_music[i] = new OpenALMusicPlayback((OpenALMusic)music, looping, fadeInTime, category);
                    return m_music[i];
                }
            }
            return null;
        }

        public void StopMusic(Music music)
        {
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback != null && playback.Music == music)
                {
                    playback.Stop();
                }
            }
        }

		public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category)
        {
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var playback = m_custom[i];
                if (playback == null || playback.Stopped)
                {
                    if (playback != null)
                    {
                        playback.Dispose();
                    }
					m_custom[i] = new OpenALCustomPlayback(source, channels, sampleRate, category);
                    return m_custom[i];
                }
            }
            return null;
        }

		private void UpdateVolume(AudioCategory category)
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
				if (source.CurrentPlayback != null && source.CurrentPlayback.Category == category)
				{
					source.UpdateVolume();
				}
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var source = m_custom[i];
				if (source != null && source.Category == category)
                {
                    source.UpdateVolume();
                }
            }
            for (int i = 0; i < m_music.Length; ++i)
            {
                var source = m_music[i];
				if (source != null && source.Category == category)
                {
                    source.UpdateVolume();
                }
            }
        }

        [ThreadStatic]
        private static float[] s_tempOrientationBuffer;

        private void UpdateListenerTransform()
        {
            var pos = m_listenerTransform.Position;
            AL.Listener(ALListener3f.Position, pos.X, pos.Y, -pos.Z);

            var up = m_listenerTransform.Up;
            var fwd = m_listenerTransform.Forward;
            if (s_tempOrientationBuffer == null)
            {
                s_tempOrientationBuffer = new float[6];
            }
            s_tempOrientationBuffer[0] = fwd.X;
            s_tempOrientationBuffer[1] = fwd.Y;
            s_tempOrientationBuffer[2] = -fwd.Z;
            s_tempOrientationBuffer[3] = up.X;
            s_tempOrientationBuffer[4] = up.Y;
            s_tempOrientationBuffer[5] = -up.Z;
            AL.Listener(ALListenerfv.Orientation, ref s_tempOrientationBuffer);
        }

        private void UpdateListenerVelocity()
        {
            var vel = m_listenerVelocity;
            AL.Listener(ALListener3f.Velocity, vel.X, vel.Y, -vel.Z);
        }
    }
}

