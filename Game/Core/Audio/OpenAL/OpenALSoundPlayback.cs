using Dan200.Core.Math;
using OpenTK.Audio.OpenAL;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALSoundPlayback : ISoundPlayback
    {
		private readonly OpenALSoundSource m_source;
		private readonly Sound m_sound;
		private readonly bool m_looping;
		private readonly AudioCategory m_category;

        private float m_volume;
        private Vector3 m_position;
        private Vector3 m_velocity;
        private float m_minRange;
        private float m_maxRange;
        private bool m_complete;

        public Sound Sound
        {
            get
            {
                return m_sound;
            }
        }

        public bool Looping
        {
            get
            {
                return m_looping;
            }
        }

		public AudioCategory Category
		{
			get
			{
				return m_category;
			}
		}
        
        public float Volume
        {
            get
            {
                return m_volume;
            }
            set
            {
                if (!m_complete && m_volume != value)
                {
                    m_volume = value;
                    UpdateVolume();
                }
            }
        }

        public Vector3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                if(!m_complete && m_position != value)
                {
                    m_position = value;
                    UpdatePosition();
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
                if (!m_complete && m_velocity != value)
                {
                    m_velocity = value;
                    UpdateVelocity();
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
                if(!m_complete && m_minRange != value)
                {
                    m_minRange = value;
                    UpdateRange();
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
                if (!m_complete && m_maxRange != value)
                {
                    m_maxRange = value;
                    UpdateRange();
                }
            }
        }

        public bool Stopped
        {
            get
            {
                return m_complete;
            }
        }

		public OpenALSoundPlayback(OpenALSoundSource source, OpenALSound sound, bool looping, AudioCategory category)
        {
            m_source = source;
            m_sound = sound;
            m_looping = looping;
			m_category = category;

            m_volume = 1.0f;
            m_position = Vector3.Zero;
            m_velocity = Vector3.Zero;
            m_minRange = 5.0f;
            m_maxRange = 15.0f;
            m_complete = false;

            AL.Source(m_source.ALSource, ALSourcei.Buffer, (int)sound.ALBuffer);
            AL.Source(m_source.ALSource, ALSourceb.Looping, looping);
            ALUtils.CheckError();

            UpdateVolume();
            UpdatePosition();
            UpdateVelocity();
            UpdateRange();

            AL.SourcePlay(m_source.ALSource);
            ALUtils.CheckError();
        }

        public void Stop()
        {
            if (!m_complete)
            {
                AL.SourceStop(m_source.ALSource);
                AL.Source(m_source.ALSource, ALSourcei.Buffer, 0);
                ALUtils.CheckError();
                m_complete = true;
            }
        }

        public void Update(float dt)
        {
            if (!m_complete)
            {
                CheckComplete();
            }
        }

        public void UpdateVolume()
        {
            if (!m_complete)
            {
				var globalVolume = OpenALAudio.Instance.GetVolume(m_category);
                AL.Source(m_source.ALSource, ALSourcef.Gain, m_volume * globalVolume);
                ALUtils.CheckError();
            }
        }

        public void UpdatePosition()
        {
            if (!m_complete)
            {
                var pos = m_position;
                AL.Source(m_source.ALSource, ALSource3f.Position, pos.X, pos.Y, -pos.Z);
                ALUtils.CheckError();
            }
        }

        public void UpdateVelocity()
        {
            if (!m_complete)
            {
                var vel = m_velocity;
                AL.Source(m_source.ALSource, ALSource3f.Velocity, vel.X, vel.Y, -vel.Z);
                ALUtils.CheckError();
            }
        }

        public void UpdateRange()
        {
            if (!m_complete)
            {
                AL.Source(m_source.ALSource, ALSourcef.RolloffFactor, 4.0f);
                AL.Source(m_source.ALSource, ALSourcef.ReferenceDistance, m_minRange);
                AL.Source(m_source.ALSource, ALSourcef.MaxDistance, m_maxRange);
                ALUtils.CheckError();
            }
        }
      
        private void CheckComplete()
        {
            var state = AL.GetSourceState(m_source.ALSource);
            ALUtils.CheckError();
            if (state == ALSourceState.Stopped)
            {
                AL.Source(m_source.ALSource, ALSourcei.Buffer, 0);
                ALUtils.CheckError();
                m_complete = true;
            }
        }
    }
}

