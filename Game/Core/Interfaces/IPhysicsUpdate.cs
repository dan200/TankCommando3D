using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces
{
	internal interface IPhysicsUpdate : IComponentInterface
	{
		void PhysicsUpdate(float dt);
	}
}
