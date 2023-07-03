using Dan200.Core.Components.Core;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct ProjectileComponentData
    {
        [Range(Min = 0.0)]
        public float Speed;

        [Range(Min = 0.0)]
        [Optional(Default = 1.0f)]
        public float GravityScale;

        [Range(Min = 0.0)]
        public float Damage;

        public string ImpactPrefab;

        [Range(Min = 0.0f)]
        public float LifeSpan;
    }

    [RequireSystem(typeof(PhysicsSystem))]
    [RequireComponent(typeof(TransformComponent))]
    internal class ProjectileComponent : Component<ProjectileComponentData>, IPhysicsUpdate, IUpdate
    {
        private PhysicsSystem m_physics;
        private TransformComponent m_transform;

        private Vector3 m_lastPosition;
        private Vector3 m_lastVelocity;
        private Vector3 m_position;
        private Vector3 m_velocity;
        private float m_gravityScale;
        private float m_lifeSpan;

        private bool m_hit;
        private Entity m_hitEntity;
        private float m_age;

        private float m_damage;
        private EntityPrefab m_impactPrefab;

        public Entity DamageOrigin
        {
            get;
            set;
        }

        protected override void OnInit(in ProjectileComponentData properties)
        {
            m_physics = Level.GetSystem<PhysicsSystem>();
            m_transform = Entity.GetComponent<TransformComponent>();

            m_position = m_transform.LocalPosition;
            m_velocity = m_transform.LocalTransform.Forward * properties.Speed;
            m_lastPosition = m_position;
            m_lastVelocity = m_velocity;
            m_gravityScale = properties.GravityScale;
            m_lifeSpan = properties.LifeSpan;

            m_hit = false;
            m_hitEntity = null;
            m_age = 0.0f;

            m_damage = properties.Damage;
            m_impactPrefab = EntityPrefab.Get(properties.ImpactPrefab);
        }

        protected override void OnShutdown()
        {
        }

        public void PhysicsUpdate(float dt)
        {
            var pos = m_position;
            var vel = m_velocity;
            if (!m_hit)
            {
                var accel = m_physics.World.Gravity * m_gravityScale;
                var newVel = vel + accel * dt;
                var newPos = pos + vel * dt + 0.5f * accel * Mathf.Square(dt);

                var ray = new Ray(pos, newPos);
                RaycastResult result;
                if (m_physics.World.Raycast(ray, CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.Player | CollisionGroup.NPC, out result))
                {
                    var hitEntity = result.Shape.UserData as Entity;
                    if (hitEntity != DamageOrigin)
                    {
                        m_hit = true;
                        m_hitEntity = hitEntity;
                        m_position = result.Position;
                        m_velocity = Vector3.Zero;
                    }
                }

                if(!m_hit)
                {
                    m_velocity = newVel;
                    m_position = newPos;
                }
            }
            m_lastPosition = pos;
            m_lastVelocity = vel;
        }

        public void Update(float dt)
        {
            // Update transform
            float lerpFactor = m_physics.World.CurrentStepFraction;
            m_transform.LocalPosition = Vector3.Lerp(m_position, m_position + (m_position - m_lastPosition), lerpFactor);
            m_transform.LocalVelocity = Vector3.Lerp(m_lastVelocity, m_velocity, lerpFactor);

            if(m_hit)
            {
                // Process the hit
                if (m_hitEntity != null && !m_hitEntity.Dead)
                {
                    // Apply damage
                    var health = m_hitEntity.GetComponent<HealthComponent>();
                    if(health != null)
                    {
                        var damage = new Damage();
                        damage.Type = DamageType.Projectile;
                        damage.Ammount = m_damage;
                        damage.Origin = DamageOrigin;
                        damage.Position = m_transform.Position;
                        damage.Direction = m_transform.Transform.Forward;
                        health.ApplyDamage(damage);
                    }

                    // Create an explosion
                    var properties = new LuaTable();
                    properties["Position"] = m_position.ToLuaValue();
                    properties["Rotation"] = new Vector3(
                        GlobalRandom.Float(0.0f, 360.0f),
                        GlobalRandom.Float(0.0f, 360.0f),
                        GlobalRandom.Float(0.0f, 360.0f)
                    ).ToLuaValue();
                    var impact = m_impactPrefab.Instantiate(Level, properties);

                    // Propogate damage origin
                    var explosion = impact.GetComponent<ExplosionComponent>();
                    if(explosion != null)
                    {
                        explosion.DamageOrigin = DamageOrigin;
                    }

                    m_hitEntity = null;
                }
                Level.Entities.Destroy(Entity);
            }
            else
            {
                // Advance timeout
                m_age += dt;
                if (m_age >= m_lifeSpan)
                {
                    Level.Entities.Destroy(Entity);
                }
            }
        }
    }
}
