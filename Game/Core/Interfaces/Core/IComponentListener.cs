using System;
using Dan200.Core.Components;
using Dan200.Core.Level;

namespace Dan200.Core.Interfaces.Core
{
	internal interface IComponentListener : IComponentInterface
	{
		void OnComponentAdded(ComponentBase component);
		void OnComponentRemoved(ComponentBase component);
	}
}
