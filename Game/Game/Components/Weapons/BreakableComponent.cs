using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Util;
using Dan200.Game.Components.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct BreakableComponentData
    {
    }

    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(DespawnerComponent))]
    internal class BreakableComponent : Component<BreakableComponentData>
    {
        private HealthComponent m_health;
        private TransformComponent m_transform;

        protected override void OnInit(in BreakableComponentData properties)
        {
            m_health = Entity.GetComponent<HealthComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_health.OnDeath += OnDeath;
        }

        protected override void OnShutdown()
        {
            m_health.OnDeath -= OnDeath;
        }

        private void OnDeath(HealthComponent health, DamageEventArgs args)
        {
            // Create an explosion
            var prefab = EntityPrefab.Get("entities/explosion.entity");
            var properties = new LuaTable();
            properties["Position"] = (m_transform.Position + 0.2f * Vector3.YAxis).ToLuaValue();
            properties["Rotation"] = new Vector3(
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f)
            ).ToLuaValue();
            properties["Radius"] = 4.0f;
            properties["Lifespan"] = 0.1f;
            properties["Damage"] = 80.0f;
            var explosion = prefab.Instantiate(Level, properties);

            // Propogate damage origin
            var explosionComponent = explosion.GetComponent<ExplosionComponent>();
            if (explosionComponent != null)
            {
                explosionComponent.DamageOrigin = args.Damage.Origin;
            }

            // Destroy ourselves
            Entity.GetComponent<DespawnerComponent>().Despawn();
        }
    }
}
