using System;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal class PhysicsCylinder : PhysicsShape
	{
		public float Length
		{
			get
			{
				float radius = 0.0f;
				float length = 0.0f;
				Ode.dGeomCylinderGetParams(m_geom, ref radius, ref length);
				return length;
			}
		}

		public float Radius
		{
			get
			{
				float radius = 0.0f;
				float length = 0.0f;
				Ode.dGeomCylinderGetParams(m_geom, ref radius, ref length);
				return radius;
			}
		}

		private static Ode.dGeomID MakeGeom(PhysicsWorld world, float length, float radius)
		{
			return Ode.dCreateCylinder(world.m_space, radius, length);
		}

		private static Ode.dMass MakeMass(float length, float radius)
		{
			var m = new Ode.dMass();
			Ode.dMassSetCylinder(ref m, 1.0f, 3, radius, length);
			return m;
		}

		internal PhysicsCylinder(PhysicsWorld world, ref Matrix4 transform, float length, float radius) :
			base(world, ref transform, MakeGeom(world, length, radius), MakeMass(length, radius) )
		{
		}
	}
}
