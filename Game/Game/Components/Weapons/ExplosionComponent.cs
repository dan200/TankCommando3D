using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Components.Render;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Interfaces;
using Dan200.Game.Systems.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct ExplosionComponentData
    {
        [Range(Min = 0.0)]
        public float StartRadius;

        [Range(Min = 0.0)]
        public float EndRadius;

        [Range(Min = 0.0)]
        public float Lifespan;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float Damage;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float NoiseRadius;

        [Range(Min = 0.0)]
        [Optional(Default = 0.0f)]
        public float PhysicsImpulse;
    }

    [RequireSystem(typeof(NoiseSystem))]
    [RequireComponentOnAncestor(typeof(PhysicsWorldComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(ModelComponent))]
    [AfterComponent(typeof(HealthComponent))]
    internal class ExplosionComponent : Component<ExplosionComponentData>, IUpdate, IDamagePropagator
    {
        private PhysicsWorldComponent m_physics;
        private NoiseSystem m_noise;
        private TransformComponent m_transform;
        private ModelComponent m_model;
        private ExplosionComponentData m_properties;

        private HashSet<Entity> m_hitEntities;
        private HashSet<Entity> m_damagedEntities;
        private float m_age;

        public Entity DamageOrigin
        {
            get;
            set;
        }

        protected override void OnInit(in ExplosionComponentData properties)
        {
            m_physics = Entity.GetComponentOnAncestor<PhysicsWorldComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_model = Entity.GetComponent<ModelComponent>();
            m_properties = properties;

            m_hitEntities = new HashSet<Entity>();
            m_damagedEntities = new HashSet<Entity>();
            m_age = -1.0f;
        }

        protected override void OnShutdown()
        {
        }
        
        public void Update(float dt)
        {
            if(m_age < 0.0f)
            {
                MakeNoise();
                m_age = 0.0f;
            }
            m_age += dt;

            var radius = UpdateScale();
            ApplyDamage(radius);
            if (m_age >= m_properties.Lifespan)
            {
                Level.Entities.Destroy(Entity);
            }
        }

        private float UpdateScale()
        {
            App.Assert(m_properties.Lifespan > 0.0f);
            var size = Mathf.Lerp(m_properties.StartRadius, m_properties.EndRadius, Mathf.Saturate(m_age / m_properties.Lifespan));
            var transform = Matrix4.CreateScale(new Vector3(size, size, size));
            for(int i=0; i<m_model.Instance.Model.GroupCount; ++i)
            {
                var groupName = m_model.Instance.Model.GetGroupName(i);
                m_model.Instance.SetGroupTransform(groupName, transform);
            }
            return size;
        }

        private void MakeNoise()
        {
            if (m_properties.NoiseRadius > 0.0f)
            {
                var noise = new Noise();
                noise.Origin = Entity;
                noise.Position = m_transform.Position;
                noise.Radius = m_properties.NoiseRadius;
                m_noise.MakeNoise(noise);
            }
        }

        private void ApplyDamage(float radius)
        {
            if(m_properties.Damage > 0.0f && radius > 0.0f)
            {
                var sphere = new Sphere(m_transform.Position, radius);
                var contacts = new List<Contact>();
                var numContacts = m_physics.World.SphereTest(sphere, CollisionGroup.Prop | CollisionGroup.NPC | CollisionGroup.Player, contacts);
                for(int i=0; i<contacts.Count; ++i)
                {
                    var contact = contacts[i];
                    var entity = contact.Shape.UserData as Entity;
                    if(entity != null && !entity.Dead && !m_hitEntities.Contains(entity))
                    {
                        // Check raycast
                        RaycastResult result;
                        if (!m_physics.World.Raycast(new Ray(m_transform.Position, contact.Position), CollisionGroup.Environment, out result))
                        {
                            var direction = (contact.Position - sphere.Center).SafeNormalise(Vector3.YAxis);

                            // Apply forces
                            var physics = entity.GetComponent<PhysicsComponent>();
                            if (physics != null)
                            {
                                physics.Object.ApplyImpulseAtPos(
                                    direction * m_properties.PhysicsImpulse,
                                    contact.Position
                                );
                            }

                            // Damage the entity
                            var health = entity.GetComponent<HealthComponent>();
                            if (health != null)
                            {
                                if (!m_damagedEntities.Contains(entity))
                                {
                                    var damage = new Damage();
                                    damage.Type = DamageType.Explosion;
                                    damage.Ammount = m_properties.Damage;
                                    damage.Origin = DamageOrigin;
                                    damage.Position = contact.Position;
                                    damage.Direction = direction;
                                    health.ApplyDamage(damage);

                                    m_damagedEntities.Add(entity);
                                    if (health.Redirect != null)
                                    {
                                        m_damagedEntities.Add(health.Redirect.Entity);
                                    }
                                }
                            }

                            m_hitEntities.Add(entity);
                        }
                    }
                }
            }
        }
    }
}
