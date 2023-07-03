using System;
using Dan200.Core.Components;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Core
{
	internal interface IHierarchyListener : IComponentInterface
	{
		void OnParentChanged(Entity oldParent, Entity newParent);
	}
}
