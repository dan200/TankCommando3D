using Dan200.Core.Components;
using Dan200.Core.Components.Core;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Components.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal struct TankSpawnPointComponentData
    {
        public string Prefab;
        public float RespawnTime;
    }

    [RequireSystem(typeof(PhysicsSystem))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(PatrolRouteComponent))]
    [RequireComponent(typeof(NameComponent))]
    internal class TankSpawnPointComponent : Component<TankSpawnPointComponentData>, IUpdate
    {
        private PhysicsSystem m_physics;
        private NameComponent m_name;
        private TransformComponent m_transform;
        private TankSpawnPointComponentData m_properties;

        private Entity m_tank;
        private float m_respawnTimer;

        public Matrix4 Transform
        {
            get
            {
                return m_transform.Transform;
            }
        }

        protected override void OnInit(in TankSpawnPointComponentData properties)
        {
            Entity.Visible = Level.InEditor;
            m_physics = Level.GetSystem<PhysicsSystem>();
            m_name = Entity.GetComponent<NameComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_properties = properties;
            App.Assert(m_name.Name != null);

            m_tank = null;
            m_respawnTimer = -1.0f;
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            if(m_tank == null || m_tank.Dead)
            {
                m_respawnTimer -= dt;
                if(m_respawnTimer < 0.0f && IsSafeToSpawn())
                {
                    Respawn();
                    m_respawnTimer = m_properties.RespawnTime;
                }
            }
        }

        private bool IsSafeToSpawn()
        {
            var playerMovement = Level.GetComponents<PlayerMovementComponent>().FirstOrDefault();
            if(playerMovement != null)
            {
                var spawnPos = m_transform.Position + Vector3.YAxis;
                var spawnDir = m_transform.Transform.Forward;
                var eyePos = playerMovement.EyePos;
                var eyeDir = playerMovement.EyeLook;

                // Not to close
                if((eyePos - spawnPos).Length < 20.0f)
                {
                    return false;
                }
                var eyeToTankDir = (spawnPos - eyePos).Normalise();
                var tankToEyeDir = -eyeToTankDir;
                var dotLimit = Mathf.Cos(90.0f * Mathf.DEGREES_TO_RADIANS);

                // Tank not in player view
                RaycastResult result;
                if (eyeToTankDir.Dot(eyeDir) > dotLimit &&
                   !m_physics.World.Raycast(new Ray(eyePos, spawnPos), CollisionGroup.Environment, out result))
                {
                    return false;
                }

                // Player not in tank view
                if (tankToEyeDir.Dot(spawnDir) > dotLimit &&
                   !m_physics.World.Raycast(new Ray(spawnPos, eyePos), CollisionGroup.Environment, out result))
                {
                    return false;
                }
            }
            return true;
        }

        private void Respawn()
        {
            var prefab = EntityPrefab.Get(m_properties.Prefab);
            var properties = new LuaTable();
            properties["Position"] = m_transform.Position.ToLuaValue();
            properties["Rotation"] = (m_transform.Transform.GetRotationAngles() * Mathf.RADIANS_TO_DEGREES).ToLuaValue();
            properties["PatrolRoutePath"] = '/' + m_name.Name;
            m_tank = prefab.Instantiate(Level, properties);
        }
    }
}
