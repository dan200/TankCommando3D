using System;
using Dan200.Core.Level;
using Dan200.Core.Render;

namespace Dan200.Core.Interfaces
{
	internal interface IDraw : IComponentInterface
	{
		void AddToDrawQueue(DrawQueue queue);
	}
}
