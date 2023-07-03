using Dan200.Core.Multiplayer;

namespace Dan200.Core.Network
{
    internal interface IRemoteUserConnectionListener : IConnectionListener
    {
        bool Accept(out IRemoteUserConnection o_connection);
    }
}
