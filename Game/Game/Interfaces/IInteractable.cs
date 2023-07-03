using System;
using Dan200.Core.Level;

namespace Dan200.Game.Interfaces
{
	internal enum Interaction
	{
		Grab,
		UseOnce,
		StartUse,
		EndUse,
		Drop,
	}

	internal interface IInteractable : IInterface
	{
        bool CanInteract(Entity player, Interaction action);
        bool Interact(Entity player, Interaction action);
	}
}
