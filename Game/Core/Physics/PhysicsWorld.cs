using Dan200.Core.Geometry;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
    internal class PhysicsWorld : IDisposable
    {
        public const float STEP_TIME = 1.0f / 50.0f;
		public const float FRICTION_MULTIPLIER = 10.0f;
        public const int MAX_CONTACTS = 8;

        private static Dictionary<IntPtr, PhysicsWorld> s_worldLookup = new Dictionary<IntPtr, PhysicsWorld>(IntPtrComparer.Instance);
		internal static Dictionary<Ode.dGeomID, PhysicsShape> s_geomToShape = new Dictionary<Ode.dGeomID, PhysicsShape>(StructComparer<Ode.dGeomID>.Instance);
        private static Ode.dNearCallback s_nearCallback = new Ode.dNearCallback(NearCallback);
        private static Ode.dNearCallback s_raycastCallback = new Ode.dNearCallback(RaycastCallback);
        private static Ode.dNearCallback s_spherecastCallback = new Ode.dNearCallback(SpherecastCallback);
		private static Ode.dNearCallback s_shapeTestCallback = new Ode.dNearCallback(ShapeTestCallback);

        internal Ode.dWorldID m_world;
        internal Ode.dSpaceID m_space;
        private Ode.dJointGroupID m_contactGroup;
        private float m_unsteppedTime;

        private Ode.dGeomID m_currentRaycastGeom;
        private Ray m_currentRaycastRay;
        private RaycastResult m_currentRaycastResult;

        private Ode.dGeomID m_currentShapeTestGeom;
        private List<Contact> m_currentShapeTestOutput;

        public Vector3 Gravity
        {
            get
            {
               	var gravity = new Ode.dVector3();
                Ode.dWorldGetGravity(m_world, ref gravity);
                return ODEHelpers.ToVector3(ref gravity);
            }
            set
            {
                Ode.dWorldSetGravity(m_world, value.X, value.Y, value.Z);
            }
        }

        public float CurrentStepFraction
        {
            get
            {
                return m_unsteppedTime / STEP_TIME;
            }
        }

        public PhysicsWorld()
        {
            m_world = Ode.dWorldCreate();
            s_worldLookup.Add(m_world.ID, this);

            //m_space = Ode.dSimpleSpaceCreate(Ode.dSpaceID.Zero);
            m_space = Ode.dHashSpaceCreate(Ode.dSpaceID.Zero);
            m_contactGroup = Ode.dJointGroupCreate(0);

            Ode.dWorldSetGravity(m_world, 0.0f, -9.8f, 0.0f);
            Ode.dWorldSetERP(m_world, 0.2f);
            Ode.dWorldSetCFM(m_world, 0.00001f);
            Ode.dWorldSetContactMaxCorrectingVel(m_world, 10.0f);
            Ode.dWorldSetMaxAngularSpeed(m_world, 4.0f * Mathf.PI);
            Ode.dWorldSetContactSurfaceLayer(m_world, 0.005f);

            Ode.dWorldSetLinearDamping(m_world, 0.1f * STEP_TIME);
            Ode.dWorldSetLinearDampingThreshold(m_world, 0.01f);
            Ode.dWorldSetAngularDamping(m_world, 0.3f * STEP_TIME);
            Ode.dWorldSetAngularDampingThreshold(m_world, 0.01f);

            Ode.dWorldSetAutoDisableFlag(m_world, 1);
            Ode.dWorldSetAutoDisableAverageSamplesCount(m_world, 4);
            Ode.dWorldSetAutoDisableLinearThreshold(m_world, 0.03f);
            Ode.dWorldSetAutoDisableAngularThreshold(m_world, 0.03f);
            Ode.dWorldSetAutoDisableTime(m_world, 10 * STEP_TIME);
            Ode.dWorldSetAutoDisableSteps(m_world, 10);
        }

        public void Dispose()
        {
            Ode.dJointGroupDestroy(m_contactGroup);
            Ode.dSpaceDestroy(m_space);
            Ode.dWorldDestroy(m_world);
            s_worldLookup.Remove(m_world.ID);
        }

		public PhysicsObject CreateObject(PhysicsMaterial material)
        {
            return new PhysicsObject(this, material, false);
        }

        public PhysicsObject CreateStaticObject(PhysicsMaterial material)
        {
			return new PhysicsObject(this, material, true);
        }

		public PhysicsBox CreateBox(Matrix4 transform, Vector3 size)
		{
			return new PhysicsBox(this, ref transform, size);
		}

		public PhysicsSphere CreateSphere(Matrix4 transform, float radius)
		{
			return new PhysicsSphere(this, ref transform, radius);
		}

		// Cylinders are oriented along the Z-Axis by default
		public PhysicsCylinder CreateCylinder(Matrix4 transform, float length, float radius)
		{
			return new PhysicsCylinder(this, ref transform, length, radius);
		}

		// Capsules are oriented along the Z-Axis by default
		public PhysicsCapsule CreateCapsule(Matrix4 transform, float length, float radius)
		{
			return new PhysicsCapsule(this, ref transform, length, radius);
		}

		// Hull must contain the origin
		public PhysicsConvexHull CreateConvexHull(Matrix4 transform, ConvexHull hull)
		{
			App.Assert(!hull.Optimise());
			App.Assert(hull.Classify(Vector3.Zero) < 0.0f);
            return PhysicsConvexHull.Create(this, ref transform, ref hull);
		}

        // Advance the physics clock by one frame time, and return how many times Step() should be called
        public int Update(float dt)
        {
            m_unsteppedTime += dt;
            var stepCount = Mathf.Floor(m_unsteppedTime / STEP_TIME);
            m_unsteppedTime -= stepCount * STEP_TIME;
            return (int)stepCount;
        }

        // Update the physics by one physics step, be sure to apply forces before calling this
        public void Step()
        {
            Ode.dSpaceCollide(m_space, m_world.ID, Marshal.GetFunctionPointerForDelegate(s_nearCallback));
            Ode.dWorldQuickStep(m_world, STEP_TIME);
            Ode.dJointGroupEmpty(m_contactGroup);
        }

        public bool Raycast(Ray ray, CollisionGroup groups, out RaycastResult o_result)
        {
            // Create the ray
            var r = Ode.dCreateRay(m_space, ray.Length);
            Ode.dGeomSetCategoryBits(r, (uint)0);
            Ode.dGeomSetCollideBits(r, (uint)groups);
            Ode.dGeomRaySetClosestHit(r, 1);
            Ode.dGeomRaySetFirstContact(r, 0);
            Ode.dGeomRaySetBackfaceCull(r, 1);
            Ode.dGeomRaySet(r, ray.Origin.X, ray.Origin.Y, ray.Origin.Z, ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
            try
            {
                // Perform the test
                m_currentRaycastGeom = r;
                m_currentRaycastRay = ray;
                m_currentRaycastResult = new RaycastResult();
                Ode.dSpaceCollide2(r, new Ode.dGeomID(m_space.ID), m_world.ID, Marshal.GetFunctionPointerForDelegate(s_raycastCallback));
                if (m_currentRaycastResult.Shape != null)
                {
                    //App.DebugDraw.DrawLine(ray.Origin, ray.Origin + ray.Direction * ray.Length, Colour.Green);
                    o_result = m_currentRaycastResult;
                    return true;
                }
                else
                {
                    //App.DebugDraw.DrawLine(ray.Origin, ray.Origin + ray.Direction * ray.Length, Colour.Red);
                    o_result = new RaycastResult();
                    return false;
                }
            }
            finally
            {
                Ode.dGeomDestroy(r);
            }
        }

        public bool SphereCast(Ray ray, float radius, CollisionGroup groups, out RaycastResult o_result)
        {
            // Determine capsule position
            var pos = ray.Origin + ray.Direction * 0.5f * ray.Length;
            Ode.dVector3 fwd, right, up;
            ODEHelpers.ToDVector3(ref ray.Direction, ref fwd);
            Ode.dPlaneSpace(ref fwd, ref right, ref up);
            var rot = new Ode.dMatrix3();
            unsafe
            {
                rot.v[0] = right.v[0];
                rot.v[1] = up.v[0];
                rot.v[2] = fwd.v[0];
                rot.v[3] = 0.0f;
                rot.v[4] = right.v[1];
                rot.v[5] = up.v[1];
                rot.v[6] = fwd.v[1];
                rot.v[7] = 0.0f;
                rot.v[8] = right.v[2];
                rot.v[9] = up.v[2];
                rot.v[10] = fwd.v[2];
                rot.v[11] = 0.0f;
            }

            // Create the capsule
            var c = Ode.dCreateCapsule(m_space, radius, ray.Length);
            Ode.dGeomSetCategoryBits(c, (uint)0);
            Ode.dGeomSetCollideBits(c, (uint)groups);
            Ode.dGeomSetPosition(c, pos.X, pos.Y, pos.Z);
            Ode.dGeomSetRotation(c, ref rot);
            try
            {
                // Perform the test
                m_currentRaycastGeom = c;
                m_currentRaycastRay = ray;
                m_currentRaycastResult = new RaycastResult();
                Ode.dSpaceCollide2(c, new Ode.dGeomID(m_space.ID), m_world.ID, Marshal.GetFunctionPointerForDelegate(s_spherecastCallback));

                var mat = new Matrix4();
                unsafe
                {
                    float* posV = Ode.dGeomGetPosition(c);
                    float* rotV = Ode.dGeomGetRotation(c);
                    ODEHelpers.ToMatrix4(posV, rotV, ref mat);
                }
				if (m_currentRaycastResult.Shape != null)
                {
                    //App.DebugDraw.DrawCapsule(mat, radius, ray.Length, Colour.Green);
                    o_result = m_currentRaycastResult;
                    return true;
                }
                else
                {
                    //App.DebugDraw.DrawCapsule(mat, radius, ray.Length, Colour.Red);
                    o_result = new RaycastResult();
                    return false;
                }
            }
            finally
            {
                Ode.dGeomDestroy(c);
            }
        }

		public int SphereTest(Sphere sphere, CollisionGroup groups, List<Contact> o_contacts)
		{
			// Setup the shape
			var geom = Ode.dCreateSphere(m_space, sphere.Radius);
			Ode.dGeomSetCategoryBits(geom, (uint)0);
			Ode.dGeomSetCollideBits(geom, (uint)groups);
			Ode.dGeomSetPosition(geom, sphere.Center.X, sphere.Center.Y, sphere.Center.Z);
			try
			{
				// Perform the test
				int initialCount = o_contacts.Count;
				m_currentShapeTestGeom = geom;
				m_currentShapeTestOutput = o_contacts;
				Ode.dSpaceCollide2(geom, new Ode.dGeomID(m_space.ID), m_world.ID, Marshal.GetFunctionPointerForDelegate(s_shapeTestCallback));
				m_currentShapeTestOutput = null;

				// Return the results
				/*
				if (o_contacts.Count > initialCount)
				{
					App.DebugDraw.DrawSphere(sphere.Center, sphere.Radius, Colour.Red);
				}
				else
				{
					App.DebugDraw.DrawSphere(sphere.Center, sphere.Radius, Colour.Green);
				}
				*/
				return o_contacts.Count - initialCount;
			}
			finally
			{
				Ode.dGeomDestroy(geom);
			}
		}

		public int ShapeTest(PhysicsShape shape, List<Contact> o_contacts)
        {
			App.Assert(shape.Object == null);

			// Setup the shape
			var geom = shape.m_geom;
			Ode.dGeomSetCategoryBits(geom, (uint)0);
			Ode.dGeomSetCollideBits(geom, (uint)shape.Group.GetColliders());
			try
			{
				// Position the shape
				var transform = shape.Transform;
				var pos = new Ode.dVector3();
				var rot = new Ode.dMatrix3();
				ODEHelpers.ToDVector3AndDMatrix3(ref transform, ref pos, ref rot);
				unsafe
				{
					Ode.dGeomSetPosition(geom, pos.v[0], pos.v[1], pos.v[2]);
					Ode.dGeomSetRotation(geom, ref rot);
				}

				// Perform the test
				int initialCount = o_contacts.Count;
				m_currentShapeTestGeom = geom;
				m_currentShapeTestOutput = o_contacts;
				Ode.dSpaceCollide2(geom, new Ode.dGeomID(m_space.ID), m_world.ID, Marshal.GetFunctionPointerForDelegate(s_shapeTestCallback));
				m_currentShapeTestOutput = null;

				// Return the results
				return o_contacts.Count - initialCount;
			}
			finally
			{
				Ode.dGeomSetCategoryBits(geom, 0);
				Ode.dGeomSetCollideBits(geom, 0);
			}
        }

        public void DebugDraw()
        {
			foreach (var pair in s_geomToShape)
            {
				var geom = pair.Key;
				var shape = pair.Value;
				if (shape.Object == null)
				{
					continue;
				}
                if (Ode.dGeomGetSpace(geom) == m_space)
                {
                    unsafe
                    {
                        float* pos = Ode.dGeomGetPosition(geom);
                        float* rot = Ode.dGeomGetRotation(geom);
                        var body = Ode.dGeomGetBody(geom);
                        Colour colour;
                        if (body == Ode.dBodyID.Zero)
                        {
                            colour = Colour.Yellow;
                        }
                        else if (Ode.dBodyIsKinematic(body) != 0)
                        {
                            if (Ode.dBodyIsEnabled(body) != 0)
                            {
                                colour = Colour.Green;
                            }
                            else
                            {
                                colour = Colour.Cyan;
                            }
                        }
                        else if (Ode.dBodyIsEnabled(body) != 0)
                        {
                            colour = Colour.Red;
                        }
                        else
                        {
                            colour = Colour.Blue;
                        }

                        var mat = new Matrix4();
                        ODEHelpers.ToMatrix4(pos, rot, ref mat);
                        if (body != Ode.dBodyID.Zero)
                        {
                            float* bodyPos = Ode.dBodyGetPosition(body);
                            App.DebugDraw.DrawCross(ODEHelpers.ToVector3(bodyPos), 0.4f, Colour.Magenta);
                        }
                        switch (Ode.dGeomGetClass(geom))
                        {
                            case Ode.dBoxClass:
                                {
                                    var size = new Ode.dVector3();
                                    Ode.dGeomBoxGetLengths(geom, ref size);
                                    App.DebugDraw.DrawBox(mat, size.v[0], size.v[1], size.v[2], colour);
                                    break;
                                }
                            case Ode.dSphereClass:
                                {
                                    var radius = Ode.dGeomSphereGetRadius(geom);
                                    App.DebugDraw.DrawSphere(mat, radius, colour);
                                    break;
                                }
                            case Ode.dCylinderClass:
                                {
                                    float radius = 0.0f;
                                    float length = 0.0f;
                                    Ode.dGeomCylinderGetParams(geom, ref radius, ref length);
                                    App.DebugDraw.DrawCylinder(mat, radius, length, colour);
                                    break;
                                }
                            case Ode.dCapsuleClass:
                                {
                                    float radius = 0.0f;
                                    float length = 0.0f;
                                    Ode.dGeomCapsuleGetParams(geom, ref radius, ref length);
                                    App.DebugDraw.DrawCapsule(mat, radius, length, colour);
                                    break;
                                }
							case Ode.dConvexClass:
								{
									var hull = shape as PhysicsConvexHull;
									if (hull != null)
									{
										App.DebugDraw.DrawHull(mat, hull.Hull, colour);
									}
									break;
								}
                        }
                    }
                }
            }
        }

        private static unsafe void NearCallback(IntPtr data, Ode.dGeomID o1, Ode.dGeomID o2)
        {
            // Get the bodies
            var b1 = Ode.dGeomGetBody(o1);
            var b2 = Ode.dGeomGetBody(o2);
            if (b1 == b2)
            {
                // Both null, or both the same body
                return;
            }

			// Get the objects
			var objA = s_geomToShape[o1].Object;
			var objB = s_geomToShape[o2].Object;
			if (objA == null || objB == null || objA == objB)
			{
				return;
			}
			if (objA.IgnoreCollision || objB.IgnoreCollision)
			{
				// Either object is set to ignore collision
				return;
			}

            // Do collision detection
            var contacts = stackalloc Ode.dContact[MAX_CONTACTS + 1]; // +1 for safety!
            int numContacts = Ode.dCollide(o1, o2, MAX_CONTACTS, ref contacts[0].geom, Marshal.SizeOf(typeof(Ode.dContact)));
            if (numContacts > 0)
            {
                // A contact occured
                bool ignoreA = objA.FireOnContact(objB);
                bool ignoreB = objB.FireOnContact(objA);
                if(ignoreA || ignoreB)
                {
                    return;
                }

                // Don't add contact joints between two kinematic objects
                bool k1 = b1 != Ode.dBodyID.Zero && objA.Kinematic;
                bool k2 = b2 != Ode.dBodyID.Zero && objB.Kinematic;
                if (k1 && k2)
                {
                    return;
                }

                var world = s_worldLookup[data];
                var matA = objA.Material;
                var matB = objB.Material;
				float mu;
				if (k1)
				{
					mu = 0.5f * (matA.Friction + matB.Friction) * objB.Mass;
				}
				else if (k2)
				{
					mu = 0.5f * (matA.Friction + matB.Friction) * objA.Mass;
				}
				else
				{
					mu = 0.5f * (matA.Friction * objA.Mass + matB.Friction * objB.Mass);
				}
                var bounce = 0.5f * (matA.Restitution + matB.Restitution);
				int mode = 0;
				if(bounce > 0.0f)
                {
                    mode |= Ode.dContactBounce;
                }
                for (int i = 0; i < numContacts; ++i)
                {
                    // Create contact joints at the collision location
                    contacts[i].surface.mu = mu * FRICTION_MULTIPLIER;
                    contacts[i].surface.mu2 = mu * FRICTION_MULTIPLIER;
                    contacts[i].surface.bounce = bounce;
                    contacts[i].surface.bounce_vel = 0.1f;
                    contacts[i].surface.mode = mode;
                    if (k1 || k2)
                    {
                        Ode.dVector3 norm = contacts[i].geom.normal;
                        Ode.dVector3 dir1, dir2;
                        Ode.dPlaneSpace(ref norm, ref dir1, ref dir2);

                        var hitPos = ODEHelpers.ToVector3(ref contacts[i].geom.pos);
                        if (k1)
                        {
                            var vel = objA.GetVelocityAtPosition(hitPos);
                            if (vel != Vector3.Zero)
                            {
                                contacts[i].surface.mode |= Ode.dContactFDir1 | Ode.dContactMotion1 | Ode.dContactMotion2 | Ode.dContactMotionN;
                                contacts[i].fdir1 = dir1;
                                contacts[i].surface.motion1 = vel.Dot(ODEHelpers.ToVector3(ref dir1));
                                contacts[i].surface.motion2 = -vel.Dot(ODEHelpers.ToVector3(ref dir2));
                                contacts[i].surface.motionN = -vel.Dot(ODEHelpers.ToVector3(ref norm));
                            }
                        }
                        else
                        {
                            var vel = objB.GetVelocityAtPosition(hitPos);
                            if (vel != Vector3.Zero)
                            {
                                contacts[i].surface.mode |= Ode.dContactFDir1 | Ode.dContactMotion1 | Ode.dContactMotion2 | Ode.dContactMotionN;
                                contacts[i].fdir1 = dir1;
                                contacts[i].surface.motion1 = vel.Dot(ODEHelpers.ToVector3(ref dir1));
                                contacts[i].surface.motion2 = vel.Dot(ODEHelpers.ToVector3(ref dir2));
                                contacts[i].surface.motionN = vel.Dot(ODEHelpers.ToVector3(ref norm));
                            }
                        }
                    }

                    var joint = Ode.dJointCreateContact(world.m_world, world.m_contactGroup, ref contacts[i]);
                    Ode.dJointAttach(joint, b1, b2);

                    /*
                    var jpos = ODEHelpers.ToVector3(ref contacts[i].geom.pos);
                    var jnorm = ODEHelpers.ToVector3(ref contacts[i].geom.normal);
                    App.DebugDraw.DrawCross(jpos, 0.25f, Colour.Black);
                    App.DebugDraw.DrawLine(jpos, jpos + jnorm * 2.0f, Colour.Black);
                    */
                }
            }
        }

        private static unsafe void RaycastCallback(IntPtr data, Ode.dGeomID o1, Ode.dGeomID o2)
        {
            // Determine which object is the ray
            var world = s_worldLookup[data];
            Ode.dGeomID ray, obj;
            if (o1 == world.m_currentRaycastGeom)
            {
                ray = o1;
                obj = o2;
            }
            else
            {
                ray = o2;
                obj = o1;
            }

            // Do collision detection
            var contact = new Ode.dContactGeom();
            if (Ode.dCollide(ray, obj, 1, ref contact, Marshal.SizeOf(typeof(Ode.dContactGeom))) > 0)
            {
				if (world.m_currentRaycastResult.Shape == null ||
                    contact.depth < world.m_currentRaycastResult.Distance)
                {
					var shape = s_geomToShape[obj];
					if (shape.Object != null)
					{
						world.m_currentRaycastResult.Shape = shape;
						world.m_currentRaycastResult.Position = ODEHelpers.ToVector3(ref contact.pos);
						world.m_currentRaycastResult.Distance = contact.depth;
					}
                    //App.DebugDraw.DrawCross(world.m_currentRaycastResult.Position, 0.3f, Colour.Black);
                }
            }
        }

        private static unsafe void SpherecastCallback(IntPtr data, Ode.dGeomID o1, Ode.dGeomID o2)
        {
            // Determine which object is the ray
            var world = s_worldLookup[data];
            Ode.dGeomID ray, obj;
            if (o1 == world.m_currentRaycastGeom)
            {
                ray = o1;
                obj = o2;
            }
            else
            {
                ray = o2;
                obj = o1;
            }

            // Do collision detection
            var contacts = stackalloc Ode.dContactGeom[MAX_CONTACTS];
            int numContacts = Ode.dCollide(ray, obj, MAX_CONTACTS, ref contacts[0], Marshal.SizeOf(typeof(Ode.dContactGeom)));
            for (int i = 0; i < numContacts; ++i)
            {
                var pos = ODEHelpers.ToVector3(ref contacts[i].pos);
                var norm = ODEHelpers.ToVector3(ref contacts[i].normal);
                pos += norm * contacts[i].depth;

                var distance = (pos - world.m_currentRaycastRay.Origin).Dot(world.m_currentRaycastRay.Direction);
				if (world.m_currentRaycastResult.Shape == null ||
                    distance < world.m_currentRaycastResult.Distance)
                {
					var shape = s_geomToShape[obj];
					if (shape.Object != null)
					{
						world.m_currentRaycastResult.Shape = shape;
						world.m_currentRaycastResult.Position = pos;
						world.m_currentRaycastResult.Distance = distance;
						//App.DebugDraw.DrawCross(world.m_currentRaycastResult.Position, 0.3f, Colour.Black);
					}
                }
            }
        }

		private static unsafe void ShapeTestCallback(IntPtr data, Ode.dGeomID o1, Ode.dGeomID o2)
        {
            // Determine which object is the sphere
            var world = s_worldLookup[data];
            Ode.dGeomID ray, obj;
            if (o1 == world.m_currentShapeTestGeom)
            {
                ray = o1;
                obj = o2;
            }
            else
            {
                ray = o2;
                obj = o1;
            }

			// Do collision detectionn
			var shape = s_geomToShape[obj];
			if (shape.Object != null)
			{
				var contacts = stackalloc Ode.dContactGeom[MAX_CONTACTS];
				int numContacts = Ode.dCollide(ray, obj, MAX_CONTACTS, ref contacts[0], Marshal.SizeOf(typeof(Ode.dContactGeom)));
				for (int i = 0; i < numContacts; ++i)
				{
					var contact = new Contact();
					contact.Shape = shape;
					contact.Position = ODEHelpers.ToVector3(ref contacts[i].pos);
					contact.Normal = UnitVector3.ConstructUnsafe(ODEHelpers.ToVector3(ref contacts[i].normal));
					contact.Depth = contacts[i].depth;
					world.m_currentShapeTestOutput.Add(contact);
					//App.DebugDraw.DrawCross(contact.Position, 0.3f, Colour.Black);
				}
			}
        }
    }
}
