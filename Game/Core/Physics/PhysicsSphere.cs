using System;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal class PhysicsSphere : PhysicsShape
	{
		public float Radius
		{
			get
			{
				return Ode.dGeomSphereGetRadius(m_geom);
			}
		}

		private static Ode.dGeomID MakeGeom(PhysicsWorld world, float radius)
		{
			return Ode.dCreateSphere(world.m_space, radius);
		}

		private static Ode.dMass MakeMass(float radius)
		{
			var m = new Ode.dMass();
			Ode.dMassSetSphere(ref m, 1.0f, radius);
			return m;
		}

		internal PhysicsSphere(PhysicsWorld world, ref Matrix4 transform, float radius) :
			base(world, ref transform, MakeGeom(world, radius), MakeMass(radius) )
		{
		}
	}
}
