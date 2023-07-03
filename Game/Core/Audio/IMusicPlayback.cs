namespace Dan200.Core.Audio
{
    internal interface IMusicPlayback : IStoppablePlayback
    {
        bool Looping { get; }
        Music Music { get; }
        void FadeToVolume(float target, float duration, bool thenStop = false);
    }
}

