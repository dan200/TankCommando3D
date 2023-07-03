using Dan200.Core.Math;

namespace Dan200.Core.Multiplayer
{
    internal interface IReplicator
    {
        void Replicate(ref int io_value);
        void Replicate(ref float io_value);
        void Replicate(ref double io_value);
        void Replicate(ref string io_value);
        void Replicate(ref Vector3 io_value);
        void Replicate(ref Matrix4 io_value);
    }
}
