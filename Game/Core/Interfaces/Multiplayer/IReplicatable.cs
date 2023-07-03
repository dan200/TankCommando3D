using System;
using Dan200.Core.Level;
using Dan200.Core.Multiplayer;

namespace Dan200.Core.Interfaces.Multiplayer
{
    internal interface IReplicatable : IInterface
    {
        void Replicate(IReplicator replicator);
    }
}
