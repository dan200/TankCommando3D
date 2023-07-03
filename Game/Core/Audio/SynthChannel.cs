using Dan200.Core.Math;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dan200.Core.Audio
{
    internal unsafe class SynthChannel
    {
        private const float MASTER_VOLUME = 0.6f;
        private const int NOISE_BUFFER_SIZE = 4;
        private const float NOISE_BUFFER_SIZE_FLOAT = 4.0f;
        private const int SOUND_QUEUE_SIZE = 32;

        private SynthSettings m_settings;        
        private short[] m_noiseBuffer;
        private float m_phase;
        private float m_vibPhase;

        private struct QueuedSettings
        {
            public float Time;
            public SynthSettings Settings;
            public SynthSettingsOptions Options;
        }
        private LockFreeQueue<QueuedSettings> m_queuedSettings;
        private float m_time;

        public ref SynthSettings Settings
        {
            get
            {
                return ref m_settings;
            }
        }
        
        public SynthChannel()
        {
            m_settings = SynthSettings.Silence;
            m_settings.Duty = 0.5f;

            m_noiseBuffer = new short[NOISE_BUFFER_SIZE];
            FillNoiseBuffer(m_noiseBuffer);
            m_phase = 0.0f;
            m_vibPhase = 0.0f;

            m_queuedSettings = new LockFreeQueue<QueuedSettings>(8);
            m_time = 0.0f;
        }

        public bool QueueSettingsChange(float delay, SynthSettings settings, SynthSettingsOptions options)
        {
            var timeNow = m_time;
            var queuedSettings = new QueuedSettings();
            queuedSettings.Time = timeNow + delay;
            queuedSettings.Settings = settings;
            queuedSettings.Options = options;
            return m_queuedSettings.Enqueue(queuedSettings);
        }

        public int GenerateSamples(in AudioBuffer buffer)
        {
            int samplesWritten = Synth(buffer, 0);
            if (samplesWritten > 0)
            {
                var subBuffer = buffer.Slice(0, samplesWritten);
                for (int i = 1; i < buffer.Channels; ++i)
                {
                    Mixers.Copy(subBuffer, i, subBuffer, 0);
                }
            }
            return samplesWritten;
        }

        private int Synth(in AudioBuffer buffer, int channel)
        {
            int start = 0;
            while(start < buffer.Length)
            {
                start += SynthOne(buffer.Slice(start), channel);
            }
            return buffer.Length;
        }

        private void ApplySettings(in SynthSettings settings, SynthSettingsOptions options)
        {
            if((options & SynthSettingsOptions.Waveform) != 0)
            {
                m_settings.Waveform = settings.Waveform;
            }
            if ((options & SynthSettingsOptions.Volume) != 0)
            {
                m_settings.Volume = settings.Volume;
            }
            if ((options & SynthSettingsOptions.Frequency) != 0)
            {
                m_settings.Frequency = settings.Frequency;
            }
            if ((options & SynthSettingsOptions.Duty) != 0)
            {
                m_settings.Duty = settings.Duty;
            }
            if ((options & SynthSettingsOptions.VibratoFrequency) != 0)
            {
                m_settings.VibratoFrequency = settings.VibratoFrequency;
            }
            if ((options & SynthSettingsOptions.VibratoDepth) != 0)
            {
                m_settings.VibratoDepth = settings.VibratoDepth;
            }
            if ((options & SynthSettingsOptions.VolumeSlide) != 0)
            {
                m_settings.VolumeSlide = settings.VolumeSlide;
            }
            if ((options & SynthSettingsOptions.FrequencySlide) != 0)
            {
                m_settings.FrequencySlide = settings.FrequencySlide;
            }
            if ((options & SynthSettingsOptions.DutySlide) != 0)
            {
                m_settings.DutySlide = settings.DutySlide;
            }
            if ((options & SynthSettingsOptions.VibratoFrequencySlide) != 0)
            {
                m_settings.VibratoFrequencySlide = settings.VibratoFrequencySlide;
            }
            if ((options & SynthSettingsOptions.VibratoDepthSlide) != 0)
            {
                m_settings.VibratoDepthSlide = settings.VibratoDepthSlide;
            }
        }

        private int SynthOne(in AudioBuffer buffer, int channel)
        {
            float sampleRatef = (float)buffer.SampleRate;
            float startTime = m_time;
            int outputSamples = buffer.Length;
            float duration = (float)outputSamples / sampleRatef;

            // See how long we have until the settings change
            QueuedSettings nextSettingsChange;
            bool applyNextSettings = false;
            if(m_queuedSettings.Peek(out nextSettingsChange))
            {
                if (nextSettingsChange.Time <= startTime + duration)
                {
                    m_queuedSettings.Dequeue();
                    applyNextSettings = true;

                    duration = Mathf.Max(nextSettingsChange.Time - startTime, 0.0f);
                    outputSamples = System.Math.Max((int)(duration * sampleRatef), 0);
                    outputSamples = System.Math.Min(outputSamples, buffer.Length);
                }
            }

            // Get and update all the settings at once
            var waveform = m_settings.Waveform;

            float initialVolume = m_settings.Volume;
            float volumeIncrement = m_settings.VolumeSlide / sampleRatef;
            float finalVolume = Mathf.Saturate(initialVolume + (float)buffer.Length * volumeIncrement);
            m_settings.Volume = finalVolume;

            float initialFrequency = m_settings.Frequency;
            float frequencyIncrement = m_settings.FrequencySlide / sampleRatef;
            float finalFrequency = Mathf.Max(initialFrequency + (float)buffer.Length * frequencyIncrement, 0.0f);
            m_settings.Frequency = finalFrequency;

            float initialDuty = m_settings.Duty;
            float dutyIncrement = m_settings.DutySlide / sampleRatef;
            float finalDuty = Mathf.Saturate(initialDuty + (float)buffer.Length * dutyIncrement);
            m_settings.Duty = finalDuty;

            float initialVibFrequency = m_settings.VibratoFrequency;
            float vibFrequencyIncrement = m_settings.VibratoFrequencySlide / sampleRatef;
            float finalVibFreqnecy = Mathf.Max(initialVibFrequency + (float)buffer.Length * vibFrequencyIncrement, 0.0f);
            m_settings.VibratoFrequency = finalVibFreqnecy;

            float initialVibDepth = m_settings.VibratoDepth;
            float vibDepthIncrement = m_settings.VibratoDepthSlide / sampleRatef;
            float finalVibDepth = Mathf.Max(initialVibDepth + (float)buffer.Length * vibDepthIncrement, 0.0f);
            m_settings.VibratoDepth = finalVibDepth;

            float initialTime = m_time;
            float finalTime = initialTime + duration;
            m_time = finalTime;

            // Apply the new settings
            if(applyNextSettings)
            {
                ApplySettings(nextSettingsChange.Settings, nextSettingsChange.Options);
            }

            /*
            // Early out if the output is silent
            if (waveform == Waveform.Silence || (initialVolume == 0.0f && finalVolume == 0.0f) || outputSamples == 0)
            {
                return;
            }
            */

            // Fill the buffer with the base waveform
            float frequency = initialFrequency;
            float duty = initialDuty;
            float vibFrequency = initialVibFrequency;
            float vibDepth = initialVibDepth;
            float phase = m_phase;
            float vibPhase = m_vibPhase;
            var samples = buffer.Samples;
            int start = buffer.Start;
            int channels = buffer.Channels;
            fixed (short* pSamples = samples)
            {
                short* pStart = pSamples + start + channel;
                short* pEnd = pStart + outputSamples * channels;
                int step = channels;
                switch (waveform)
                {
                    case Waveform.Silence:
                    default:
                        {
                            for (short* pPos = pStart; pPos < pEnd; pPos += step)
                            {
                                *pPos = 0;
                            }
                            break;
                        }
                    case Waveform.Square:
                        {
                            for (short* pPos = pStart; pPos < pEnd; pPos += step)
                            {
                                *pPos = SampleSquare(phase, duty);
                                float vibbedFrequency = ClampFrequency(frequency + vibDepth * Mathf.Sin(2.0f * Mathf.PI * vibPhase));
                                phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                                vibPhase = (vibPhase + (vibFrequency / sampleRatef)) % 1.0f;
                                frequency = Mathf.Max(frequency + frequencyIncrement, 0.0f);
                                vibFrequency = Mathf.Max(vibFrequency + vibFrequencyIncrement, 0.0f);
                                vibDepth = Mathf.Max(vibDepth + vibDepthIncrement, 0.0f);
                                duty = Mathf.Saturate(duty + dutyIncrement);
                            }
                            break;
                        }
                    case Waveform.Triangle:
                        {
                            for (short* pPos = pStart; pPos < pEnd; pPos += step)
                            {
                                *pPos = SampleTriangle(phase, duty);
                                float vibbedFrequency = ClampFrequency(frequency + vibDepth * Mathf.Sin(2.0f * Mathf.PI * vibPhase));
                                phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                                vibPhase = (vibPhase + (vibFrequency / sampleRatef)) % 1.0f;
                                frequency = Mathf.Max(frequency + frequencyIncrement, 0.0f);
                                vibFrequency = Mathf.Max(vibFrequency + vibFrequencyIncrement, 0.0f);
                                vibDepth = Mathf.Max(vibDepth + vibDepthIncrement, 0.0f);
                                duty = Mathf.Saturate(duty + dutyIncrement);
                            }
                            break;
                        }
                    case Waveform.Sawtooth:
                        {
                            for (short* pPos = pStart; pPos < pEnd; pPos += step)
                            {
                                *pPos = SampleSawtooth(phase);
                                float vibbedFrequency = ClampFrequency(frequency + vibDepth * Mathf.Sin(2.0f * Mathf.PI * vibPhase));
                                phase = (phase + (vibbedFrequency / sampleRatef)) % 1.0f;
                                vibPhase = (vibPhase + (vibFrequency / sampleRatef)) % 1.0f;
                                frequency = Mathf.Max(frequency + frequencyIncrement, 0.0f);
                                vibFrequency = Mathf.Max(vibFrequency + vibFrequencyIncrement, 0.0f);
                                vibDepth = Mathf.Max(vibDepth + vibDepthIncrement, 0.0f);
                                duty = Mathf.Saturate(duty + dutyIncrement);
                            }
                            break;
                        }
                    case Waveform.Noise:
                        {
                            var noiseBuffer = m_noiseBuffer;
                            for (short* pPos = pStart; pPos < pEnd; pPos += step)
                            {
                                *pPos = SampleNoise(phase, noiseBuffer);
                                float vibbedFrequency = ClampFrequency(frequency + vibDepth * Mathf.Sin(2.0f * Mathf.PI * vibPhase));
                                phase = (phase + (vibbedFrequency / sampleRatef));
                                if (phase >= 1.0f)
                                {
                                    FillNoiseBuffer(noiseBuffer);
                                    phase %= 1.0f;
                                }
                                vibPhase = (vibPhase + (vibFrequency / sampleRatef)) % 1.0f;
                                frequency = Mathf.Max(frequency + frequencyIncrement, 0.0f);
                                vibFrequency = Mathf.Max(vibFrequency + vibFrequencyIncrement, 0.0f);
                                vibDepth = Mathf.Max(vibDepth + vibDepthIncrement, 0.0f);
                                duty = Mathf.Saturate(duty + dutyIncrement);
                            }
                            break;
                        }
                }
            }

            // Shape the buffer to the envelope
            float amplitude = initialVolume * MASTER_VOLUME;
            float amplitudeIncrement = volumeIncrement * MASTER_VOLUME;
            for (int i = 0; i < outputSamples; ++i)
            {
                var idx = (start + i) * channels + channel;
                samples[idx] = (short)((float)samples[idx] * amplitude);
                amplitude = Mathf.Saturate( amplitude + amplitudeIncrement );
            }

            // Store state
            m_phase = phase;
            m_vibPhase = vibPhase;
            
            return outputSamples;
        }

        private static float ClampFrequency(float f)
        {
            return Mathf.Clamp(f, 0.0f, 10000.0f);
        }

        private static short Float2Short(float f)
        {
            return (short)(f * 32767.0f);
        }

        private static short SampleSquare(float t, float duty)
        {
            return t < duty ? short.MaxValue : short.MinValue;
        }

        private static short SampleTriangle(float t, float duty)
        {
            return Float2Short(
                (t < duty) ?
                (-1.0f + 2.0f * (t / duty)) :
                (1.0f - 2.0f * ((t - duty) / (1.0f - duty)))
            );
        }

        private static short SampleSawtooth(float t)
        {
            return Float2Short(-1.0f + 2.0f * t);
        }

        private static short SampleSin(float t)
        {
            return Float2Short(Mathf.Sin(2.0f * Mathf.PI * t));
        }

        private static void FillNoiseBuffer(short[] noiseBuffer)
        {
            for (int i = 0; i < NOISE_BUFFER_SIZE; ++i)
            {
                var r = (short)GlobalRandom.Int(short.MinValue, short.MaxValue);
                noiseBuffer[i] = r;
            }
        }

        private static short SampleNoise(float t, short[] noiseBuffer)
        {
            var r = (short)noiseBuffer[(int)(t * NOISE_BUFFER_SIZE_FLOAT)];
            return r;
        }
    }
}
