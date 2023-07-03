namespace Dan200.Core.Audio
{
    internal interface ISoundPlayback : IStoppablePlayback, ISpatialPlayback
    {
        bool Looping { get; }
        Sound Sound { get; }
    }
}

