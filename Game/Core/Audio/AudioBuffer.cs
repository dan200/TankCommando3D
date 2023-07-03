using System;
using Dan200.Core.Main;

namespace Dan200.Core.Audio
{
    internal struct AudioBuffer
    {
        public readonly short[] Samples;
        public readonly int Start;
        public readonly int Length;
        public readonly int Channels;
        public readonly int SampleRate;

        public AudioBuffer(int length, int channels, int sampleRate)
        {
            App.Assert(length >= 0);
            App.Assert(channels == 1 || channels == 2);
            App.Assert(sampleRate > 0);
            Samples = new short[length * channels];
            Start = 0;
            Length = length;
            Channels = channels;
            SampleRate = sampleRate;
        }

        private AudioBuffer(in AudioBuffer parent, int start, int length)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= parent.Length);
            Samples = parent.Samples;
            Start = parent.Start + start;
            Length = length;
            Channels = parent.Channels;
            SampleRate = parent.SampleRate;
        }

        public AudioBuffer Slice(int start, int length)
        {
            return new AudioBuffer(this, start, length);
        }

        public AudioBuffer Slice(int start)
        {
            return new AudioBuffer(this, start, Length - start);
        }
    }
}
