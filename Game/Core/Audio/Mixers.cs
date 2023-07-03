using System;
using Dan200.Core.Main;

namespace Dan200.Core.Audio
{
    internal static class Mixers
    {
        private static short Clamp(int value)
        {
            return (short)(System.Math.Min(System.Math.Max(value, short.MinValue), short.MaxValue));
        }

        public static void GenerateSilence(in AudioBuffer buffer, int channel)
        {
            App.Assert(channel < buffer.Channels);

            var samples = buffer.Samples;
            for (int i = (buffer.Start * buffer.Channels) + channel; i < (buffer.Start + buffer.Length) * buffer.Channels; i += buffer.Channels)
            {
                samples[i] = 0;
            }
        }

        public static void GenerateSquareWave(in AudioBuffer buffer, int channel, int timeOffset, int amplitude, int hiDuration, int loDuration)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(timeOffset >= 0);
            App.Assert(amplitude >= 0);
            App.Assert(hiDuration > 0);
            App.Assert(loDuration > 0);

            var samples = buffer.Samples;
            short highValue = Clamp(amplitude);
            short lowValue = Clamp(-amplitude);
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                int posInWave = timeOffset % (hiDuration + loDuration);
                if (posInWave < hiDuration)
                {
                    samples[pos] = highValue;
                }
                else
                {
                    samples[pos] = lowValue;
                }
                timeOffset++;
            }
        }

        public static void GenerateTriangleWave(in AudioBuffer buffer, int channel, int timeOffset, int amplitude, int upDuration, int downDuration)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(timeOffset >= 0);
            App.Assert(amplitude >= 0);
            App.Assert(upDuration > 0);
            App.Assert(downDuration > 0);

            var samples = buffer.Samples;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                int posInWave = timeOffset % (upDuration + downDuration);
                if (posInWave < upDuration)
                {
                    samples[pos] = Clamp(-amplitude + ((2 * amplitude * posInWave) / upDuration));
                }
                else
                {
                    samples[pos] = Clamp(amplitude - ((2 * amplitude * (posInWave - upDuration)) / downDuration));
                }
                timeOffset++;
            }
        }

        public static void GenerateSawtoothWave(in AudioBuffer buffer, int channel, int timeOffset, int amplitude, int period)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(timeOffset >= 0);
            App.Assert(amplitude >= 0);
            App.Assert(period > 0);

            var samples = buffer.Samples;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                int posInWave = timeOffset % period;
                samples[pos] = Clamp(-amplitude + ((2 * amplitude * posInWave) / period));
                timeOffset++;
            }
        }

        public static void GenerateSineWave(in AudioBuffer buffer, int channel, int timeOffset, int amplitude, int period)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(timeOffset >= 0);
            App.Assert(amplitude >= 0);
            App.Assert(period > 0);

            var samples = buffer.Samples;
            var amplitudeD = (double)amplitude;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                double posInWave = (double)(timeOffset % period) / (double)period;
                samples[pos] = Clamp((int)(amplitudeD * System.Math.Sin(posInWave * 2.0 * System.Math.PI)));
                timeOffset++;
            }
        }

        public static void Amplify(in AudioBuffer buffer, int channel, int amplitude)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(amplitude >= 0);

            var samples = buffer.Samples;
            var amplitudeL = (long)amplitude;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                samples[pos] = Clamp((samples[pos] * amplitude) / short.MaxValue);
            }
        }

        public static void Copy(in AudioBuffer buffer, int channel, in AudioBuffer src, int srcChannel)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(srcChannel < src.Channels);
            App.Assert(!buffer.Equals(src) || channel != srcChannel);
            App.Assert(buffer.Length == src.Length);

            var samples = buffer.Samples;
            var srcSamples = src.Samples;
            int srcPos = (src.Start * src.Channels) + srcChannel;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                samples[pos] = srcSamples[srcPos];
                srcPos += src.Channels;
            }
        }

        public static void Combine(in AudioBuffer buffer, int channel, in AudioBuffer src, int srcChannel)
        {
            App.Assert(channel < buffer.Channels);
            App.Assert(srcChannel < src.Channels);
            App.Assert(!buffer.Equals(src) || channel != srcChannel);
            App.Assert(buffer.Length == src.Length);

            var samples = buffer.Samples;
            var srcSamples = src.Samples;
            int srcPos = (src.Start * src.Channels) + srcChannel;
            for (int pos = (buffer.Start * buffer.Channels) + channel; pos < (buffer.Start + buffer.Length) * buffer.Channels; pos += buffer.Channels)
            {
                samples[pos] = Clamp(samples[pos] + srcSamples[srcPos]);
                srcPos += src.Channels;
            }
        }
    }
}
