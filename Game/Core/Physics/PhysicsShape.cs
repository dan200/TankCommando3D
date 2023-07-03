using System;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal class PhysicsShape : IDisposable
	{
		private PhysicsWorld m_world;
		private PhysicsObject m_object;
		private Matrix4 m_transform;
		internal Ode.dGeomID m_geom;
		internal Ode.dMass m_mass;
		private CollisionGroup m_group;
		private object m_userData;

		public PhysicsWorld World
		{
			get
			{
				return m_world;
			}
		}

		public PhysicsObject Object
		{
			get
			{
				return m_object;
			}
			internal set
			{
				m_object = value;
			}
		}

		public Matrix4 Transform
		{
			get
			{
				return m_transform;
			}
			set
			{
				m_transform = value;
			}
		}

		public float Volume
		{
			get
			{
				return m_mass.mass;
			}
		}

		public CollisionGroup Group
		{
			get
			{
				return m_group;
			}
			set
			{
				if (m_group != value)
				{
					m_group = value;
					if (m_object != null && !m_object.IgnoreCollision)
					{
						Ode.dGeomSetCategoryBits(m_geom, (uint)m_group);
						Ode.dGeomSetCollideBits(m_geom, (uint)m_group.GetColliders());
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

		internal PhysicsShape(PhysicsWorld world, ref Matrix4 transform, Ode.dGeomID geom, Ode.dMass mass)
		{
			PhysicsWorld.s_geomToShape[geom] = this;
			m_world = world;
			m_object = null;
			m_transform = transform;
			m_geom = geom;
			m_mass = mass;
			m_group = CollisionGroup.Prop;
			m_userData = null;
			Ode.dGeomSetCategoryBits(geom, 0);
			Ode.dGeomSetCollideBits(geom, 0);
		}

		public virtual void Dispose()
		{
			App.Assert(m_object == null);
			PhysicsWorld.s_geomToShape.Remove(m_geom);
			Ode.dGeomDestroy(m_geom);
		}
	}
}
