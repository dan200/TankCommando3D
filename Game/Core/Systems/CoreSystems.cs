using System;
using Dan200.Core.Level;

namespace Dan200.Core.Systems
{
    internal static class CoreSystems
    {
        public static void Register()
        {
            ComponentRegistry.RegisterSystem<AudioSystem>("Audio");
            ComponentRegistry.RegisterSystem<GUISystem>("GUI");
            ComponentRegistry.RegisterSystem<LightingSystem>("Lighting");
            ComponentRegistry.RegisterSystem<NameSystem>("Name");
            ComponentRegistry.RegisterSystem<PhysicsSystem>("Physics");
            ComponentRegistry.RegisterSystem<ScriptSystem>("Script");
        }
    }
}
