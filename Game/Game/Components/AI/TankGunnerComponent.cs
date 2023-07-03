using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Components.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct TankGunnerComponentData
    {
        public float TurnRate;
        public string GunPath;
    }

    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(GunComponent))]
    [RequireSystem(typeof(NameSystem))]
    [AfterComponent(typeof(NameComponent))]
    [RequireComponent(typeof(HealthComponent))]
    internal class TankGunnerComponent : Component<TankGunnerComponentData>, IUpdate
    {
        private TransformComponent m_transform;
        private GunComponent m_gun;
        private HealthComponent m_health;
        private TankGunnerComponentData m_properties;

        private enum Behaviour
        {
            AimForward,
            AimAtTarget,
            ShootAtTarget,
            LookAroundRandomly,
        }
        private Behaviour m_behaviour;
        private Entity m_target;
        private Vector3 m_targetOffset;
        private float m_nextRandomYaw;

        protected override void OnInit(in TankGunnerComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            m_gun = Level.GetSystem<NameSystem>().Lookup(properties.GunPath, Entity).GetComponent<GunComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_gun.DamageOrigin = Entity;
            m_properties = properties;

            m_behaviour = Behaviour.AimForward;
            m_target = null;
        }

        protected override void OnShutdown()
        {
        }

        public void AimForward()
        {
            m_behaviour = Behaviour.AimForward;
            m_target = null;
        }

        public void AimAt(Vector3 pos)
        {
            AimAt(null, pos);
        }

        public void AimAt(Entity target, Vector3 offset)
        {
            m_behaviour = Behaviour.AimAtTarget;
            m_target = target;
            m_targetOffset = offset;
        }

        public void ShootAt(Vector3 pos)
        {
            AimAt(null, pos);
        }

        public void ShootAt(Entity target, Vector3 offset)
        {
            m_behaviour = Behaviour.ShootAtTarget;
            m_target = target;
            m_targetOffset = offset;
        }

        public void LookAroundRandomly()
        {
            if(m_behaviour != Behaviour.LookAroundRandomly)
            {
                m_behaviour = Behaviour.LookAroundRandomly;
                m_target = null;
                m_nextRandomYaw = GlobalRandom.Float() * 2.0f * Mathf.PI;
            }
        }

        public void Update(float dt)
        {
            // Do nothing if dead
            if(m_health.IsDead)
            {
                m_gun.TriggerHeld = false;
                m_gun.TargetPos = null;
                return;
            }

            var transform = m_transform.LocalTransform;
            var angularVelocity = Vector3.Zero;
            var fwd = transform.Forward;

            // Find the target position
            var targetPosition = m_targetOffset;
            if (m_target != null)
            {
                targetPosition = m_target.GetComponent<TransformComponent>().Transform.ToWorldPos(targetPosition);
            }

            // Determine target yaw
            float yaw = Mathf.ATan2(fwd.X, fwd.Z);
            float targetYaw;
            switch (m_behaviour)
            {
                case Behaviour.AimForward:
                default:
                    targetYaw = 0.0f;
                    break;
                case Behaviour.AimAtTarget:
                case Behaviour.ShootAtTarget:
                    var parentTransform = m_transform.ParentTransform;
                    var targetPositionLS = parentTransform.ToLocalPos(targetPosition);
                    targetYaw = Mathf.ATan2(targetPositionLS.X, targetPositionLS.Z);
                    break;
                case Behaviour.LookAroundRandomly:
                    targetYaw = m_nextRandomYaw;
                    break;
            }

            // Do the turn
            var angleDiff = Mathf.AngleDiff(targetYaw, yaw);
            var turnSpeed = Mathf.Sign(angleDiff) * m_properties.TurnRate * Mathf.DEGREES_TO_RADIANS;
            var turnThisFrame = turnSpeed * dt;
            if (Mathf.Abs(turnThisFrame) >= Mathf.Abs(angleDiff))
            {
                turnThisFrame = angleDiff;
                if (m_behaviour == Behaviour.LookAroundRandomly)
                {
                    m_nextRandomYaw = GlobalRandom.Float() * 2.0f * Mathf.PI;
                }
            }
            m_transform.LocalTransform = Matrix4.CreateRotationY(turnThisFrame) * transform;

            // Update firing
            if(m_behaviour == Behaviour.ShootAtTarget)
            {
                m_gun.TriggerHeld = (Mathf.Abs(angleDiff) < 0.5f * Mathf.DEGREES_TO_RADIANS);
                m_gun.TargetPos = targetPosition;
            }
            else
            {
                m_gun.TriggerHeld = false;
                m_gun.TargetPos = null;
            }
        }
    }
}
