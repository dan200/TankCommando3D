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
    internal struct GrenadeComponentData
    {
        [Range(Min = 0.0)]
        public float Timer;
    }

    [RequireSystem(typeof(NameSystem))]
    [RequireComponent(typeof(PhysicsComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(PhysicsComponent))]
    internal class GrenadeComponent : Component<GrenadeComponentData>, IInteractable, IUpdate
    {
        private PhysicsComponent m_physics;
        private TransformComponent m_transform;
        private GrenadeComponentData m_properties;

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
            m_properties = properties;

            m_fuseTimer = -1.0f;
        }
        
        protected override void OnShutdown()
        {
        }

        public void LightFuse()
        {
            if(m_fuseTimer < 0.0f)
            {
                m_fuseTimer = m_properties.Timer;
            }
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
            // Update fuze
            if(m_fuseTimer >= 0.0f)
            {
                m_fuseTimer -= dt;
                if(m_fuseTimer < dt)
                {
                    Explode();
                    Level.Entities.Destroy(Entity);
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
            var explosion = prefab.Instantiate(Level, properties);

            // Propogate damage origin
            var explosionComponent = explosion.GetComponent<ExplosionComponent>();
            if (explosionComponent != null)
            {
                explosionComponent.DamageOrigin = DamageOrigin;
            }
        }
    }
}
