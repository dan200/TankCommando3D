namespace Dan200.Core.Audio
{
    internal interface ICustomAudioSource
    {
        void GenerateSamples(in AudioBuffer buffer);
    }
}
