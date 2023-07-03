using System;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal class PhysicsCapsule : PhysicsShape
	{
		public float Length
		{
			get
			{
				float radius = 0.0f;
				float length = 0.0f;
				Ode.dGeomCapsuleGetParams(m_geom, ref radius, ref length);
				return length;
			}
		}

		public float Radius
		{
			get
			{
				float radius = 0.0f;
				float length = 0.0f;
				Ode.dGeomCapsuleGetParams(m_geom, ref radius, ref length);
				return radius;
			}
		}

		private static Ode.dGeomID MakeGeom(PhysicsWorld world, float length, float radius)
		{
            App.Assert(length > 0.0f);
            App.Assert(radius > 0.0f);
            return Ode.dCreateCapsule(world.m_space, radius, length);
		}

		private static Ode.dMass MakeMass(float length, float radius)
		{
            App.Assert(length > 0.0f);
            App.Assert(radius > 0.0f);
            var m = new Ode.dMass();
			Ode.dMassSetCapsule(ref m, 1.0f, 3, radius, length);
			return m;
		}

		internal PhysicsCapsule(PhysicsWorld world, ref Matrix4 transform, float length, float radius) :
			base(world, ref transform, MakeGeom(world, length, radius), MakeMass(length, radius) )
		{
		}
	}
}
