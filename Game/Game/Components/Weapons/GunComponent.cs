using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Components.Player;
using Dan200.Game.Interfaces;
using Dan200.Game.Systems.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct GunComponentData
    {
        public string Projectile;

        [Range(Min = 0.0)]
        public float Spread;

        [Range(Min = 0.0)]
        public float AutomaticFireRate;

        [Range(Min = 0.0)]
        public float ManualFireRate;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float Kickback;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float Recoil;

        [Range(Min = 0)]
        public int ClipSize;

        [Range(Min = 0)]
        [Optional(Default = 1)]
        public int ProjectilesPerShot;

        [Range(Min = 0)]
        [Optional(Default = 0.0f)]
        public float FireWhenDroppedSpeedThreshold;

        [Range(Min = 0, Max = 1)]
        [Optional(Default = 0.0f)]
        public float FireWhenDroppedChance;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float NoiseRadius;
    }

    [RequireSystem(typeof(NameSystem))]
    [RequireSystem(typeof(NoiseSystem))]
    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(NameComponent))]
    [AfterComponent(typeof(PhysicsComponent))]
    internal class GunComponent : Component<GunComponentData>, IInteractable, IUpdate
    {
        private NoiseSystem m_noise;
        private TransformComponent m_transform;
        private TransformComponent m_barrelTransform;
        private TransformComponent m_muzzleFlashTransform;
        private PhysicsComponent m_physics;

        private EntityPrefab m_projectilePrefab;
        private GunComponentData m_properties;

        private bool m_triggerHeld;
        private Vector3? m_targetPos;
        private bool m_misfire;

        private float m_muzzleFlashTimer; // Time until muzzle flash dissapears
        private float m_fireTimer; // Time until firing is allowed again 
        private int m_ammoInClip;

        public event StructEventHandler<GunComponent> OnFired;

        public bool TriggerHeld
        {
            get
            {
                return m_triggerHeld;
            }
            set
            {
                m_triggerHeld = value;
            }
        }

        public Vector3? TargetPos
        {
            get
            {
                return m_targetPos;
            }
            set
            {
                m_targetPos = value;
            }
        }

        public Entity DamageOrigin
        {
            get;
            set;
        }

        public int AmmoInClip
        {
            get
            {
                return m_ammoInClip;
            }
        }

        public GunComponentData Properties
        {
            get
            {
                return m_properties;
            }
        }
        
        protected override void OnInit(in GunComponentData properties)
        {
            m_noise = Level.GetSystem<NoiseSystem>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_barrelTransform = Level.GetSystem<NameSystem>().Lookup("./Barrel", Entity).GetComponent<TransformComponent>();
            m_muzzleFlashTransform = Level.GetSystem<NameSystem>().Lookup("./Barrel/MuzzleFlash", Entity).GetComponent<TransformComponent>();
            m_physics = Entity.GetComponent<PhysicsComponent>();
            if(m_physics != null)
            {
                m_physics.OnCollisionStart += OnCollisionStart;
            }

            m_projectilePrefab = EntityPrefab.Get(properties.Projectile);
            m_properties = properties;

            m_triggerHeld = false;
            m_targetPos = null;
            m_misfire = false;

            m_muzzleFlashTransform.Entity.Visible = false;
            m_muzzleFlashTimer = -1.0f;
            m_fireTimer = -1.0f;
            m_ammoInClip = properties.ClipSize;
        }

        private void OnCollisionStart(PhysicsComponent sender, CollisionStartEventArgs args)
        {
            var hitPhysics = args.HitEntity.GetComponent<PhysicsComponent>();
            var hitVelocity = (hitPhysics != null) ? hitPhysics.Object.Velocity : Vector3.Zero;
            var relativeVelocity = (sender.Object.Velocity - hitVelocity);
            if(relativeVelocity.Length >= m_properties.FireWhenDroppedSpeedThreshold &&
               GlobalRandom.Float() < m_properties.FireWhenDroppedChance)
            {
                m_misfire = true;
            }
        }

        protected override void OnShutdown()
        {
        }

        private void Fire()
        {
            App.Assert(m_ammoInClip > 0);

            // Calculate base transform
            var barrelTransform = m_barrelTransform.Transform;
            if(m_targetPos.HasValue)
            {
                var direction = (m_targetPos.Value - barrelTransform.Position).SafeNormalise(barrelTransform.Forward);
                barrelTransform.Rotation = Matrix3.CreateLook(direction, barrelTransform.Up);
            }

            // For each shot:
            int numShots = Math.Min(m_properties.ProjectilesPerShot, m_ammoInClip);
            for (int i = 0; i < m_properties.ProjectilesPerShot; ++i)
            {
                // Calculate projectile transform
                var projectileTransform = barrelTransform;
                projectileTransform.Rotation = Matrix3.CreateRotationZ(2.0f * Mathf.PI * GlobalRandom.Float()) * projectileTransform.Rotation;
                projectileTransform.Rotation = Matrix3.CreateRotationY(m_properties.Spread * Mathf.DEGREES_TO_RADIANS * GlobalRandom.Float()) * projectileTransform.Rotation;
                projectileTransform.Rotation = Matrix3.CreateRotationZ(2.0f * Mathf.PI * GlobalRandom.Float()) * projectileTransform.Rotation;

                // Create the projectile
                var properties = new LuaTable();
                properties["Position"] = projectileTransform.Position.ToLuaValue();
                properties["Rotation"] = (projectileTransform.Rotation.GetRotationAngles() * Mathf.RADIANS_TO_DEGREES).ToLuaValue();
                var projectile = m_projectilePrefab.Instantiate(Level, properties);

                // Propogate the damage origin
                var projectileComponent = projectile.GetComponent<ProjectileComponent>();
                if(projectileComponent != null)
                {
                    projectileComponent.DamageOrigin = DamageOrigin;
                }
            }

            // Deplete the ammo
            m_ammoInClip -= numShots;

            // Flash the muzzle
            m_muzzleFlashTransform.Entity.Visible = true;
            m_muzzleFlashTransform.LocalRotation = Matrix3.CreateRotationZ(2.0f * Mathf.PI * GlobalRandom.Float()) * m_muzzleFlashTransform.LocalRotation;
            m_muzzleFlashTimer = 0.05f;

            // Make some noise
            if (m_properties.NoiseRadius > 0.0f)
            {
                var noise = new Noise();
                noise.Origin = (DamageOrigin != null) ? DamageOrigin : Entity;
                noise.Position = barrelTransform.Position;
                noise.Radius = m_properties.NoiseRadius;
                m_noise.MakeNoise(noise);
            }

            // Inform listeners
            FireOnFired();
        }

        public bool CanInteract(Entity player, Interaction interaction)
        {
            if (interaction == Interaction.UseOnce)
            {
                var playerWeapon = player.GetComponent<PlayerWeaponHolderComponent>();
                if (playerWeapon != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Interact(Entity player, Interaction interaction)
        {
            if(CanInteract(player, interaction))
            {
                player.GetComponent<PlayerWeaponHolderComponent>().TakeWeapon(Entity);
                return true;
            }
            return false;
        }

        public void Update(float dt)
        {
            // Update firing
            m_fireTimer -= dt;
            if (m_triggerHeld || m_misfire)
            {
                if(m_fireTimer < 0.0f && m_ammoInClip > 0)
                {
                    Fire();
                    if(m_properties.AutomaticFireRate > 0.0f)
                    {
                        m_fireTimer = (1.0f / m_properties.AutomaticFireRate);
                    }
                    else
                    {
                        m_fireTimer = float.MaxValue;
                    }
                }
                m_misfire = false;
            }
            else
            {
                m_fireTimer = Mathf.Min(m_fireTimer, (1.0f / m_properties.ManualFireRate));
            }

            // Update muzzle flash
            if (m_muzzleFlashTimer > 0.0f)
            {
                m_muzzleFlashTimer -= dt;
                if(m_muzzleFlashTimer <= 0.0f)
                {
                    m_muzzleFlashTransform.Entity.Visible = false;
                }
            }
        }

        private void FireOnFired()
        {
            if(OnFired != null)
            {
                OnFired.Invoke(this, StructEventArgs.Empty);
            }
        }
    }
}
