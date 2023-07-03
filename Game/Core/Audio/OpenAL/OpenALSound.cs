using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;
using System.IO;
using System.Text;

namespace Dan200.Core.Audio.OpenAL
{
    internal class OpenALSound : Sound
    {
        private string m_path;
        private uint m_buffer;
        private float m_duration;

        public override string Path
        {
            get
            {
                return m_path;
            }
        }

        public override float Duration
        {
            get
            {
                return m_duration;
            }
        }

        public uint ALBuffer
        {
            get
            {
                return m_buffer;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            return LoadWave(stream, path);
        }

        public OpenALSound(string path, object data)
        {
            m_path = path;
            Load(data);
        }

        public override void Dispose()
        {
            Unload();
        }

        public override void Reload(object data)
        {
            Unload();
            Load(data);
        }

        private void Load(object data)
        {
            var wave = (WaveData)data;

            // Create the buffer
            AL.GenBuffer(out m_buffer);
            if (OpenALAudio.Instance.XRam.IsInitialized)
            {
                OpenALAudio.Instance.XRam.SetBufferMode(1, ref m_buffer, XRamExtension.XRamStorage.Hardware);
            }
            ALUtils.CheckError();

            // Measure the sound
            int samples = (wave.SoundData.Length * 8) / (wave.BitsPerSample * wave.NumChannels);
            m_duration = (float)samples / (float)wave.SampleRate;

            // Load the sound
            var soundFormat = GetSoundFormat(wave.NumChannels, wave.BitsPerSample);
            AL.BufferData((int)m_buffer, soundFormat, wave.SoundData, wave.SoundData.Length, wave.SampleRate);
            ALUtils.CheckError();
        }

        private void Unload()
        {
            OpenALAudio.Instance.StopSound(this);
            AL.DeleteBuffer(ref m_buffer);
            ALUtils.CheckError();
        }

        private class WaveData
        {
            public int NumChannels;
            public int BitsPerSample;
            public int SampleRate;
            public byte[] SoundData;
        }

        private static WaveData LoadWave(Stream stream, string path)
        {
            var reader = new BinaryReader(stream, Encoding.ASCII);

            // RIFF header
            string signature = new string(reader.ReadChars(4));
            if (signature != "RIFF")
            {
                throw new NotSupportedException("Specified stream is not a wave file.");
            }

            reader.ReadInt32(); // riff_chunk_size

            string format = new string(reader.ReadChars(4));
            if (format != "WAVE")
            {
                throw new NotSupportedException("Specified stream is not a wave file.");
            }

            // WAVE header
            string format_signature = new string(reader.ReadChars(4));
            if (format_signature != "fmt ")
            {
                throw new NotSupportedException("Specified wave file is not supported.");
            }

            reader.ReadInt32(); // format_chunk_size
            int audio_format = reader.ReadInt16();
            int num_channels = reader.ReadInt16();
            int sample_rate = reader.ReadInt32();
            reader.ReadInt32(); // byte_rate
            reader.ReadInt16(); // block_align
            int bits_per_sample = reader.ReadInt16();
            if ((audio_format != 1) ||
                (num_channels != 1 && num_channels != 2) ||
                (bits_per_sample != 8 && bits_per_sample != 16))
            {
                throw new NotSupportedException("Specified wave file is not supported.");
            }

            // Sound data
            string data_signature = new string(reader.ReadChars(4));
            if (data_signature != "data")
            {
                throw new NotSupportedException("Specified wave file is not supported.");
            }

            int data_chunk_size = reader.ReadInt32();
            var sound_data = reader.ReadBytes(data_chunk_size);

            // Downgrade to mono
            if(num_channels == 2)
            {
                App.LogWarning("Sample {0} is in Stereo, will be downgraded to Mono to support 3D sound.", path);
                var bytesPerSample = bits_per_sample / 8;
                var numSamples = sound_data.Length / (num_channels * bytesPerSample);
                var monoData = new byte[numSamples * bytesPerSample];
                for( int i=0; i<monoData.Length; ++i)
                {
                    var sample = i / bytesPerSample;
                    var sampleByte = i % bytesPerSample;
                    monoData[i] = sound_data[2 * sample * bytesPerSample + sampleByte];
                }
                sound_data = monoData;
                num_channels = 1;
            }

            // Store the data
            var wave = new WaveData();
            wave.NumChannels = num_channels;
            wave.BitsPerSample = bits_per_sample;
            wave.SampleRate = sample_rate;
            wave.SoundData = sound_data;
            return wave;
        }

        private static ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1:
                    {
                        return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                    }
                case 2:
                    {
                        return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                    }
                default:
                    {
                        throw new NotSupportedException("The specified sound format is not supported.");
                    }
            }
        }
    }
}

