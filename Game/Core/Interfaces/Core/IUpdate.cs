using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Core
{
    internal interface IUpdate : IInterface
    {
        void Update(float dt);
    }
}
