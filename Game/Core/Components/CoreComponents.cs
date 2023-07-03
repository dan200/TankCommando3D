using System;
using Dan200.Core.Level;
using Dan200.Core.Components.Physics;
using Dan200.Core.Components.Render;
using Dan200.Core.Components.Core;
using Dan200.Core.Components.Audio;
using Dan200.Core.Components.Misc;

namespace Dan200.Core.Components
{
    internal static class CoreComponents
    {
		public static void Register()
        {
            // Audio
            ComponentRegistry.RegisterComponent<AudioEmitterComponent, AudioEmmitterComponentData>("AudioEmitter");

            // Core
            ComponentRegistry.RegisterComponent<NameComponent, NameComponentData>("Name");
            ComponentRegistry.RegisterComponent<TransformComponent, TransformComponentData>("Transform");
            ComponentRegistry.RegisterComponent<HierarchyComponent, HierarchyComponentData>("Hierarchy");

            // Physics
            ComponentRegistry.RegisterComponent<BoxCollisionComponent, BoxCollisionComponentData>("BoxCollision");
            ComponentRegistry.RegisterComponent<MapCollisionComponent, MapCollisionComponentData>("MapCollision");
            ComponentRegistry.RegisterComponent<ModelCollisionComponent, ModelCollisionComponentData>("ModelCollision");
            ComponentRegistry.RegisterComponent<PhysicsComponent, PhysicsComponentData>("Physics");
            ComponentRegistry.RegisterComponent<SphereCollisionComponent, SphereCollisionComponentData>("SphereCollision");

            // Render
            ComponentRegistry.RegisterComponent<DirectionalLightComponent, DirectionalLightComponentData>("DirectionalLight");
            ComponentRegistry.RegisterComponent<MapComponent, MapComponentData>("Map");
            ComponentRegistry.RegisterComponent<ModelComponent, ModelComponentData>("Model");
            ComponentRegistry.RegisterComponent<ParticleSystemComponent, ParticleSystemComponentData>("ParticleSystem");
            ComponentRegistry.RegisterComponent<PointLightComponent, PointLightComponentData>("PointLight");

            // Misc
            ComponentRegistry.RegisterComponent<AnimationComponent, AnimationComponentData>("Animation");
            ComponentRegistry.RegisterComponent<InputComponent, InputComponentData>("Input");
            ComponentRegistry.RegisterComponent<RotatorComponent, RotatorComponentData>("Rotator");
            ComponentRegistry.RegisterComponent<TriggerComponent, TriggerComponentData>("Trigger");
        }
    }
}
