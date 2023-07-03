using System;
using Dan200.Core.Level;
using Dan200.Game.Components.Player;
using Dan200.Game.Components.Editor;
using Dan200.Game.Components.Misc;
using Dan200.Game.Components.AI;
using Dan200.Game.Components.Weapons;

namespace Dan200.Game.Components
{
    internal static class GameComponents
    {
		public static void Register()
        {
            // AI
            ComponentRegistry.RegisterComponent<CharacterNameComponent, CharacterNameComponentData>("CharacterName");
            ComponentRegistry.RegisterComponent<NavGraphComponent, NavGraphComponentData>("NavGraph");
            ComponentRegistry.RegisterComponent<NavigationWaypointComponent, NavigationWaypointComponentData>("NavigationWaypoint");
            ComponentRegistry.RegisterComponent<NavigatorComponent, NavigatorComponentData>("Navigator");
            ComponentRegistry.RegisterComponent<PatrolRouteComponent, PatrolRouteComponentData>("PatrolRoute");
            ComponentRegistry.RegisterComponent<SightComponent, SightComponentData>("Sight");
            ComponentRegistry.RegisterComponent<SightTargetComponent, SightTargetComponentData>("SightTarget");
            ComponentRegistry.RegisterComponent<TankBehaviourComponent, TankBehaviourComponentData>("TankBehaviour");
            ComponentRegistry.RegisterComponent<TankGunnerComponent, TankGunnerComponentData>("TankGunner");
            ComponentRegistry.RegisterComponent<TankMoverComponent, TankMoverComponentData>("TankMover");
            ComponentRegistry.RegisterComponent<TankSpawnPointComponent, TankSpawnPointComponentData>("TankSpawnPoint");

            // Editor
            ComponentRegistry.RegisterComponent<EditorComponent, EditorComponentData>("_Editor");
            ComponentRegistry.RegisterComponent<TransformManipulatorComponent, TransformManipulatorComponentData>("_TransformManipulator");

            // Player
            ComponentRegistry.RegisterComponent<PlayerCameraComponent, PlayerCameraComponentData>("PlayerCamera");
            ComponentRegistry.RegisterComponent<PlayerHUDComponent, PlayerHUDComponentData>("PlayerHUD");
            ComponentRegistry.RegisterComponent<PlayerInputComponent, PlayerInputComponentData>("PlayerInput");
            ComponentRegistry.RegisterComponent<PlayerInteractionComponent, PlayerInteractionComponentData>("PlayerInteraction");
            ComponentRegistry.RegisterComponent<PlayerMovementComponent, PlayerMovementComponentData>("PlayerMovement");
            ComponentRegistry.RegisterComponent<PlayerSettingsComponent, PlayerSettingsComponentData>("PlayerSettings");
            ComponentRegistry.RegisterComponent<PlayerSpawnPointComponent, PlayerSpawnPointComponentData>("PlayerSpawnPoint");
            ComponentRegistry.RegisterComponent<PlayerStatsComponent, PlayerStatsComponentData>("PlayerStats");
            ComponentRegistry.RegisterComponent<PlayerTrackerComponent, PlayerTrackerComponentData>("PlayerTracker");
            ComponentRegistry.RegisterComponent<PlayerWeaponHolderComponent, PlayerWeaponHolderComponentData>("PlayerWeaponHolder");
            ComponentRegistry.RegisterComponent<TrackableComponent, TrackableComponentData>("Trackable");

            // Weapons
            ComponentRegistry.RegisterComponent<AmmoComponent, AmmoComponentData>("Ammo");
            ComponentRegistry.RegisterComponent<ExplosionComponent, ExplosionComponentData>("Explosion");
            ComponentRegistry.RegisterComponent<GunComponent, GunComponentData>("Gun");
            ComponentRegistry.RegisterComponent<GrenadeComponent, GrenadeComponentData>("Grenade");
            ComponentRegistry.RegisterComponent<HealthComponent, HealthComponentData>("Health");
            ComponentRegistry.RegisterComponent<ProjectileComponent, ProjectileComponentData>("Projectile");
            ComponentRegistry.RegisterComponent<BreakableComponent, BreakableComponentData>("Breakable");
            ComponentRegistry.RegisterComponent<WeaponComponent, WeaponComponentData>("Weapon");
            ComponentRegistry.RegisterComponent<WeaponSpawnPointComponent, WeaponSpawnPointComponentData>("WeaponSpawnPoint");

            // Misc
            ComponentRegistry.RegisterComponent<DespawnerComponent, DespawnerComponentData>("Despawner");
            ComponentRegistry.RegisterComponent<PropertyTestComponent, PropertyTestComponentData>("PropertyTest");
            ComponentRegistry.RegisterComponent<GrabbableComponent, GrabbaleComponentData>("Grabbable");
        }
    }
}
