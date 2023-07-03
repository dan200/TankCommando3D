namespace Dan200.Core.Audio
{
    internal interface ICustomPlayback : IStoppablePlayback, ISpatialPlayback
    {
        ICustomAudioSource Source { get; }
    }
}
