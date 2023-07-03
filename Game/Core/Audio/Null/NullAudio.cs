using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;

namespace Dan200.Core.Audio.Null
{
    internal class NullAudio : IAudio
    {
		private float[] m_volume;

        public Matrix4 ListenerTransform
        {
            get;
            set;
        }

        public Vector3 ListenerVelocity
        {
            get;
            set;
        }

        public NullAudio()
        {
			m_volume = new float[EnumConverter.GetValues<AudioCategory>().Length];
            ListenerTransform = Matrix4.Identity;
            ListenerVelocity = Vector3.Zero;
        }

        public void Dispose()
        {
        }

		public float GetVolume(AudioCategory category)
		{
			return m_volume[EnumConverter.ToInt(category)];
		}

		public void SetVolume(AudioCategory category, float volume)
		{
			App.Assert(volume >= 0.0f && volume <= 1.0f);
			m_volume[EnumConverter.ToInt(category)] = volume;
		}

		public ISoundPlayback PlaySound(Sound sound, bool looping, AudioCategory category)
        {
            return null;
        }

        public IMusicPlayback PlayMusic(Music music, bool looping, float fadeInTime, AudioCategory category)
        {
            return null;
        }

        public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category)
        {
            return null;
        }

        public void Update(float dt)
        {
        }
    }
}

