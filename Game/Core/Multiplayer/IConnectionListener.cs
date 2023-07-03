using System;

namespace Dan200.Core.Multiplayer
{ 
    internal interface IConnectionListener : IDisposable
    {
        bool IsOpen { get; }
        void Open();
        void Close();
        bool Accept(out IConnection o_connection);
    }
}
