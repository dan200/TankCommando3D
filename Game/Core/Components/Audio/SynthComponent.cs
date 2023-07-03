using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Level;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Components.Audio
{
    internal struct SynthComponentData
    {
        [Range(Min = 1)]
        public int Channels;
    }

    [RequireComponent(typeof(AudioEmitterComponent))]
    internal class SynthComponent : Component<SynthComponentData>
    {
        private AudioEmitterComponent m_emitter;
        private Synth m_synth;
        private ICustomPlayback m_playback;

        private class SoundTweakWindow : DebugWindow
        {
            private Synth m_synth;
            private int m_channel;

            public SoundTweakWindow(Synth synth)
            {
                m_synth = synth;
                m_channel = 0;
            }

            protected override void OnGUI(ref DebugGUIBuilder builder)
            {
                builder.IntSlider("Channel", ref m_channel, 0, m_synth.Channels - 1);

                ref var settings = ref m_synth.AccesssChannelSettings(m_channel);
                builder.DropDown("Waveform", ref settings.Waveform, EnumConverter.GetValues<Waveform>());

                if(builder.FloatSlider("Frequency", ref settings.Frequency, 0.0f, 1000.0f))
                {
                    settings.FrequencySlide = 0.0f;
                }
                builder.FloatSlider("Frequency Slide", ref settings.FrequencySlide, -1000.0f, 1000.0f);

                if(builder.FloatSlider("Volume", ref settings.Volume, 0.0f, 1.0f))
                {
                    settings.VolumeSlide = 0.0f;
                }
                builder.FloatSlider("Volume Slide", ref settings.VolumeSlide, -1.0f, 1.0f);

                if(builder.FloatSlider("Duty", ref settings.Duty, 0.0f, 1.0f))
                {
                    settings.DutySlide = 0.0f;
                }
                builder.FloatSlider("Duty Slide", ref settings.DutySlide, -1.0f, 1.0f);

                if(builder.FloatSlider("Vibrato Depth", ref settings.VibratoDepth, 0.0f, 1000.0f))
                {
                    settings.VibratoDepthSlide = 0.0f;
                }
                builder.FloatSlider("Vibrato Depth Slide", ref settings.VibratoDepthSlide, -1000.0f, 1000.0f);

                if (builder.FloatSlider("Vibrato Frequency", ref settings.VibratoFrequency, 0.0f, 100.0f))
                {
                    settings.VibratoFrequencySlide = 0.0f;
                }
                builder.FloatSlider("Vibrato Frequency Slide", ref settings.VibratoFrequencySlide, -100.0f, 100.0f);
            }
        }
        //private SoundTweakWindow m_window;

        protected override void OnInit(in SynthComponentData properties)
        {
            m_emitter = Entity.GetComponent<AudioEmitterComponent>();
            m_synth = new Synth(properties.Channels);
            m_playback = m_emitter.PlayCustom(m_synth, 1, 8192, AudioCategory.Sound);

            /*
            // Test code for tanks: TODO: REMOVE ME
            ref var settings = ref m_synth.AccesssChannelSettings(0);
            settings.Waveform = Waveform.Triangle;
            settings.Volume = 0.0f;
            settings.VolumeSlide = 0.2f;
            settings.Frequency = GlobalRandom.Float(65.0f, 75.0f);
            settings.Duty = 0.9f;
            settings.VibratoDepth = GlobalRandom.Float(18.0f, 22.0f);
            settings.VibratoFrequency = 10.0f;

            var settings = new SynthSettings();
            settings.Waveform = Waveform.Triangle;
            settings.Frequency = GlobalRandom.Float(0.99f, 1.01f) * 2000.0f;
            settings.FrequencySlide = -3000.0f;
            settings.Volume = 0.0f;
            settings.VolumeSlide = 10.0f;
            m_synth.QueueSettingsChange(0, 0.0f, settings, SynthSettingsOptions.Waveform | SynthSettingsOptions.Frequency | SynthSettingsOptions.FrequencySlide | SynthSettingsOptions.VolumeSlide);

            settings.VolumeSlide = -3.0f;
            m_synth.QueueSettingsChange(0, 0.1f, settings, SynthSettingsOptions.VolumeSlide);

            m_window = new SoundTweakWindow(m_synth);
            Level.GetSystem<GUISystem>().Screen.Elements.Add(m_window);
            */
        }

        protected override void OnShutdown()
        {
            //Level.GetSystem<GUISystem>().Screen.Elements.Remove(m_window);
            //m_window.Dispose();
            m_playback.Stop();
        }
    }
}
