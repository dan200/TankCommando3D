using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
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
    internal enum TriggerMechanism
    {
        Timer,
        Proximity,
    }

    internal struct GrenadeComponentData
    {
        [Optional(Default = TriggerMechanism.Timer)]
        public TriggerMechanism TriggerMechanism;

        [Optional(Default = 1.0f)]
        public float Range;

        [Range(Min = 0.0)]
        public float Timer;
    }

    [RequireSystem(typeof(NameSystem))]
    [RequireComponent(typeof(PhysicsComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(WeaponComponent))]
    [AfterComponent(typeof(HealthComponent))]
    internal class GrenadeComponent : Component<GrenadeComponentData>, IUpdate, IDamagePropagator
    {
        private PhysicsComponent m_physics;
        private TransformComponent m_transform;
        private HealthComponent m_health;
        private GrenadeComponentData m_properties;

        private bool m_armed;
        private float m_fuseTimer;
       
        public Entity DamageOrigin
        {
            get;
            set;
        }

        public GrenadeComponentData Properties
        {
            get
            {
                return m_properties;
            }
        }
        
        protected override void OnInit(in GrenadeComponentData properties)
        {
            m_physics = Entity.GetComponent<PhysicsComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_properties = properties;

            m_armed = false;
            m_fuseTimer = -1.0f;

            if (m_health != null)
            {
                m_health.Invulnerable = true;
                m_health.OnDeath += OnDeath;
            }
        }

        protected override void OnShutdown()
        {
            if(m_health != null)
            {
                m_health.OnDeath -= OnDeath;
            }
        }

        private void OnDeath(HealthComponent sender, DamageEventArgs args)
        {
            if(m_armed)
            {
                Explode();
            }
        }

        public void Arm()
        {
            m_armed = true;
            if (m_properties.TriggerMechanism == TriggerMechanism.Timer)
            {
                m_fuseTimer = m_properties.Timer;
            }
            if (m_health != null)
            {
                m_health.Invulnerable = false;
            }
        }

        public void Update(float dt)
        {
            if (m_armed)
            {
                // Check for proximity
                if (m_properties.TriggerMechanism == TriggerMechanism.Proximity && m_fuseTimer < 0.0f)
                {
                    var sphere = new Sphere(m_transform.Position, m_properties.Range);
                    var contacts = new List<Contact>();
                    var numContacts = m_physics.Object.World.SphereTest(sphere, CollisionGroup.NPC | CollisionGroup.Player, contacts);
                    foreach(var contact in contacts)
                    {
                        var entity = contact.Shape.UserData as Entity;
                        if(entity != DamageOrigin)
                        {
                            m_fuseTimer = m_properties.Timer;
                            break;
                        }
                    }
                }

                // Countdown the fuse
                if (m_fuseTimer >= 0.0f)
                {
                    m_fuseTimer -= dt;
                    if (m_fuseTimer < dt)
                    {
                        Explode();
                    }
                }
            }
        }

        private void Explode()
        {
            // Create an explosion
            var prefab = EntityPrefab.Get("entities/explosion.entity");
            var properties = new LuaTable();
            properties["Position"] = m_physics.Object.CenterOfMass.ToLuaValue();
            properties["Rotation"] = new Vector3(
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f)
            ).ToLuaValue();
            properties["Radius"] = 8.0f;
            properties["Lifespan"] = 0.2f;
            properties["Damage"] = 100.0f;
            var explosion = prefab.Instantiate(Level, properties, 1); // TODO

            // Propogate damage origin
            foreach(var propagator in explosion.GetComponentsWithInterface<IDamagePropagator>())
            {
                propagator.DamageOrigin = DamageOrigin;
            }

            // Die
            Level.Entities.Destroy(Entity);
        }
    }
}
