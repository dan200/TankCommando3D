using System;
using Dan200.Core.Level;
using Dan200.Game.Systems.AI;

namespace Dan200.Game.Systems
{
    internal static class GameSystems
    {
		public static void Register()
        {
            // AI
            ComponentRegistry.RegisterSystem<ChatterSystem>("Chatter");
            ComponentRegistry.RegisterSystem<NoiseSystem>("Noise");
        }
    }
}
