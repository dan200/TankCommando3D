using System;
using Dan200.Core.Util;

namespace Dan200.Core.Multiplayer
{
    internal interface IConnection : IDisposable
    {
        bool IsLocal { get; }
        float PingTime { get; }
        ConnectionState State { get; }
        void SendMessage(ByteString message);
        bool Receive(out Packet o_packet);
        void Flush();
        void Disconnect();
    }
}
