using System;

namespace Dan200.Core.Multiplayer
{
    [Flags]
    internal enum Side
    {
        Server = 1,
        Client = 2,
        Both = Client | Server,
    }
}
