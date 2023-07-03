using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces
{
	internal interface IDescendantListener : IComponentInterface
	{
		void OnComponentAdded(Entity descendant, ComponentBase component);
		void OnComponentRemoved(Entity descendant, ComponentBase component);
		void OnDescendantAdded(Entity descendant);
		void OnDescendantRemoved(Entity descendant);
	}
}
