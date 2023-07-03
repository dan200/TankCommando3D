using System;
using Dan200.Core.Multiplayer;
using Dan200.Core.Util;

namespace Dan200.Core.Interfaces.Multiplayer
{
    internal interface IMessageReceiver
    {
        void ReceiveMessage(IConnection sender, NetworkReader reader);
    }
}
