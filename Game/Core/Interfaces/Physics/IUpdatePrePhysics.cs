using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Physics
{
    internal interface IUpdatePrePhysics : IComponentInterface
    {
        void UpdatePrePhysics(float dt);
    }
}
