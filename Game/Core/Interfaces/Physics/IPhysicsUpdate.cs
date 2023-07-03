using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Physics
{
	internal interface IPhysicsUpdate : IComponentInterface
	{
		void PhysicsUpdate(float dt);
	}
}
