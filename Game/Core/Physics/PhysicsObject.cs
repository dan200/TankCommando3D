using Dan200.Core.Math;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using Ode = ODE.ODE;
using Dan200.Core.Main;
using System.Collections;

namespace Dan200.Core.Physics
{
    internal unsafe struct ContactEventArgs
    {
        public readonly PhysicsObject HitObject;
        private readonly bool* m_pIgnoreContacts;

        public bool IgnoreContacts
        {
            get
            {
                App.Assert(m_pIgnoreContacts != null);
                return *m_pIgnoreContacts;
            }
            set
            {
                App.Assert(m_pIgnoreContacts != null);
                *m_pIgnoreContacts = value;
            }
        }

        public ContactEventArgs(PhysicsObject hitObject, bool* pIgnoreContacts)
        {
            HitObject = hitObject;
            m_pIgnoreContacts = pIgnoreContacts;
        }
    }

    internal class PhysicsObject : IDisposable
    {
        private PhysicsWorld m_world;
        private PhysicsMaterial m_material;
        private Ode.dBodyID m_body; // The body, zero if static
		private HashSet<PhysicsShape> m_shapes;
		private float m_totalVolume;
        private Vector3 m_centerOfMassOffset; // The offset in local space between the object position and the body position, unused if static
        private Matrix4 m_staticTransform; // The transform of the object if static, unused if dynamic.
        private Ode.dMass m_staticMass; // The mass of the object if static, unused if dynamic.
        internal Vector3 m_kinematicVelocity; // The virtual velocity used during collisions for a kinematic object
        internal Vector3 m_kinematicAngularVelocity;
		private bool m_ignoreCollision;
        private object m_userData;

        public event StructEventHandler<PhysicsObject, ContactEventArgs> OnContact;

        public PhysicsWorld World
        {
            get
            {
                return m_world;
            }
        }

		internal struct ShapeCollection : IReadOnlyCollection<PhysicsShape>
		{
			private PhysicsObject m_owner;

			public int Count
			{
				get
				{
					return m_owner.m_shapes.Count;
				}
			}

			internal ShapeCollection(PhysicsObject owner)
			{
				m_owner = owner;
			}

			public HashSet<PhysicsShape>.Enumerator GetEnumerator()
			{
				return m_owner.m_shapes.GetEnumerator();
			}

			IEnumerator<PhysicsShape> IEnumerable<PhysicsShape>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public ShapeCollection Shapes
		{
			get
			{
				return new ShapeCollection(this);
			}
		}

		public PhysicsMaterial Material
        {
            get
            {
                return m_material;
            }
            set
            {
                m_material = value;
            }
        }

        public bool Static
        {
            get
            {
                return m_body == Ode.dBodyID.Zero;
            }
        }

