using System;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Core
{
	internal interface IAncestryListener : IComponentInterface
	{
		void OnComponentAdded(Entity ancestor, ComponentBase component);
		void OnComponentRemoved(Entity ancestor, ComponentBase component);
		void OnAncestryChanged();
	}
}
