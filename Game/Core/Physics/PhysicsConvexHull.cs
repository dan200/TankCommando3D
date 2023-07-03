using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dan200.Core.Geometry;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
	internal unsafe class PhysicsConvexHull : PhysicsShape
	{
		private readonly ConvexHull m_hull;
		private IntPtr m_memory;

		public ConvexHull Hull
		{
			get
			{
				return m_hull;
			}
		}

        public static PhysicsConvexHull Create(PhysicsWorld world, ref Matrix4 transform, ref ConvexHull hull)
        {
            // Get the hull geometry
            var verts = new List<Vector3>();
			var indicies = new List<int>();
            hull.BuildGeometry(verts, indicies);
            App.Assert(verts.Count > 0);

            // Build the shape
            IntPtr memory;
            Ode.dGeomID geom;
			unsafe
			{
				int planeCount = hull.Planes.Length;
				int pointCount = verts.Count;
				int polyCount = indicies.Count;
				memory = Marshal.AllocHGlobal(
					planeCount * 4 * sizeof(float) +
					pointCount * 3 * sizeof(float) +
					polyCount * sizeof(int)
				);

				float* planes = (float*)memory;
				for (int i = 0; i < planeCount; ++i)
				{
					ref Plane plane = ref hull.Planes[i];
					App.Assert(plane.DistanceFromOrigin > 0.0f);
					planes[i * 4 + 0] = plane.Normal.X;
					planes[i * 4 + 1] = plane.Normal.Y;
					planes[i * 4 + 2] = plane.Normal.Z;
					planes[i * 4 + 3] = plane.DistanceFromOrigin;
				}

				float* points = (float*)(memory + planeCount * 4 * sizeof(float));
				for (int i = 0; i<pointCount; ++i)
				{
					var vert = verts[i];
					points[i * 3 + 0] = vert.X;
					points[i * 3 + 1] = vert.Y;
					points[i * 3 + 2] = vert.Z;
				}

				uint* polygons = (uint*)(memory + planeCount * 4 * sizeof(float) + pointCount * 3 * sizeof(float));
				for (int i = 0; i<polyCount; ++i)
				{
					var idx = indicies[i];
					polygons[i] = (uint)idx;
				}

                geom = Ode.dCreateConvex(
					world.m_space,
					ref planes[0],
					(uint)planeCount,
					ref points[0],
					(uint)pointCount,
					ref polygons[0]
				);
			}

            // Build the mass
            var mass = new Ode.dMass();
            {
                // Approximate as a box
                var centreOfMass = Vector3.Zero;
                foreach (var vert in verts)
                {
                    centreOfMass += vert;
                }
                centreOfMass /= verts.Count;

                var averageDivergence = Vector3.Zero;
                foreach (var vert in verts)
                {
                    averageDivergence.X += Mathf.Abs(vert.X - centreOfMass.X);
                    averageDivergence.Y += Mathf.Abs(vert.Y - centreOfMass.Y);
                    averageDivergence.Z += Mathf.Abs(vert.Z - centreOfMass.Z);
                }
                averageDivergence /= verts.Count;

                var size = 2.0f * averageDivergence;
                var centre = centreOfMass;
                Ode.dMassSetBox(ref mass, 1.0f, size.X, size.Y, size.Z);
                Ode.dMassTranslate(ref mass, centre.X, centre.Y, centre.Z);
            }

            return new PhysicsConvexHull(world, ref transform, ref hull, geom, mass, memory);
		}

        private PhysicsConvexHull(PhysicsWorld world, ref Matrix4 transform, ref ConvexHull hull, Ode.dGeomID geom, Ode.dMass mass, IntPtr memory) :
            base(world, ref transform, geom, mass)
		{
			m_hull = hull;
            m_memory = memory;
		}

		public override void Dispose()
		{
			Marshal.FreeHGlobal(m_memory);
			base.Dispose();
		}
	}
}
