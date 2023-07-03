using System;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal class PhysicsBox : PhysicsShape
	{
		public Vector3 Size
		{
			get
			{
				var size = new Ode.dVector3();
				Ode.dGeomBoxGetLengths(m_geom, ref size);
				return ODEHelpers.ToVector3(ref size);
			}
		}

		private static Ode.dGeomID MakeGeom(PhysicsWorld world, Vector3 size)
		{
			return Ode.dCreateBox(world.m_space, size.X, size.Y, size.Z);
		}

		private static Ode.dMass MakeMass(Vector3 size)
		{
			var m = new Ode.dMass();
			Ode.dMassSetBox(ref m, 1.0f, size.X, size.Y, size.Z);
			return m;
		}

		internal PhysicsBox(PhysicsWorld world, ref Matrix4 transform, Vector3 size) :
			base(world, ref transform, MakeGeom(world, size), MakeMass(size) )
		{
		}
	}
}
