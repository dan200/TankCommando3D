using System;
using Dan200.Core.Components;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces
{
	internal interface IHierarchyListener : IComponentInterface
	{
		void OnParentChanged(Entity oldParent, Entity newParent);
		void OnChildAdded(Entity child);
		void OnChildRemoved(Entity child);
	}
}
