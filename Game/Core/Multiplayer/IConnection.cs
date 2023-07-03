using System;

namespace Dan200.Core.Multiplayer
{
    internal interface IConnection : IDisposable
    {
        bool IsLocal { get; }
        TimeSpan PingTime { get; }
        ConnectionStatus Status { get; }
        void Send(IMessage message);
        bool Receive(out Packet o_packet);
        void Flush();
        void Disconnect();
    }
}
