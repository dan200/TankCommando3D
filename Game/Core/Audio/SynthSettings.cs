using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Audio
{
    internal enum Waveform
    {
        Silence = 0,
        Square,
        Triangle,
        Sawtooth,
        Noise
    }

    [Flags]
    internal enum SynthSettingsOptions
    {
        Waveform = 1,
        Volume = 2,
        Frequency = 4,
        Duty = 8,
        VibratoFrequency = 16,
        VibratoDepth = 32,
        VolumeSlide = 64,
        FrequencySlide = 128,
        DutySlide = 256,
        VibratoFrequencySlide = 512,
        VibratoDepthSlide = 1024,
    }

    internal struct SynthSettings
    {
        public readonly static SynthSettings Silence = new SynthSettings();

        public Waveform Waveform;
        public float Volume;
        public float Frequency;
        public float Duty;
        public float VibratoFrequency;
        public float VibratoDepth;
        public float VolumeSlide;
        public float FrequencySlide;
        public float DutySlide;
        public float VibratoFrequencySlide;
        public float VibratoDepthSlide;
    }
}
