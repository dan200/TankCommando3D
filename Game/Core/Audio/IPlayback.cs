using Dan200.Core.Math;

namespace Dan200.Core.Audio
{
    internal interface IPlayback
    {
		AudioCategory Category { get; }
        float Volume { get; set; }
    }

    internal interface IStoppablePlayback : IPlayback
    {
        bool Stopped { get; }
        void Stop();
    }

    internal interface ISpatialPlayback : IPlayback
    {
        Vector3 Position { get; set; }
        Vector3 Velocity { get; set; }
        float MinRange { get; set; }
        float MaxRange { get; set; }
    }
}

