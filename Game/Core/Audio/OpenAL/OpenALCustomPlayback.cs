using Dan200.Core.Main;
using Dan200.Core.Math;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALCustomPlayback : ICustomPlayback, IDisposable
    {
        private const int BUFFER_DURATION_MILLIS = 20;
        private const int NUM_BUFFERS = 3;
        private const int UPDATE_INTERVAL_MILLIS = 5;

        private readonly ICustomAudioSource m_audioSource;
        private readonly int m_channels;
        private readonly int m_sampleRate;
        private readonly uint m_source;
        private readonly uint[] m_buffers;
		private readonly AudioCategory m_category;

        private float m_volume;
        private Vector3 m_position;
        private Vector3 m_velocity;
        private float m_minRange;
        private float m_maxRange;
        private volatile bool m_stopped;
        private volatile bool m_finished;

        public ICustomAudioSource Source
        {
            get
            {
                return m_audioSource;
            }
        }

		public AudioCategory Category
		{
			get
			{
				return m_category;
			}
		}

        public int Channels
        {
            get
            {
                return m_channels;
            }
        }

        public int SampleRate
        {
            get
            {
                return m_sampleRate;
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
                if (!m_stopped && m_volume != value)
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
                if(!m_stopped && m_position != value)
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
                if (!m_stopped && m_velocity != value)
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
                if (!m_stopped && m_minRange != value)
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
                if (!m_stopped && m_maxRange != value)
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
                return m_stopped;
            }
        }

		public OpenALCustomPlayback(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category)
        {
            m_audioSource = source;
            m_channels = channels;
            m_sampleRate = sampleRate;
			m_category = category;
            m_volume = 1.0f;
            m_position = Vector3.Zero;
            m_velocity = Vector3.Zero;
            m_minRange = 5.0f;
            m_maxRange = 15.0f;
            m_stopped = false;
            m_finished = false;

            // Create the buffers
            m_buffers = new uint[NUM_BUFFERS];
            for (int i = 0; i < m_buffers.Length; ++i)
            {
                AL.GenBuffer(out m_buffers[i]);
                ALUtils.CheckError();
                if (OpenALAudio.Instance.XRam.IsInitialized)
                {
                    OpenALAudio.Instance.XRam.SetBufferMode(1, ref m_buffers[i], XRamExtension.XRamStorage.Hardware);
                }
            }

            // Create the source
            AL.GenSource(out m_source);
            UpdateVolume();
            UpdatePosition();
            UpdateVelocity();
            UpdateRange();
            ALUtils.CheckError();

            // Start the background thread
            var thread = new Thread(Run);
            thread.Name = "OpenALCustomback";
            thread.Start();
        }

        public void Dispose()
        {
            // Request stop
            Stop();

            // Busy loop until the thread is terminated
            do {} while (!m_finished);

            // Delete the sources
            AL.DeleteSource((int)m_source);
            ALUtils.CheckError();

            AL.DeleteBuffers(m_buffers);
            ALUtils.CheckError();
        }

        public void Update(float dt)
        {
        }

        public void Stop()
        {
            m_stopped = true;
        }

        private void Run()
        {
            try
            {
                App.LogDebug("Started streaming custom audio @ {0}Hz", m_sampleRate);

                // Get number of channels and sample rate
                var channels = m_channels;
                var format = (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                var sampleRate = m_sampleRate;

                // Start reading
                int nextBufferIndex = 0;
                int bufferLength = System.Math.Max((BUFFER_DURATION_MILLIS * sampleRate) / 1000, 1);
                var buffer = new AudioBuffer(bufferLength, channels, sampleRate);

                // Loop until the we stop
                while (true)
                {
                    if (m_stopped)
                    {
                        break;
                    }

                    // Get the queue state
                    int queued, processed;
                    AL.GetSource(m_source, ALGetSourcei.BuffersQueued, out queued);
                    AL.GetSource(m_source, ALGetSourcei.BuffersProcessed, out processed);
                    ALUtils.CheckError(true);

                    // Dequeued processed buffers
                    if (processed > 0)
                    {
                        unsafe
                        {
                            uint* bids = stackalloc uint[processed];
                            AL.SourceUnqueueBuffers(m_source, processed, bids);
                        }
                        ALUtils.CheckError(true);
                        queued -= processed;
                        processed = 0;
                    }

                    // Queue some samples
                    while (queued < m_buffers.Length)
                    {
                        // Generate the samples
                        m_audioSource.GenerateSamples(buffer);

                        if (m_stopped)
                        {
                            break;
                        }

                        // Pick an OpenAL buffer to put the samples in
                        var alBuffer = m_buffers[nextBufferIndex];
                        nextBufferIndex = (nextBufferIndex + 1) % m_buffers.Length;

                        // Put the samples into the OpenAL buffer
                        unsafe
                        {
                            fixed(void* pData = buffer.Samples)
                            {
                                AL.BufferData(alBuffer, format, new IntPtr(pData), buffer.Length * buffer.Channels * sizeof(short), sampleRate);
                            }
                        }
                        ALUtils.CheckError(true);

                        // Add the buffer to the source's queue
                        AL.SourceQueueBuffer((int)m_source, (int)alBuffer);
                        ALUtils.CheckError(true);

                        queued++;
                    }

                    // Play the source if it's not playing already
                    if (m_stopped)
                    {
                        break;
                    }

                    var state = AL.GetSourceState(m_source);
                    if (state != ALSourceState.Playing)
                    {
                        if (state != ALSourceState.Initial)
                        {
							App.LogDebug("Buffer overrun detected. Resuming custom audio");
                        }
                        AL.SourcePlay(m_source);
                        ALUtils.CheckError(true);
                    }

                    // Sleep
                    Thread.Sleep(UPDATE_INTERVAL_MILLIS);
                }
                App.LogDebug("Stopped streaming custom audio");
            }
            catch (Exception e)
            {
				App.LogError("Error streaming custom audio: Threw {0}: {1}", e.GetType().FullName, e.Message);
                App.LogError(e.StackTrace);
            }
            finally
            {
                if (m_stopped)
                {
                    // Stop the audio immediately
                    try
                    {
                        AL.SourceStop(m_source);
                        ALUtils.CheckError(true);
                    }
                    catch (Exception e)
                    {
                        App.LogError("Error stopping custom audio: Threw {0}: {1}", e.GetType().FullName, e.Message);
                        App.LogError(e.StackTrace);
                    }
                }

                // Allow the audio to be disposed
                m_finished = true;
            }
        }

        public void UpdateVolume()
        {
            if (!m_stopped)
            {
				var globalVolume = OpenALAudio.Instance.GetVolume(m_category);
                var localVolume = m_volume;
                AL.Source(m_source, ALSourcef.Gain, globalVolume * localVolume);
                ALUtils.CheckError();
            }
        }

        public void UpdatePosition()
        {
            if (!m_stopped)
            {
                var pos = m_position;
                AL.Source(m_source, ALSource3f.Position, pos.X, pos.Y, -pos.Z);
                ALUtils.CheckError();
            }
        }

        public void UpdateVelocity()
        {
            if (!m_stopped)
            {
                var vel = m_velocity;
                AL.Source(m_source, ALSource3f.Velocity, vel.X, vel.Y, -vel.Z);
                ALUtils.CheckError();
            }
        }

        public void UpdateRange()
        {
            if (!m_stopped)
            {
                AL.Source(m_source, ALSourcef.RolloffFactor, 1.0f);
                AL.Source(m_source, ALSourcef.ReferenceDistance, m_minRange);
                AL.Source(m_source, ALSourcef.MaxDistance, m_maxRange);
                ALUtils.CheckError();
            }
        }
    }
}

