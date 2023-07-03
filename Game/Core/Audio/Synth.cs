using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Audio
{
    internal class Synth : ICustomAudioSource
    {
        private SynthChannel[] m_channels;

        public int Channels
        {
            get
            {
                return m_channels.Length;
            }
        }

        public Synth(int channels)
        {
            m_channels = new SynthChannel[channels];
            for (int i = 0; i < m_channels.Length; ++i)
            {
                m_channels[i] = new SynthChannel();
            }
        }
        
        public ref SynthSettings AccesssChannelSettings(int channel)
        {
            App.Assert(channel >= 0 && channel < m_channels.Length);
            var ch = m_channels[channel];
            return ref ch.Settings;
        }

        public bool QueueSettingsChange(int channel, float delay, in SynthSettings settings, SynthSettingsOptions options)
        {
            App.Assert(channel >= 0 && channel < m_channels.Length);
            var ch = m_channels[channel];
            return ch.QueueSettingsChange(delay, settings, options);
        }

        private AudioBuffer m_tempBuffer;

        public void GenerateSamples(in AudioBuffer buffer)
        {
            // Fill in the first channel
            int samplesWritten = m_channels[0].GenerateSamples(buffer);
            if (samplesWritten < buffer.Length)
            {
                var remainder = buffer.Slice(samplesWritten);
                for (int i = 0; i < remainder.Channels; ++i)
                {
                    Mixers.GenerateSilence(remainder, i);
                }
            }

            if (m_channels.Length > 0)
            {
                // Create a temporary buffer
                if (m_tempBuffer.Samples == null || m_tempBuffer.Length < buffer.Length || m_tempBuffer.Channels < buffer.Channels)
                {
                    m_tempBuffer = new AudioBuffer(buffer.Length, buffer.Channels, buffer.SampleRate);
                }

                // Fill in remaining channels
                for (int i = 1; i < m_channels.Length; ++i)
                {
                    var ch = m_channels[i];
                    samplesWritten = ch.GenerateSamples(m_tempBuffer);
                    if (samplesWritten > 0)
                    {
                        var dst = buffer.Slice(0, samplesWritten);
                        var src = m_tempBuffer.Slice(0, samplesWritten);
                        for (int j = 0; j < src.Channels; ++j)
                        {
                            Mixers.Combine(dst, j, src, j);
                        }
                    }
                }
            }
        }
    }
}
