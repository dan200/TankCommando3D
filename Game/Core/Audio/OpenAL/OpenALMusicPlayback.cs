using Dan200.Core.Main;
using NVorbis;
using OpenTK.Audio.OpenAL;
using System;
using System.Threading;
using Dan200.Core.Math;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALMusicPlayback : IMusicPlayback, IDisposable
    {
        private const int BUFFER_SIZE = 16384;
        private const int NUM_BUFFERS = 4;
        private const int UPDATE_INTERVAL_MILLIS = 50;

        private readonly OpenALMusic m_music;
		private readonly AudioCategory m_category;
        private readonly bool m_looping;
        private readonly uint m_source;
        private readonly uint[] m_buffers;

        private float m_fade;
        private float m_targetFade;
        private float m_fadeSpeed;
        private bool m_stopAfterFade;

        private readonly object m_lock;
        private float m_volume;
        private volatile bool m_stopped;

        public Music Music
        {
            get
            {
                return m_music;
            }
        }

		public AudioCategory Category
		{
			get
			{
				return m_category;
			}
		}

        public bool Looping
        {
            get
            {
                return m_looping;
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

        public bool Stopped
        {
            get
            {
                return m_stopped;
            }
        }

		public OpenALMusicPlayback(OpenALMusic music, bool looping, float fadeInTime, AudioCategory category)
        {
            m_music = music;
			m_category = category;
            m_looping = looping;
            m_volume = 1.0f;
            if (fadeInTime > 0.0f)
            {
                m_fade = 0.0f;
                m_targetFade = 1.0f;
                m_fadeSpeed = 1.0f / fadeInTime;
            }
            else
            {
                m_fade = 1.0f;
                m_targetFade = 1.0f;
                m_fadeSpeed = 0.0f;
            }
            m_stopAfterFade = false;
            m_lock = new object();
            m_stopped = false;

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
            ALUtils.CheckError();
            UpdateVolume();

            // Start the background thread
            var thread = new Thread(Run);
            thread.Name = "OpenALMusicPlayback";
            thread.Start();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Update(float dt)
        {
            if (!m_stopped)
            {
                if (m_fade < m_targetFade)
                {
                    // Fade in
                    m_fade = Mathf.Min(m_fade + dt * m_fadeSpeed, m_targetFade);
                    UpdateVolume();
                    if (m_fade >= m_targetFade && m_stopAfterFade)
                    {
                        Stop();
                    }
                }
                else if (m_fade > m_targetFade)
                {
                    // Fade out
                    m_fade = Mathf.Max(m_fade - dt * m_fadeSpeed, m_targetFade);
                    UpdateVolume();
                    if (m_fade <= m_targetFade && m_stopAfterFade)
                    {
                        Stop();
                    }
                }
            }
        }

        public void FadeToVolume(float target, float duration, bool thenStop = false)
        {
            if (!m_stopped)
            {
                if (duration > 0.0f)
                {
                    m_targetFade = target;
                    m_fadeSpeed = System.Math.Abs(m_targetFade - m_fade) / duration;
                    m_stopAfterFade = thenStop;
                }
                else
                {
                    m_targetFade = target;
                    m_fade = target;
                    UpdateVolume();
                    if (thenStop)
                    {
                        Stop();
                    }
                }
            }
        }

        public void Stop()
        {
            lock (m_lock)
            {
                if (!m_stopped)
                {
                    m_stopped = true;

                    AL.DeleteSource((int)m_source);
                    ALUtils.CheckError();

                    AL.DeleteBuffers(m_buffers);
                    ALUtils.CheckError();
                }
            }
        }

        private void ConvertSamples(float[] input, short[] output, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                output[i] = (short)(32768.0f * input[i]);
            }
        }

        private void Run()
        {
            bool started = false;
            try
            {
                App.LogDebug("Started streaming {0}", m_music.Path);
                using (var vorbis = new VorbisReader(m_music.OpenForStreaming(), true))
                {
                    // Get number of channels and sample rate
                    var channels = vorbis.Channels;
                    var format = (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                    var sampleRate = vorbis.SampleRate;

                    // Start reading
                    int nextBufferIndex = 0;
                    var buffer = new float[BUFFER_SIZE];
                    var shortBuffer = new short[BUFFER_SIZE];
                    while (!m_stopped)
                    {
                        // Start the file
                        if (started)
                        {
                            App.LogDebug("Reached end of {0}. Restarting", m_music.Path);
                            vorbis.DecodedPosition = 0;
                        }
                        else
                        {
                            started = true;
                        }

                        // Loop until the end of the file, or done is requested
                        bool reachedEnd = false;
                        while (!reachedEnd)
                        {
                            // Get the queue state
                            int queued, processed;
                            lock (m_lock)
                            {
                                if (m_stopped)
                                {
                                    break;
                                }

                                AL.GetSource(m_source, ALGetSourcei.BuffersQueued, out queued);
                                AL.GetSource(m_source, ALGetSourcei.BuffersProcessed, out processed);
                                ALUtils.CheckError(true);

                                // Dequeued processed buffers
                                if (processed > 0)
                                {
                                    AL.SourceUnqueueBuffers((int)m_source, processed);
                                    ALUtils.CheckError(true);
                                    queued -= processed;
                                    processed = 0;
                                }
                            }

                            // Queue some samples
                            while (queued < m_buffers.Length)
                            {
                                // Read the samples
                                int count = vorbis.ReadSamples(buffer, 0, buffer.Length);
                                if (count == 0)
                                {
                                    reachedEnd = true;
                                    break;
                                }

                                // Convert the samples to a format OpenAL likes
                                ConvertSamples(buffer, shortBuffer, count);

                                // Pick an OpenAL buffer to put the samples in
                                var alBuffer = m_buffers[nextBufferIndex];
                                nextBufferIndex = (nextBufferIndex + 1) % m_buffers.Length;

                                lock (m_lock)
                                {
                                    if (m_stopped)
                                    {
                                        break;
                                    }

                                    // Put the samples into the OpenAL buffer
                                    AL.BufferData((int)alBuffer, format, shortBuffer, count * sizeof(short), sampleRate);
                                    ALUtils.CheckError(true);

                                    // Add the buffer to the source's queue
                                    AL.SourceQueueBuffer((int)m_source, (int)alBuffer);
                                    ALUtils.CheckError(true);
                                }

                                queued++;
                            }

                            // Play the source if it's not playing already
                            lock (m_lock)
                            {
                                if (m_stopped || reachedEnd)
                                {
                                    break;
                                }

                                var state = AL.GetSourceState(m_source);
                                if (state != ALSourceState.Playing)
                                {
                                    if (state != ALSourceState.Initial)
                                    {
										App.LogDebug("Buffer overrun detected. Resuming {0}", m_music.Path);
                                    }
                                    AL.SourcePlay(m_source);
                                    ALUtils.CheckError(true);
                                }
                            }

                            // Sleep
                            Thread.Sleep(UPDATE_INTERVAL_MILLIS);
                        }

                        // Break out after one playthrough if not looping
                        if (!m_looping)
                        {
                            break;
                        }
                    }
                }
                App.LogDebug("Stopped streaming {0}", m_music.Path);
            }
            catch (Exception e)
            {
				App.LogError("Error streaming {0}: Threw {1}: {2}", m_music.Path, e.GetType().FullName, e.Message);
                App.LogError(e.StackTrace);
            }
            finally
            {
                if (started && !m_stopped)
                {
                    // Wait for complete
                    while (true)
                    {
                        lock (m_lock)
                        {
                            if (m_stopped)
                            {
                                break;
                            }

                            var state = AL.GetSourceState(m_source);
                            if (state != ALSourceState.Playing)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(10);
                    }

                    // Stop
                    Stop();
                }
            }
        }

        public void UpdateVolume()
        {
            lock (m_lock)
            {
                if (!m_stopped)
                {
					var globalVolume = OpenALAudio.Instance.GetVolume(m_category);
                    var localVolume = m_fade * m_volume;
                    AL.Source(m_source, ALSourcef.Gain, globalVolume * localVolume);
                    ALUtils.CheckError();
                }
            }
        }
    }
}

