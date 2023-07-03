using System;
using System.Collections.Generic;
using Dan200.Core.Geometry;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Game.Level;
using Dan200.Core.Multiplayer;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Physics;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Components;
using Dan200.Core.Lua;
using Dan200.Core.Systems;
using Dan200.Core.Components.Core;
using Dan200.Core.Serialisation;
using Dan200.Game.Components.Weapons;
using Dan200.Core.Main;
using Dan200.Core.Components.Physics;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerMovementComponentData
    {
        // Geometry
        [Range(Min = 0.0)]
        public float Radius;

        [Range(Min = 0.0)]
        public float Height;

        [Range(Min = 0.0)]
        public float CrouchedHeight;

        [Range(Min = 0.0)]
        public float GroundClearance;

        [Range(Min = 0.0)]
        public float EyeHeight;

        // Movement
        [Range(Min = 0.0)]
        public float RunSpeed;

        [Range(Min = 0.0)]
        public float WalkSpeed;

        [Range(Min = 0.0)]
        public float CrouchedSpeed;

        [Range(Min = 0.0)]
        public float CrouchTransitionTime;

        [Range(Min = 0.0)]
        public float Acceleration;

        [Range(Min = 0.0)]
        public float Decceleration;

        [Range(Min = 0.0)]
        public float AirAcceleration;

        [Range(Min = 0.0)]
        public float StrideLength;

        // Jump
        [Range(Min = 0.0)]
        public float GravityScale;

        [Range(Min = 0.0)]
        public float JumpHeight;

        [Range(Min = 0)]
        public int NumDoubleJumps;

        [Range(Min = 0)]
        [Optional(Default = 0.0f)]
        public float FallDamageThreshold;

        [Range(Min = 0)]
        [Optional(Default = 0.0f)]
        public float FallDamageScale;
    }

    [RequireComponentOnAncestor(typeof(PhysicsWorldComponent))]
    [RequireComponent(typeof(TransformComponent))]
	[RequireComponent(typeof(PlayerInputComponent))]
    [RequireComponent(typeof(HealthComponent))]
    [AfterComponent(typeof(PhysicsComponent))]
    internal class PlayerMovementComponent : Component<PlayerMovementComponentData>, IUpdate, IPhysicsUpdate // TODO: Schedule before physics
	{
        private PhysicsWorldComponent m_physics;
        private TransformComponent m_transform;
        private PlayerInputComponent m_input;
        private HealthComponent m_health;
        private PlayerMovementComponentData m_properties;

        private PhysicsObject m_object;
        private PhysicsCapsule m_capsule;
		private float m_pitch;
		private float m_yaw;
        private float m_recoil;
        private Vector3 m_velocity;
        private float m_stride;

        private bool m_grounded;
        private Vector3 m_groundVelocity;
        private float m_groundAngularVelocity;

		private int m_doubleJumpsLeft;
        private float m_crouchFraction;
        private float m_eyeHeightSmoothing;

        private float m_lastEyeHeightSmoothing;
        private Vector3 m_lastPosition;
        private Vector3 m_lastWorldVelocity;
		private bool m_jumpQueued;

        public bool MovementLocked
		{
            get;
            set;
		}

		public Vector3 FeetPos
		{
			get
			{
                return Vector3.Lerp(m_lastPosition, m_object.Position, m_physics.World.CurrentStepFraction);
			}
		}

        public Vector3 EyePos
		{
			get
			{
                var height = Mathf.Lerp( m_properties.Height, m_properties.CrouchedHeight, Mathf.Ease(m_crouchFraction) );
                var eyeHeightSmoothing = Mathf.Lerp(m_lastEyeHeightSmoothing, m_eyeHeightSmoothing, m_physics.World.CurrentStepFraction);
                return FeetPos + (height + (m_properties.EyeHeight - m_properties.Height) + eyeHeightSmoothing) * Vector3.YAxis;
			}
		}

        public float Pitch
		{
			get
			{
                return m_pitch + m_recoil;
			}
		}

        public float Yaw
		{
			get
			{
                return m_yaw;
			}
		}

        public float Stride
        {
            get
            {
                return m_stride;
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                return m_object.AngularVelocity;
            }
        }

        public UnitVector3 EyeLook
		{
			get
			{
				var pitch = Pitch;
				var yaw = Yaw;
				return new Vector3(
					Mathf.Sin(yaw) * Mathf.Cos(pitch),
					Mathf.Sin(pitch),
					Mathf.Cos(yaw) * Mathf.Cos(pitch)
				).SafeNormalise(Vector3.ZAxis);
			}
		}

        public Matrix4 EyeTransform
        {
            get
            {
                var eyePos = EyePos;
                var eyeFwd = EyeLook;
                return Matrix4.CreateLookAt(
                    eyePos,
                    eyePos + eyeFwd,
                    Vector3.YAxis
                );
            }
        }

        public PhysicsObject Object
		{
			get
			{
				return m_object;
			}
		}

        private PhysicsCapsule CreateCapsule(float crouchFraction)
        {
            var height = Mathf.Lerp( m_properties.Height,  m_properties.CrouchedHeight, Mathf.Ease(crouchFraction) );
            App.Assert(height > m_properties.GroundClearance + 2.0f * m_properties.Radius);
            var capsule = m_physics.World.CreateCapsule(
                Matrix4.CreateRotationX(0.5f * Mathf.PI) *
                Matrix4.CreateTranslation(0.0f, m_properties.GroundClearance + 0.5f * (height - m_properties.GroundClearance), 0.0f),
                height - 2.0f * m_properties.Radius - m_properties.GroundClearance,
                m_properties.Radius
            );
            capsule.Group = CollisionGroup.Player;
            capsule.UserData = Entity;
            return capsule;
        }

        protected override void OnInit(in PlayerMovementComponentData properties)
		{
            App.Assert(properties.Height > properties.GroundClearance + 2.0f * properties.Radius);
            App.Assert(properties.CrouchedHeight > properties.GroundClearance + 2.0f * properties.Radius);

            m_physics = Entity.GetComponentOnAncestor<PhysicsWorldComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_input = Entity.GetComponent<PlayerInputComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_properties = properties;

			m_object = m_physics.World.CreateObject(PhysicsMaterial.Default);
			m_object.Kinematic = true;
			m_object.Transform = Matrix4.CreateTranslation( m_transform.Position );
            m_capsule = CreateCapsule(0.0f);
			m_object.AddShape(m_capsule);
            m_object.UserData = Entity;

            m_pitch = 0.0f;
            m_yaw = m_transform.LocalTransform.GetRotationAngles().Y;
            m_recoil = 0.0f;
            m_velocity = Vector3.Zero;
            m_stride = 0.0f;

            m_grounded = false;
            m_groundVelocity = Vector3.Zero;
            m_groundAngularVelocity = 0.0f;

            m_doubleJumpsLeft = 0;
            m_crouchFraction = 0.0f;
            m_eyeHeightSmoothing = 0.0f;

            m_lastPosition = m_object.Position;
            m_lastWorldVelocity = m_object.Velocity;
            m_jumpQueued = false;
        }

        protected override void OnShutdown()
		{
            m_object.ClearShapes();
            m_capsule.Dispose();
            m_object.Dispose();
		}

        public void AddRecoil(float recoil)
        {
            m_recoil += recoil;
        }

        public void PhysicsUpdate(float dt)
        {
            // Store previous state for interpolation
            m_lastPosition = m_object.Position;
            m_lastWorldVelocity = m_object.Velocity;
            m_lastEyeHeightSmoothing = m_eyeHeightSmoothing;

            // Update movement
            UpdateMovement(dt);

            // Smooth eye height
            m_eyeHeightSmoothing = Mathf.ApproachDecay(m_eyeHeightSmoothing, 0.0f, dt, 15.0f);
        }

        private void UpdateLook(float dt)
        {
            // Apply rotation
            m_yaw += m_input.YawDelta;
            m_pitch = Mathf.Clamp(m_pitch + m_input.PitchDelta, Mathf.ToRadians(-89.0f), Mathf.ToRadians(89.0f));
            m_recoil = Mathf.Decay(m_recoil, dt, 4.0f);
        }

        private void UpdateCrouch(float dt)
        {
            // Determine target crouch fraction
            float targetCrouchFraction = (m_input.Crouch || m_health.IsDead) ? 1.0f : 0.0f;
            if (targetCrouchFraction < 1.0f)
            {
                // Check that there's room to stand
                var start = m_object.Position + Vector3.YAxis * (m_properties.GroundClearance + m_properties.Radius);
                var end = m_object.Position + Vector3.YAxis * (m_properties.Height - m_properties.Radius);
                RaycastResult result;
                if (m_physics.World.SphereCast(new Ray(start, end), 0.99f * m_properties.Radius, CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.NPC, out result))
                {
                    targetCrouchFraction = 1.0f;
                }
            }

            // Update the capsule height
            float crouchFraction = Mathf.ApproachLinear(m_crouchFraction, targetCrouchFraction, dt, 1.0f / m_properties.CrouchTransitionTime);
            if (crouchFraction != m_crouchFraction)
            {
                m_crouchFraction = crouchFraction;
                m_object.RemoveShape(m_capsule);
                m_capsule.Dispose();
                m_capsule = CreateCapsule(crouchFraction);
                m_object.AddShape(m_capsule);
            }
        }

        private void UpdateStride(float dt)
        {
            // Update stride
            var lateralSpeed = m_velocity.WithY(0.0f).Length;
            m_stride += (lateralSpeed / m_properties.StrideLength) * dt;
        }

        private void UpdateMovement(float dt)
        {
            // Determine target velocity
            var targetLocalVelocity = Vector3.Zero;
            if (!MovementLocked && !m_health.IsDead)
            {
                float speed = Mathf.Lerp(
                    speed = m_input.Run ? m_properties.RunSpeed : m_properties.WalkSpeed,
                    m_properties.CrouchedSpeed,
                    Mathf.Ease(m_crouchFraction)
                );
                targetLocalVelocity.Z += speed * m_input.Forward;
                targetLocalVelocity.X += speed * m_input.Right;
            }

            // Transform
            var forward = UnitVector3.ConstructUnsafe(Mathf.Sin(m_yaw), 0.0f, Mathf.Cos(m_yaw));
            var rotation = Matrix3.CreateLook(forward, Vector3.YAxis);
            var localVelocity = rotation.ToLocalDir(m_velocity);

            // Determine acceleration
            var localAcceleration = Vector3.Zero;
            if (m_grounded)
            {
                if (Mathf.Abs(targetLocalVelocity.X) >= Mathf.Abs(localVelocity.X) && (targetLocalVelocity.X * localVelocity.X >= 0.0f))
                {
                    localAcceleration.X = m_properties.Acceleration;
                }
                else
                {
                    localAcceleration.X = m_properties.Decceleration;
                }
                if (Mathf.Abs(targetLocalVelocity.Z) >= Mathf.Abs(localVelocity.Z) && (targetLocalVelocity.Z * localVelocity.Z >= 0.0f))
                {
                    localAcceleration.Z = m_properties.Acceleration;
                }
                else
                {
                    localAcceleration.Z = m_properties.Decceleration;
                }
            }
            else
            {
                localAcceleration.X = m_properties.AirAcceleration * Mathf.Abs(m_input.Right);
                localAcceleration.Z = m_properties.AirAcceleration * Mathf.Abs(m_input.Forward);
            }

            // Apply acceleration
            var velocityDiff = targetLocalVelocity - localVelocity;
            var localVelocityChangeThisFrame = localAcceleration * dt;
            if (localVelocityChangeThisFrame.X >= Mathf.Abs(velocityDiff.X))
            {
                localVelocity.X = targetLocalVelocity.X;
            }
            else
            {
                localVelocity.X += Mathf.Sign(velocityDiff.X) * localVelocityChangeThisFrame.X;
            }
            if (localVelocityChangeThisFrame.Z >= Mathf.Abs(velocityDiff.Z))
            {
                localVelocity.Z = targetLocalVelocity.Z;
            }
            else
            {
                localVelocity.Z += Mathf.Sign(velocityDiff.Z) * localVelocityChangeThisFrame.Z;
            }
            var velocity = rotation.ToWorldDir(localVelocity);

			// Apply gravity and jump
			var gravity = m_physics.World.Gravity.Y * m_properties.GravityScale;
			velocity.Y += gravity * dt;
			if(m_jumpQueued)
			{
				if (!MovementLocked && !m_health.IsDead && (m_grounded || m_doubleJumpsLeft > 0))
				{
                    // v2 = u2 + 2as
                    // u = sqrt(-2as)
                    velocity.Y = Mathf.Sqrt(-2.0f * gravity * m_properties.JumpHeight);
					if (m_grounded)
					{
                        m_grounded = false;
                        velocity += m_groundVelocity;
                        m_groundVelocity = Vector3.Zero;
                        m_groundAngularVelocity = 0.0f;
                        m_doubleJumpsLeft = m_properties.NumDoubleJumps;
                    }
                    else
					{
						m_doubleJumpsLeft--;
					}
				}
				m_jumpQueued = false;
			}

            // Move (this will also set the kinematic velocity)
            var delta = (m_groundVelocity + velocity) * dt;
            var previousPos = m_object.Position;
            var previousGrounded = m_grounded;
            var previousGroundVelocity = m_groundVelocity;
            Move(delta, dt);

            // Update velocity
            var newPos = m_object.Position;
            var actualDelta = (newPos - previousPos);
            m_velocity = (actualDelta / dt) - m_groundVelocity;
            if(m_grounded)
            {
                // Grounded
                if (!previousGrounded)
                {
                    // Landed
                    ApplyFallDamage(velocity);
                    previousGroundVelocity = m_groundVelocity;
                }
                m_velocity = ((actualDelta / dt) - previousGroundVelocity).WithY(0.0f);
                m_yaw += m_groundAngularVelocity * dt;
                m_eyeHeightSmoothing -= actualDelta.Y;
            }
            else
            {
                // Airborne
                m_velocity = (actualDelta / dt);
            }
		}

        private void ApplyFallDamage(Vector3 impactVelocity)
        {
            if (impactVelocity.Y > m_properties.FallDamageThreshold && m_properties.FallDamageScale > 0.0)
            {
                var damage = new Damage();
                damage.Type = DamageType.Fall;
                damage.Ammount = (impactVelocity.Y - m_properties.FallDamageThreshold) * m_properties.FallDamageScale;
                damage.Origin = Entity;
                damage.Position = FeetPos;
                damage.Direction = Vector3.YAxis;
                m_health.ApplyDamage(damage);
            }
        }

        public void Update(float dt)
		{
            // Update movement
            UpdateLook(dt);
            UpdateCrouch(dt);
            UpdateStride(dt);
            if (m_input.Jump)
            {
                m_jumpQueued = true;
            }

            // Apply transform
            m_transform.LocalTransform = Matrix4.CreateTranslationScaleRotation(
				FeetPos,
                Vector3.One,
                new Vector3(0.0f, Yaw, 0.0f)
			);
            m_transform.LocalVelocity = m_groundVelocity + m_velocity;
		}

		private List<Contact> m_moveContacts = new List<Contact>();

		private void Move(Vector3 delta, float dt)
		{
            App.Assert(dt > 0.0f);

			// Determine new position
            var oldPos = m_object.Position;
			var newPos = oldPos + delta;

            // Find the floor
            var rayStart = 0.5f * m_properties.Height;
			var rayLength = rayStart;
            if(m_grounded)
            {
                // Hug the ground
                rayLength += m_properties.GroundClearance;
            }

			RaycastResult result;
			if (m_physics.World.Raycast(
				new Ray(newPos + rayStart * Vector3.YAxis, -Vector3.YAxis, rayLength),
				CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.NPC,
				out result))
			{
                // Grounded
                result.Shape.Object.Awake = true;
                m_grounded = true;
                m_groundVelocity = result.Shape.Object.GetVelocityAtPosition(result.Position);
                m_groundAngularVelocity = result.Shape.Object.AngularVelocity.Y;
                newPos.Y = result.Position.Y;
            }
            else
			{
                // Airborne
                m_grounded = false;
                m_groundVelocity = Vector3.Zero;
                m_groundAngularVelocity = 0.0f;
            }

            // Detect sideways motion
            m_moveContacts.Clear();
            var height = Mathf.Lerp(m_properties.Height, m_properties.CrouchedHeight, Mathf.Ease(m_crouchFraction));
            m_physics.World.SphereTest(
                new Sphere(newPos + (height - m_properties.Radius) * Vector3.YAxis, m_properties.Radius),
				CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.NPC,
				m_moveContacts
			);
            m_physics.World.SphereTest(
				new Sphere(newPos + (m_properties.GroundClearance + m_properties.Radius) * Vector3.YAxis, m_properties.Radius),
				CollisionGroup.Environment | CollisionGroup.Prop | CollisionGroup.NPC,
				m_moveContacts
			);
			var adjustment = Vector3.Zero;
			foreach (var contact in m_moveContacts)
			{
				var push = contact.Normal * contact.Depth;
				if (Math.Abs(adjustment.X) < Math.Abs(push.X))
				{
					adjustment.X = push.X;
				}
				if (Math.Abs(adjustment.Z) < Math.Abs(push.Z))
				{
					adjustment.Z = push.Z;
				}
                if (Math.Abs(adjustment.Y) < Math.Abs(push.Y))
                {
                    adjustment.Y = push.Y;
                }
                if (delta.LengthSquared > 0.0f)
                {
                    contact.Shape.Object.Awake = true;
                }
			}
            var adjustedPos = newPos + adjustment;
            if (m_grounded)
            {
                adjustedPos.Y = Mathf.Max(adjustedPos.Y, newPos.Y);
            }
            newPos = adjustedPos;

            // Apply the motion
			m_object.Transform = Matrix4.CreateTranslation(newPos);
            m_object.Velocity = (newPos - oldPos) / dt;
		}
	}
}
