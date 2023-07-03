using Dan200.Core.Multiplayer;

namespace Dan200.Core.Network
{
    internal interface IRemoteUserConnection : IConnection
    {
        IRemoteUser RemoteUser { get; }
    }
}
