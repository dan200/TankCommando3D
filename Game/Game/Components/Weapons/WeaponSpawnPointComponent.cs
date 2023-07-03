using Dan200.Core.Components;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal struct WeaponSpawnPointComponentData
    {
        [Range(Min = 0, Max = 1)]
        public float EnableChance;

        public string[] Prefabs;

        [Range(Min = 0)]
        public float RespawnTimer;
    }

    [RequireComponent(typeof(TransformComponent))]
    internal class WeaponSpawnPointComponent : Component<WeaponSpawnPointComponentData>, IUpdate
    {
        private TransformComponent m_transform;
        private WeaponSpawnPointComponentData m_properties;

        private Entity m_spawnedObject;
        private float m_respawnTimer;

        protected override void OnInit(in WeaponSpawnPointComponentData properties)
        {
            Entity.Visible = Level.InEditor;
            m_transform = Entity.GetComponent<TransformComponent>();
            m_properties = properties;

            App.Assert(properties.Prefabs.Length > 0);
            if(GlobalRandom.Float() < properties.EnableChance)
            {
                Respawn();
            }
            m_respawnTimer = -1.0f;
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            if(m_spawnedObject != null && m_spawnedObject.Dead)
            {
                m_spawnedObject = null;
                m_respawnTimer = m_properties.RespawnTimer;
            }
            if(m_respawnTimer >= 0.0f)
            {
                m_respawnTimer -= dt;
                if (m_respawnTimer < 0.0f)
                {
                    Respawn();
                }
            }
        }

        private void Respawn()
        {
            App.Assert(m_properties.Prefabs.Length > 0);

            var prefabIndex = GlobalRandom.Int(0, m_properties.Prefabs.Length - 1);
            var prefab = EntityPrefab.Get(m_properties.Prefabs[prefabIndex]);
            var properties = new LuaTable();
            properties["Position"] = m_transform.Position.ToLuaValue();
            properties["Rotation"] = (m_transform.Transform.GetRotationAngles() * Mathf.RADIANS_TO_DEGREES).ToLuaValue();
            m_spawnedObject = prefab.Instantiate(Level, properties, 1); // TODO
        }
    }
}
