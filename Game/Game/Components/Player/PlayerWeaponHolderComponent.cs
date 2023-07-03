using Dan200.Core.Geometry;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Main;
using Dan200.Core.Interfaces;
using Dan200.Core.Lua;
using Dan200.Core.Systems;
using Dan200.Core.Components;
using Dan200.Game.Interfaces;
using System.Linq;
using Dan200.Core.Util;
using Dan200.Core.Components.Core;
using Dan200.Game.Components.Misc;
using Dan200.Game.Components.Weapons;
using Dan200.Core.Components.Physics;
using Dan200.Core.Serialisation;
using Dan200.Game.Components.AI;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerWeaponHolderComponentData
    {
        [Range(Min = 0.0)]
        public float ThrowChargeTime;
    }

    [RequireSystem(typeof(PhysicsSystem))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(PlayerInputComponent))]
    [RequireComponent(typeof(PlayerMovementComponent))]
    [RequireComponent(typeof(PlayerStatsComponent))]
    [RequireComponent(typeof(HealthComponent))]
    internal class PlayerWeaponHolderComponent : Component<PlayerWeaponHolderComponentData>, IUpdate, IDamageOrigin
	{
        private PhysicsSystem m_physics;
        private TransformComponent m_transform;
        private PlayerInputComponent m_input;
		private PlayerMovementComponent m_movement;
        private PlayerStatsComponent m_stats;
        private HealthComponent m_health;
        private PlayerWeaponHolderComponentData m_properties;

        private Entity m_heldWeapon;
        private float m_weaponBobPhase;
        private float m_weaponKickback;
        private float m_throwCharge;

        public Entity Weapon
        {
            get
            {
                return m_heldWeapon;
            }
        }

        protected override void OnInit(in PlayerWeaponHolderComponentData properties)
		{
            m_physics = Level.GetSystem<PhysicsSystem>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_input = Entity.GetComponent<PlayerInputComponent>();
			m_movement = Entity.GetComponent<PlayerMovementComponent>();
            m_stats = Entity.GetComponent<PlayerStatsComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_properties = properties;

            m_heldWeapon = null;
            m_weaponBobPhase = 0.0f;
            m_throwCharge = -1.0f;
        }
        
        protected override void OnShutdown()
		{
			if (m_heldWeapon != null)
			{
                DropWeapon();
			}
		}

        public void TakeWeapon(Entity weapon)
        {
            if(m_heldWeapon != null)
            {
                DropWeapon();
            }

            m_heldWeapon = weapon;
            m_weaponBobPhase = 0.0f;
            m_throwCharge = -1.0f;

            var weaponHierarchy = m_heldWeapon.GetComponent<HierarchyComponent>();
            if(weaponHierarchy != null)
            {
                weaponHierarchy.Parent = null;
            }

            var weaponPhysics = m_heldWeapon.GetComponent<PhysicsComponent>();
            if (weaponPhysics != null)
            {
                weaponPhysics.Object.Kinematic = true;
                weaponPhysics.Object.IgnoreCollision = true;
            }

            var weaponGun = m_heldWeapon.GetComponent<GunComponent>();
            if (weaponGun != null)
            {
                weaponGun.OnFired += OnWeaponFired;
            }

            var despawner = m_heldWeapon.GetComponent<DespawnerComponent>();
            if (despawner != null)
            {
                despawner.CancelDespawn();
            }
        }

        public void DropWeapon()
        {
            ThrowWeapon(0.0f);
        }

        public void ThrowWeapon(float charge)
        {
            App.Assert(m_heldWeapon != null);
            App.Assert(charge >= 0.0f && charge <= 1.0f);

            var weaponTransform = m_heldWeapon.GetComponent<TransformComponent>();
            var weaponPhysics = m_heldWeapon.GetComponent<PhysicsComponent>();
            if (weaponPhysics != null)
            {
                var scaledCharge = Mathf.Pow(charge, 2.0f);
                weaponPhysics.Object.Kinematic = false;
                weaponPhysics.Object.IgnoreCollision = false;
                weaponPhysics.Object.Awake = true;
                weaponPhysics.Object.Velocity += m_movement.EyeLook * scaledCharge * 18.0f;
                weaponPhysics.Object.AngularVelocity += weaponTransform.Transform.Right * scaledCharge * 10.0f;
            }

            var weaponGun = m_heldWeapon.GetComponent<GunComponent>();
            if (weaponGun != null)
            {
                weaponGun.TriggerHeld = false;
                weaponGun.TargetPos = null;
                weaponGun.DamageOrigin = null;
                weaponGun.OnFired -= OnWeaponFired;
            }

            var despawner = m_heldWeapon.GetComponent<DespawnerComponent>();
            if(weaponGun != null && weaponGun.AmmoInClip == 0 && despawner != null)
            {
                despawner.Despawn();
            }

            m_heldWeapon = null;
        }

        private void OnWeaponFired(GunComponent sender, StructEventArgs args)
        {
            m_movement.AddRecoil(sender.Properties.Recoil * Mathf.DEGREES_TO_RADIANS);
            m_weaponKickback = Mathf.Max(sender.Properties.Kickback, m_weaponKickback);
        }

        public void Update(float dt)
		{
            // Ignore a deleted weapon
            if(m_heldWeapon != null && m_heldWeapon.Dead)
            {
                m_heldWeapon = null;
                m_throwCharge = -1.0f;
            }

            // Drop the weapon on death
            if(m_health.IsDead && m_heldWeapon != null)
            {
                DropWeapon();
            }

            // If we have a weapon:
            if (m_heldWeapon != null)
			{
                // Update weapon bob
                m_weaponBobPhase += (m_movement.Velocity.WithY(0.0f).Length * dt) / 2.0f;
                m_weaponKickback = Mathf.ApproachDecay(m_weaponKickback, 0.0f, dt, 3.0f);

                // Position the weapon
                var eyePos = m_movement.EyePos;
                var eyeFwd = m_movement.EyeLook;
                var weaponTransform = m_heldWeapon.GetComponent<TransformComponent>();
                if(weaponTransform != null)
                {
                    var eyeTransform = m_movement.EyeTransform;
                    var feetTransform = m_transform.Transform;
                    var gunPos = eyePos;
                    gunPos += feetTransform.Forward * 0.1f;
                    gunPos += 0.01f * eyeTransform.Right * Mathf.Sin(m_weaponBobPhase * 0.5f * Mathf.TWO_PI);
                    gunPos += 0.02f * eyeTransform.Up * Mathf.Cos(m_weaponBobPhase * Mathf.TWO_PI);
                    gunPos -= m_weaponKickback * eyeTransform.Forward;
                    weaponTransform.LocalTransform = Matrix4.CreateLookAt(gunPos, gunPos + eyeFwd, feetTransform.Up);
                    if (m_throwCharge > 0.0f)
                    {
                        var rotOffset = new Vector3(0.0f, -0.6f, -0.3f);
                        weaponTransform.LocalTransform =
                            Matrix4.CreateTranslation(-rotOffset) *
                            Matrix4.CreateRotationX(-Mathf.Pow(m_throwCharge, 2.0f) * 50.0f * Mathf.DEGREES_TO_RADIANS) *
                            Matrix4.CreateTranslation(rotOffset) *
                            weaponTransform.LocalTransform;
                    }
                    weaponTransform.LocalVelocity = m_movement.Velocity;
                    weaponTransform.LocalAngularVelocity = m_movement.AngularVelocity;
                }

                // Fire the weapon
                var weaponGun = m_heldWeapon.GetComponent<GunComponent>();
                if (weaponGun != null)
                {
                    if (m_input.Fire && m_throwCharge < 0.0f)
                    {
                        var ray = new Ray(eyePos, eyeFwd, 100.0f);
                        RaycastResult result;
                        Vector3 targetPos;
                        if (m_physics.World.Raycast(ray, CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.NPC, out result))
                        {
                            targetPos = ray.Origin + Mathf.Max(result.Distance, 2.0f) * ray.Direction;
                        }
                        else
                        {
                            targetPos = ray.Origin + ray.Length * ray.Direction;
                        }
                        weaponGun.TriggerHeld = true;
                        weaponGun.TargetPos = targetPos;
                        weaponGun.DamageOrigin = Entity;
                    }
                    else
                    {
                        weaponGun.TriggerHeld = false;
                        weaponGun.TargetPos = null;
                        weaponGun.DamageOrigin = null;
                    }
                }

                // Drop the weapon
                if (m_input.Throw && !m_health.IsDead)
                {
                    // Start the throw
                    App.Assert(m_properties.ThrowChargeTime > 0.0f);
                    if (m_throwCharge < 0.0f)
                    {
                        m_throwCharge = 0.0f;

                        var weaponGrenade = m_heldWeapon.GetComponent<GrenadeComponent>();
                        if (weaponGrenade != null)
                        {
                            weaponGrenade.DamageOrigin = Entity;
                            weaponGrenade.LightFuse();
                        }
                    }

                    // Charge up the throw
                    m_throwCharge = Mathf.Min( m_throwCharge + dt / m_properties.ThrowChargeTime, 1.0f );
                }
                else if(m_throwCharge >= 0.0f)
                {
                    // Release the throw
                    ThrowWeapon(m_throwCharge);
                    m_throwCharge = -1.0f;
                }
            }
		}

        public void NotifyDamageDealt(HealthComponent hurtComponent, in Damage damage)
        {
            if(hurtComponent.IsDead && hurtComponent.Entity.GetComponent<TankBehaviourComponent>() != null)
            {
                m_stats.AddStat(PlayerStatistic.TanksKilled);
            }
        }
    }
}
