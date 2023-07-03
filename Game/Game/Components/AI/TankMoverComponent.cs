using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Game.Components.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct TankMoverComponentData
    {
        public float Speed;
        public float TurnRate;
    }

    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(NavigatorComponent))]
    [RequireComponent(typeof(HealthComponent))]
    internal class TankMoverComponent : Component<TankMoverComponentData>, IUpdate
    {
        private TransformComponent m_transform;
        private NavigatorComponent m_navigator;
        private HealthComponent m_health;
        private TankMoverComponentData m_properties;

        private enum State
        {
            Idle,
            Moving
        }
        private State m_state;

        protected override void OnInit(in TankMoverComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            m_navigator = Entity.GetComponent<NavigatorComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_properties = properties;
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            // Do nothing if dead
            if (m_health.IsDead)
            {
                m_transform.LocalVelocity = Vector3.Zero;
                m_transform.LocalAngularVelocity = Vector3.Zero;
                return;
            }

            var nextWaypoint = m_navigator.GetNextWaypointOnRoute();
            var transform = m_transform.LocalTransform;
            var velocity = Vector3.Zero;
            var angularVelocity = Vector3.Zero;
            switch (m_state)
            {
                case State.Idle:
                    {
                        if(nextWaypoint != null)
                        {
                            m_state = State.Moving;
                        }
                        break;
                    }
                case State.Moving:
                    {
                        if (nextWaypoint == null)
                        {
                            m_state = State.Idle;
                            break;
                        }

                        // Turn to face
                        var fwd = transform.Forward;
                        var yaw = Mathf.ATan2(fwd.X, fwd.Z);

                        var targetFwd = nextWaypoint.Data.Position - transform.Position;
                        var targetYaw = Mathf.ATan2(targetFwd.X, targetFwd.Z);

                        var angleDiff = Mathf.AngleDiff(targetYaw, yaw);
                        var turnSpeed = Mathf.Sign(angleDiff) * m_properties.TurnRate * Mathf.DEGREES_TO_RADIANS;
                        var turnThisFrame = turnSpeed * dt;

                        bool facing = false;
                        if (Mathf.Abs(turnThisFrame) >= Mathf.Abs(angleDiff))
                        {
                            m_state = State.Moving;
                            turnThisFrame = angleDiff;
                            facing = true;
                        }
                        else
                        {
                            angularVelocity = turnSpeed * Vector3.YAxis;
                        }
                        transform = Matrix4.CreateRotationY(turnThisFrame) * transform;

                        // Move (if facing)
                        if (facing)
                        {
                            if (m_navigator.GetCurrentWaypoint() != null)
                            {
                                m_navigator.SetDepartedCurrentWaypoint();
                            }

                            var pos = transform.Position;
                            var targetPos = nextWaypoint.Data.Position;
                            var distanceRemaining = (targetPos - pos).WithY(0.0f).Length;
                            var movementSpeed = m_properties.Speed;
                            var movementThisFrame = movementSpeed * dt;
                            if (movementThisFrame >= distanceRemaining)
                            {
                                m_state = State.Idle;
                                m_navigator.SetArrivedAtNextWaypoint();
                                transform.Position = targetPos.WithY(pos.Y);
                                velocity = Vector3.Zero;
                            }
                            else
                            {
                                var moveDirection = (targetPos - pos).WithY(0.0f).Normalise();
                                transform.Position += moveDirection * movementThisFrame;
                                velocity = moveDirection * movementSpeed;
                            }
                        }

                        // Set transform
                        m_transform.LocalVelocity = velocity;
                        m_transform.LocalAngularVelocity = angularVelocity;
                        m_transform.LocalTransform = transform;
                        break;
                    }
            }
        }
    }
}