        public Vector3 Position
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Calculate the position from the body position and the center of mass
                    unsafe
                    {
                        float* pos = Ode.dBodyGetPosition(m_body);
                        return ODEHelpers.ToVector3(pos) - Rotation.ToWorldDir(m_centerOfMassOffset);
                    }
                }
                else
                {
                    // Get the static position
                    return m_staticTransform.Position;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Set the body position based on the position and the center of mass
                    var pos = value + Rotation.ToWorldDir(m_centerOfMassOffset);
                    Ode.dBodySetPosition(m_body, pos.X, pos.Y, pos.Z);
                }
                else
                {
                    // Set the static position and update geometry positions
                    var delta = value - m_staticTransform.Position;
                    m_staticTransform.Position = value;
                    unsafe
                    {
						foreach(var shape in m_shapes)
                        {
							var geomID = shape.m_geom;
                            float* pos = Ode.dGeomGetPosition(geomID);
                            Ode.dGeomSetPosition(geomID, pos[0] + delta.X, pos[1] + delta.Y, pos[2] + delta.Z);
                        }
                    }
                }
            }
        }

        public Vector3 CenterOfMass
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body position (this is always the center of mass)
                    unsafe
                    {
                        float* pos = Ode.dBodyGetPosition(m_body);
                        return ODEHelpers.ToVector3(pos);
                    }
                }
                else
                {
                    // Get the static center of mass
                    return m_staticTransform.Position + ODEHelpers.ToVector3(ref m_staticMass.c);
                }
            }
        }

		public Vector3 LocalCentreOfMass
		{
			get
			{
				if (m_body != Ode.dBodyID.Zero)
				{
					return m_centerOfMassOffset;
				}
				else
				{
					return ODEHelpers.ToVector3(ref m_staticMass.c);
				}
			}
		}

        public Matrix3 Rotation
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body rotation
                    unsafe
                    {
                        var mat = new Matrix3();
                        float* rot = Ode.dBodyGetRotation(m_body);
                        ODEHelpers.ToMatrix3(rot, ref mat);
                        return mat;
                    }
                }
                else
                {
                    // Get the static rotation
                    return m_staticTransform.Rotation;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Set the body rotation
                    var rot = new Ode.dMatrix3();
                    ODEHelpers.ToDMatrix3(ref value, ref rot);
                    Ode.dBodySetRotation(m_body, ref rot);
                }
                else
                {
                    // Set the static rotation and update geometry transforms
                    var newTransform = m_staticTransform;
                    newTransform.Rotation = value;
                    var difference = m_staticTransform.InvertAffine() * newTransform;
                    m_staticTransform = newTransform;
                    foreach (var shape in m_shapes)
                    {
						var geom = shape.m_geom;
                        unsafe
                        {
                            var mat = new Matrix4();
                            float* pos = Ode.dGeomGetPosition(geom);
                            float* rot = Ode.dGeomGetRotation(geom);
                            ODEHelpers.ToMatrix4(pos, rot, ref mat);
                            mat = mat * difference;
                            var newPos = new Ode.dVector3();
                            var newRot = new Ode.dMatrix3();
                            ODEHelpers.ToDVector3AndDMatrix3(ref mat, ref newPos, ref newRot);
                            Ode.dGeomSetPosition(geom, newPos.v[0], newPos.v[1], newPos.v[2]);
                            Ode.dGeomSetRotation(geom, ref newRot);
                        }
                    }
                }
            }
        }

        public Matrix4 Transform
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Calculate the transform from the body position and the center of mass
                    unsafe
                    {
                        var mat = new Matrix4();
                        float* pos = Ode.dBodyGetPosition(m_body);
                        float* rot = Ode.dBodyGetRotation(m_body);
                        ODEHelpers.ToMatrix4(pos, rot, ref mat);
                        mat.Position -= Rotation.ToWorldDir(m_centerOfMassOffset);
                        return mat;
                    }
                }
                else
                {
                    // Get the static transform
                    return m_staticTransform;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Set the body transform based on the transform and the center of mass
                    unsafe
                    {
                        var mat = value;
                        var pos = new Ode.dVector3();
                        var rot = new Ode.dMatrix3();
                        mat.Position += Rotation.ToWorldDir(m_centerOfMassOffset);
                        ODEHelpers.ToDVector3AndDMatrix3(ref mat, ref pos, ref rot);
                        Ode.dBodySetRotation(m_body, ref rot);
                        Ode.dBodySetPosition(m_body, pos.v[0], pos.v[1], pos.v[2]);
                    }
                }
                else
                {
                    // Set the static rotation and update geometry transforms
                    var difference = m_staticTransform.InvertAffine() * value;
                    m_staticTransform = value;
                    foreach (var shape in m_shapes)
                    {
						var geom = shape.m_geom;
                        unsafe
                        {
                            var mat = new Matrix4();
                            float* pos = Ode.dGeomGetPosition(geom);
                            float* rot = Ode.dGeomGetRotation(geom);
                            ODEHelpers.ToMatrix4(pos, rot, ref mat);
                            mat = mat * difference;
                            var newPos = new Ode.dVector3();
                            var newRot = new Ode.dMatrix3();
                            ODEHelpers.ToDVector3AndDMatrix3(ref mat, ref newPos, ref newRot);
                            Ode.dGeomSetPosition(geom, newPos.v[0], newPos.v[1], newPos.v[2]);
                            Ode.dGeomSetRotation(geom, ref newRot);
                        }
                    }
                }
            }
        }

        public Vector3 Velocity
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body velocity
                    int kinematic = Ode.dBodyIsKinematic(m_body);
                    if (kinematic != 0)
                    {
                        return m_kinematicVelocity;
                    }
                    else
                    {
                        unsafe
                        {
                            float* vel = Ode.dBodyGetLinearVel(m_body);
                            return ODEHelpers.ToVector3(vel);
                        }
                    }
                }
                else
                {
                    // Get the static velocity
                    return Vector3.Zero;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Set the body velocity
                    int kinematic = Ode.dBodyIsKinematic(m_body);
                    if (kinematic != 0)
                    {
                        m_kinematicVelocity = value;
                        if (m_kinematicVelocity.LengthSquared > 0.0f || m_kinematicAngularVelocity.LengthSquared > 0.0f)
                        {
                            Ode.dBodyEnable(m_body);
                        }
                        else
                        {
                            Ode.dBodyDisable(m_body);
                        }
                    }
                    else
                    {
                        Ode.dBodySetLinearVel(m_body, value.X, value.Y, value.Z);
                        if (value.LengthSquared > 0.0f)
                        {
                            Ode.dBodyEnable(m_body);
                        }
                    }
                }
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body angular velocity
                    int kinematic = Ode.dBodyIsKinematic(m_body);
                    if (kinematic != 0)
                    {
                        return m_kinematicAngularVelocity;
                    }
                    else
                    {
                        unsafe
                        {
                            float* vel = Ode.dBodyGetAngularVel(m_body);
                            return ODEHelpers.ToVector3(vel);
                        }
                    }
                }
                else
                {
                    // Get the static velocity
                    return Vector3.Zero;
                }
            }
            set
            {
                // Set the body angular velocity
                if (m_body != Ode.dBodyID.Zero)
                {
                    int kinematic = Ode.dBodyIsKinematic(m_body);
                    if (kinematic != 0)
                    {
                        m_kinematicAngularVelocity = value;
                        if (m_kinematicVelocity.LengthSquared > 0.0f || m_kinematicAngularVelocity.LengthSquared > 0.0f)
                        {
                            Ode.dBodyEnable(m_body);
                        }
                        else
                        {
                            Ode.dBodyDisable(m_body);
                        }
                    }
                    else
                    {
                        Ode.dBodySetAngularVel(m_body, value.X, value.Y, value.Z);
                        if (value.LengthSquared > 0.0f)
                        {
                            Ode.dBodyEnable(m_body);
                        }
                    }
                }
            }
        }

        public float Mass
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body mass
                    var mass = new Ode.dMass();
                    Ode.dBodyGetMass(m_body, ref mass);
                    return mass.mass;
                }
                else
                {
                    // Get the static mass
                    return m_staticMass.mass;
                }
            }
            set
            {
				App.Assert(value > 0.0f);
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Modify the body mass
                    var mass = new Ode.dMass();
                    Ode.dBodyGetMass(m_body, ref mass);
                    Ode.dMassAdjust(ref mass, value);

                    int wasKinematic = Ode.dBodyIsKinematic(m_body);
                    Ode.dBodySetMass(m_body, ref mass);
                    if (wasKinematic != 0)
                    {
                        Ode.dBodySetKinematic(m_body);
                    }
                }
                else
                {
                    // Modify the static mass
                    Ode.dMassAdjust(ref m_staticMass, value);
                }
            }
        }

        public Matrix3 AngularMass
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the body angular mass
                    var mat = new Matrix3();
                    var mass = new Ode.dMass();
                    Ode.dBodyGetMass(m_body, ref mass);
                    ODEHelpers.ToMatrix3(ref mass.I, ref mat);
                    return mat;
                }
                else
                {
                    // Get the static angular mass
                    var mat = new Matrix3();
                    ODEHelpers.ToMatrix3(ref m_staticMass.I, ref mat);
                    return mat;
                }
            }
        }

        public bool IgnoreGravity
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    return Ode.dBodyGetGravityMode(m_body) == 0;
                }
                else
                {
                    return true;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    Ode.dBodySetGravityMode(m_body, value ? 0 : 1);
                }
            }
        }

        public bool Awake
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    return Ode.dBodyIsEnabled(m_body) != 0;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    if (value)
                    {
                        Ode.dBodyEnable(m_body);
                    }
                    else
                    {
                        Ode.dBodyDisable(m_body);
                    }
                }
            }
        }

        public bool AutoSleep
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    return Ode.dBodyGetAutoDisableFlag(m_body) != 0;
                }
                else
                {
                    return true;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    Ode.dBodySetAutoDisableFlag(m_body, value ? 1 : 0);
                }
            }
        }

        public bool Kinematic
        {
            get
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Get the kinematic state of a body
                    return Ode.dBodyIsKinematic(m_body) != 0;
                }
                else
                {
                    // Static bodies are kinematic I guess
                    return true;
                }
            }
            set
            {
                if (m_body != Ode.dBodyID.Zero)
                {
                    // Set the kinematic state of the body, switching between virtual and real velocity if necessary
                    int wasKinematic = Ode.dBodyIsKinematic(m_body);
                    if (value)
                    {
                        if (wasKinematic == 0)
                        {
                            Ode.dBodySetKinematic(m_body);
                            unsafe
                            {
                                float* vel = Ode.dBodyGetLinearVel(m_body);
                                float* angVel = Ode.dBodyGetAngularVel(m_body);
                                m_kinematicVelocity = ODEHelpers.ToVector3(vel);
                                m_kinematicAngularVelocity = ODEHelpers.ToVector3(angVel);
                            }
                            Ode.dBodySetLinearVel(m_body, 0.0f, 0.0f, 0.0f);
                            Ode.dBodySetAngularVel(m_body, 0.0f, 0.0f, 0.0f);

                            Ode.dBodySetAutoDisableFlag(m_body, 0);
                            if (m_kinematicVelocity.LengthSquared > 0.0f || m_kinematicAngularVelocity.LengthSquared > 0.0f)
                            {
                                Ode.dBodyEnable(m_body);
                            }
                            else
                            {
                                Ode.dBodyDisable(m_body);
                            }
                        }
                    }
                    else
                    {
                        if (wasKinematic != 0)
                        {
                            Ode.dBodySetDynamic(m_body);
                            Ode.dBodySetLinearVel(m_body, m_kinematicVelocity.X, m_kinematicVelocity.Y, m_kinematicVelocity.Z);
                            Ode.dBodySetAngularVel(m_body, m_kinematicAngularVelocity.X, m_kinematicAngularVelocity.Y, m_kinematicAngularVelocity.Z);
                            Ode.dBodySetAutoDisableDefaults(m_body);
                        }
                    }
                }
            }
        }

		public bool IgnoreCollision
		{
			get
			{
				return m_ignoreCollision;
			}
			set
			{
				if (m_ignoreCollision != value)
				{
					m_ignoreCollision = value;
					if (m_ignoreCollision)
					{
						foreach (var shape in m_shapes)
						{
							var geom = shape.m_geom;
							Ode.dGeomSetCategoryBits(geom, 0);
							Ode.dGeomSetCollideBits(geom, 0);
						}
					}
					else
					{
						foreach (var shape in m_shapes)
						{
							var geom = shape.m_geom;
							Ode.dGeomSetCategoryBits(geom, (uint)shape.Group);
							Ode.dGeomSetCollideBits(geom, (uint)shape.Group.GetColliders());
						}
					}
				}
			}
		}

        public object UserData
        {
            get
            {
                return m_userData;
            }
            set
            {
                m_userData = value;
            }
        }

		internal PhysicsObject(PhysicsWorld world, PhysicsMaterial material, bool _static)
        {
            // Set common parameters
            m_world = world;
			m_shapes = new HashSet<PhysicsShape>();
			m_totalVolume = 0.0f;
            m_centerOfMassOffset = Vector3.Zero;
			m_material = material;
            m_userData = null;

            // Create a sensible default mass
            var mass = new Ode.dMass();
            Ode.dMassSetSphereTotal(ref mass, 1.0f, 1.0f);
            if (_static)
            {
                // Setup a static object
                m_staticTransform = Matrix4.Identity;
                m_staticMass = mass;
            }
            else
            {
                // Setup a dynamic object
                m_body = Ode.dBodyCreate(m_world.m_world);
                Ode.dBodySetMass(m_body, ref mass);
            }
			m_ignoreCollision = false;
        }

        public void Dispose()
        {
            App.Assert(m_shapes.Count == 0);
            if (m_body != Ode.dBodyID.Zero)
            {
                Ode.dBodyDestroy(m_body);
            }
        }

		public void AddShape(PhysicsShape shape)
		{
			App.Assert(!m_shapes.Contains(shape));
			App.Assert(shape.World == World);
			App.Assert(shape.Object == null);

			// Add the geom
			var geom = shape.m_geom;
			if (m_ignoreCollision)
			{
				Ode.dGeomSetCategoryBits(geom, 0);
				Ode.dGeomSetCollideBits(geom, 0);
			}
			else
			{
				Ode.dGeomSetCategoryBits(geom, (uint)shape.Group);
				Ode.dGeomSetCollideBits(geom, (uint)shape.Group.GetColliders());
			}
            if (m_body != Ode.dBodyID.Zero)
            {
                Ode.dGeomSetBody(geom, m_body);
            }
			m_shapes.Add(shape);
			shape.Object = this;

			// Transform the geometry
			var mass = shape.m_mass;
			unsafe
            {
				var transform = shape.Transform;
                var pos = new Ode.dVector3();
				var rot = new Ode.dMatrix3();
                ODEHelpers.ToDVector3AndDMatrix3(ref transform, ref pos, ref rot);
				pos.v[0] -= m_centerOfMassOffset.X;
				pos.v[1] -= m_centerOfMassOffset.Y;
				pos.v[2] -= m_centerOfMassOffset.Z;

                // Mass
                Ode.dMassRotate(ref mass, ref rot);
                Ode.dMassTranslate(ref mass, pos.v[0], pos.v[1], pos.v[2]);

                // Geometry
                if (m_body != Ode.dBodyID.Zero)
                {
                    Ode.dGeomSetOffsetRotation(geom, ref rot);
                    Ode.dGeomSetOffsetPosition(geom, pos.v[0], pos.v[1], pos.v[2]);
                }
                else
                {
					var worldTransform = m_staticTransform.ToWorld(transform);
					ODEHelpers.ToDVector3AndDMatrix3(ref worldTransform, ref pos, ref rot);
                    Ode.dGeomSetRotation(geom, ref rot);
                    Ode.dGeomSetPosition(geom, pos.v[0], pos.v[1], pos.v[2]);
                }
            }

			// Generate the new mass distribution
			if (m_body != Ode.dBodyID.Zero)
			{
				var existingMass = new Ode.dMass();
				Ode.dBodyGetMass(m_body, ref existingMass);
				if (m_shapes.Count > 1)
				{
					var oldMass = existingMass.mass;
					Ode.dMassAdjust(ref existingMass, m_totalVolume);
					Ode.dMassAdd(ref mass, ref existingMass);
					Ode.dMassAdjust(ref mass, oldMass);
				}
				else
				{
					Ode.dMassAdjust(ref mass, existingMass.mass);
				}
			}
			else
			{
				if (m_shapes.Count > 1)
				{
					var oldMass = m_staticMass.mass;
					Ode.dMassAdjust(ref m_staticMass, m_totalVolume);
					Ode.dMassAdd(ref mass, ref m_staticMass);
					Ode.dMassAdjust(ref mass, oldMass);
				}
				else
				{
					Ode.dMassAdjust(ref mass, m_staticMass.mass);
				}
            }

			// Update the mass
            if (m_body != Ode.dBodyID.Zero)
            {
                // Move things around so the body is still centered at the center of the mass (ODE requires this)
				unsafe
				{
					float* com = mass.c.v;
					if (com[0] != 0.0f || com[1] != 0.0f || com[2] != 0.0f)
					{
						var bodyMat = new Matrix3();
						float* bodyPos = Ode.dBodyGetPosition(m_body);
						float* bodyRot = Ode.dBodyGetRotation(m_body);
						ODEHelpers.ToMatrix3(bodyRot, ref bodyMat);
						var comWorld = bodyMat.ToWorldDir(ODEHelpers.ToVector3(com));
						Ode.dBodySetPosition(
							m_body,
						    bodyPos[0] + comWorld.X,
							bodyPos[1] + comWorld.Y,
							bodyPos[2] + comWorld.Z
						);
						foreach (var otherShape in m_shapes)
						{
							var otherGeom = otherShape.m_geom;
							float* otherPos = Ode.dGeomGetOffsetPosition(otherGeom);
							Ode.dGeomSetOffsetPosition(otherGeom, otherPos[0] - com[0], otherPos[1] - com[1], otherPos[2] - com[2]);
						}
						m_centerOfMassOffset.X += com[0];
						m_centerOfMassOffset.Y += com[1];
						m_centerOfMassOffset.Z += com[2];
						com[0] = 0.0f;
						com[1] = 0.0f;
						com[2] = 0.0f;
					}

					// Set the mass
					int wasKinematic = Ode.dBodyIsKinematic(m_body);
					Ode.dBodySetMass(m_body, ref mass);
					if (wasKinematic != 0)
					{
						Ode.dBodySetKinematic(m_body);
					}
				}
            }
            else
            {
                // Set the mass
                m_staticMass = mass;
            }

			// Update the volume
			m_totalVolume += shape.Volume;
		}

		public void AddShapes(List<PhysicsShape> shapes)
		{
			// TODO: Optimise
			foreach(PhysicsShape shape in shapes)
			{
				AddShape(shape);
			}
		}

		private void RemoveShape_Remove(PhysicsShape shape)
		{
			App.Assert(m_shapes.Contains(shape));
			App.Assert(shape.Object == this);

			var geom = shape.m_geom;
			m_shapes.Remove(shape);
			shape.Object = null;
			Ode.dGeomSetBody(geom, Ode.dBodyID.Zero);
			Ode.dGeomSetCategoryBits(geom, 0);
			Ode.dGeomSetCollideBits(geom, 0);
		}

		private void RemoveShape_UpdateMass()
		{
			var mass = new Ode.dMass();
			var volume = 0.0f;
			if (m_shapes.Count > 0)
			{
				Ode.dMassSetZero(ref mass);
				foreach(var shape in m_shapes)
				{
					var shapeMass = shape.m_mass;
					var shapeTransform = shape.Transform;
					var pos = new Ode.dVector3();
					var rot = new Ode.dMatrix3();
					ODEHelpers.ToDVector3AndDMatrix3(ref shapeTransform, ref pos, ref rot);
					unsafe
					{
						pos.v[0] -= m_centerOfMassOffset.X;
						pos.v[1] -= m_centerOfMassOffset.Y;
						pos.v[2] -= m_centerOfMassOffset.Z;
						Ode.dMassRotate(ref shapeMass, ref rot);
						Ode.dMassTranslate(ref shapeMass, pos.v[0], pos.v[1], pos.v[2]);
					}
					Ode.dMassAdd(ref mass, ref shapeMass);
					volume += shape.Volume;
				}
			}
			else
			{
				Ode.dMassSetSphereTotal(ref mass, 1.0f, 1.0f);
			}
			App.Assert(mass.mass > 0.0f);

			// Update the mass
			if (m_body != Ode.dBodyID.Zero)
			{
				// Rescale the new mass
				var existingMass = new Ode.dMass();
				Ode.dBodyGetMass(m_body, ref existingMass);
				Ode.dMassAdjust(ref mass, existingMass.mass);

				// Move things around so the body is still centered at the center of the mass (ODE requires this)
				unsafe
				{
					float* com = mass.c.v;
					if (com[0] != 0.0f || com[1] != 0.0f || com[2] != 0.0f)
					{
						var bodyMat = new Matrix3();
						float* bodyPos = Ode.dBodyGetPosition(m_body);
						float* bodyRot = Ode.dBodyGetRotation(m_body);
						ODEHelpers.ToMatrix3(bodyRot, ref bodyMat);
						var comWorld = bodyMat.ToWorldDir(ODEHelpers.ToVector3(com));
						Ode.dBodySetPosition(
							m_body,
							bodyPos[0] + comWorld.X,
							bodyPos[1] + comWorld.Y,
							bodyPos[2] + comWorld.Z
						);
						foreach (var otherShape in m_shapes)
						{
							var otherGeom = otherShape.m_geom;
							float* otherPos = Ode.dGeomGetOffsetPosition(otherGeom);
							Ode.dGeomSetOffsetPosition(otherGeom, otherPos[0] - com[0], otherPos[1] - com[1], otherPos[2] - com[2]);
						}
						m_centerOfMassOffset.X += com[0];
						m_centerOfMassOffset.Y += com[1];
						m_centerOfMassOffset.Z += com[2];
						com[0] = 0.0f;
						com[1] = 0.0f;
						com[2] = 0.0f;
					}
				}

				// Set the mass
				int wasKinematic = Ode.dBodyIsKinematic(m_body);
				App.Assert(mass.mass > 0.0f);
				Ode.dBodySetMass(m_body, ref mass);
				if (wasKinematic != 0)
				{
					Ode.dBodySetKinematic(m_body);
				}
			}
			else
			{
				// Rescale the new mass
				Ode.dMassAdjust(ref mass, m_staticMass.mass);

				// Set the new mass
				App.Assert(mass.mass > 0.0f);
				m_staticMass = mass;
			}
			m_totalVolume = volume;
		}

		public void RemoveShape(PhysicsShape shape)
		{
            App.Assert(shape != null);
			RemoveShape_Remove(shape);
			RemoveShape_UpdateMass();
		}

		public void RemoveShapes(List<PhysicsShape> shapes)
		{
			if (shapes.Count > 0)
			{
				foreach (var shape in shapes)
				{
					RemoveShape_Remove(shape);
				}
				RemoveShape_UpdateMass();
			}
		}

        public void ClearShapes()
        {
			// Early out if there's nothing to do
			if (m_shapes.Count == 0)
			{
				return;
			}

			// Remove the shapes
            foreach (var shape in m_shapes)
            {
				var geom = shape.m_geom;
				shape.Object = null;
				Ode.dGeomSetBody(geom, Ode.dBodyID.Zero);
				Ode.dGeomSetCategoryBits(geom, 0);
				Ode.dGeomSetCollideBits(geom, 0);
				Ode.dGeomSetBody(geom, Ode.dBodyID.Zero);
			}
            m_shapes.Clear();

			// Reset to a sensible default mass and volume
			if (m_body != Ode.dBodyID.Zero)
			{
				var existingMass = new Ode.dMass();
				Ode.dBodyGetMass(m_body, ref existingMass);
				Ode.dMassSetSphereTotal(ref existingMass, existingMass.mass, 1.0f);
				m_centerOfMassOffset = Vector3.Zero;

				int wasKinematic = Ode.dBodyIsKinematic(m_body);
				Ode.dBodySetMass(m_body, ref existingMass);
				if (wasKinematic != 0)
				{
					Ode.dBodySetKinematic(m_body);
				}
			}
			else
			{
				Ode.dMassSetSphereTotal(ref m_staticMass, m_staticMass.mass, 1.0f);
			}
			m_totalVolume = 0.0f;
		}

        public Vector3 GetVelocityAtPosition(Vector3 position)
        {
            if (m_body == Ode.dBodyID.Zero)
            {
                // Static
                return Vector3.Zero;
            }
            else
            {
                unsafe
                {
                    float* pos = Ode.dBodyGetPosition(m_body);
                    position.X -= pos[0];
                    position.Y -= pos[1];
                    position.Z -= pos[2];
                    if (Ode.dBodyIsKinematic(m_body) != 0)
                    {
                        // Kinematic
                        return m_kinematicVelocity + m_kinematicAngularVelocity.Cross(position);
                    }
                    else
                    {
                        // Dynamic
                        var vel = ODEHelpers.ToVector3(Ode.dBodyGetLinearVel(m_body));
                        var angVel = ODEHelpers.ToVector3(Ode.dBodyGetAngularVel(m_body));
                        return vel + angVel.Cross(position);
                    }
                }
            }
        }

        public void ApplyForce(Vector3 force)
        {
            // Apply a force at the center of mass
            if (m_body != Ode.dBodyID.Zero && force.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                Ode.dBodyEnable(m_body);
                Ode.dBodyAddForce(m_body, force.X, force.Y, force.Z);
            }
        }

        public void ApplyForceAtPos(Vector3 force, Vector3 pos)
        {
            // Apply a force at the specified position
            if (m_body != Ode.dBodyID.Zero && force.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                if (force.LengthSquared > 0.0f)
                {
                    Ode.dBodyEnable(m_body);
                    Ode.dBodyAddForceAtPos(m_body, force.X, force.Y, force.Z, pos.X, pos.Y, pos.Z);
                }
            }
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            // Apply an impulse at the center of mass
            if (m_body != Ode.dBodyID.Zero && impulse.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                var force = new Ode.dVector3();
                Ode.dWorldImpulseToForce(m_world.m_world, PhysicsWorld.STEP_TIME, impulse.X, impulse.Y, impulse.Z, ref force);
                unsafe
                {
                    Ode.dBodyEnable(m_body);
                    Ode.dBodyAddForce(m_body, force.v[0], force.v[1], force.v[2]);
                }
            }
        }

        public void ApplyImpulseAtPos(Vector3 impulse, Vector3 pos)
        {
            // Apply an impulse at the specified position
            if (m_body != Ode.dBodyID.Zero && impulse.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                var force = new Ode.dVector3();
                Ode.dWorldImpulseToForce(m_world.m_world, PhysicsWorld.STEP_TIME, impulse.X, impulse.Y, impulse.Z, ref force);
                unsafe
                {
                    Ode.dBodyEnable(m_body);
                    Ode.dBodyAddForceAtPos(m_body, force.v[0], force.v[1], force.v[2], pos.X, pos.Y, pos.Z);
                }
            }
        }

        public void ApplyTorque(Vector3 torque)
        {
            // Apply a torque
            if (m_body != Ode.dBodyID.Zero && torque.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                Ode.dBodyEnable(m_body);
                Ode.dBodyAddTorque(m_body, torque.X, torque.Y, torque.Z);
            }
        }

        public void ApplyTorqueImpulse(Vector3 impulse)
        {
            // Apply a torque impulse
            if (m_body != Ode.dBodyID.Zero && impulse.LengthSquared > 0.0f && Ode.dBodyIsKinematic(m_body) == 0)
            {
                var torque = new Ode.dVector3();
                Ode.dWorldImpulseToForce(m_world.m_world, PhysicsWorld.STEP_TIME, impulse.X, impulse.Y, impulse.Z, ref torque);
                unsafe
                {
                    Ode.dBodyEnable(m_body);
                    Ode.dBodyAddTorque(m_body, torque.v[0], torque.v[1], torque.v[2]);
                }
            }
        }

        internal unsafe bool FireOnContact(PhysicsObject o)
        {
            bool ignoreContacts = false;
            var onContact = OnContact;
            if (onContact != null)
            {
                var args = new ContactEventArgs(o, &ignoreContacts);
                onContact.Invoke(this, args);
            }
            return ignoreContacts;
        }
    }
}
