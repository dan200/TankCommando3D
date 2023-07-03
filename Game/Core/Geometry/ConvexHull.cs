using System;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
	internal struct ConvexHull
	{
		private Plane[] m_planes;

		public Plane[] Planes
		{
			get
			{
				return m_planes;
			}
		}

		public ConvexHull(params Plane[] planes)
		{
			App.Assert(planes.Length >= 4);
			m_planes = planes;
		}

		public float Classify(Vector3 point)
		{
			float result = float.MinValue;
			foreach (var plane in m_planes)
			{
				result = Mathf.Max(result, plane.Classify(point));
			}
			return result;
		}

		public float Classify(Sphere sphere)
		{
			float result = float.MinValue;
			foreach (var plane in m_planes)
			{
				result = Mathf.Max(result, plane.Classify(sphere));
			}
			return result;
		}

		public bool Optimise()
		{
			int numPlanes = Planes.Length;
			for (int a = 0; a < numPlanes; ++a)
			{
				ref Plane planeA = ref m_planes[a];
				for (int b = a + 1; b < numPlanes; ++b)
				{
					ref Plane planeB = ref m_planes[b];
					if (planeA.Normal == planeB.Normal)
					{
						// Planes are parallel:
						// Merge plane B into plane A
						planeA.DistanceFromOrigin = Mathf.Min(planeA.DistanceFromOrigin, planeB.DistanceFromOrigin);

						// Remove plane B
						if (b < numPlanes - 1)
						{
							planeB = Planes[numPlanes - 1];
							b--;
						}
						numPlanes--;
					}
				}
			}
			if (numPlanes < Planes.Length)
			{
				App.Assert(numPlanes >= 4);
				Array.Resize(ref m_planes, numPlanes);
				return true;
			}
			return false;
		}

        public unsafe void BuildGeometry(List<Vector3> o_points, List<int> o_polygons)
		{
			int numPlanes = Planes.Length;
			int maxPointsPerPlane = ((numPlanes - 1) * (numPlanes - 2)) / 2;
			int indicesPerPlaneStride = (maxPointsPerPlane + 1);
			var indicesPerPlane = stackalloc int[numPlanes * indicesPerPlaneStride];

			// Find all the points where planes intersect:
			// For each set of 3 non-parallel planes
			for (int a = 0; a < numPlanes - 2; ++a)
			{
				ref Plane planeA = ref m_planes[a];
				for (int b = a + 1; b < numPlanes - 1; ++b)
				{
					ref Plane planeB = ref m_planes[b];
					var aCrossB = planeA.Normal.Cross(planeB.Normal);
					if (aCrossB.LengthSquared == 0.0f) continue; // A is parallel with B

					for (int c = b + 1; c < numPlanes; ++c)
					{
						ref Plane planeC = ref m_planes[c];
						float determinant = planeC.Normal.Dot(aCrossB);
						if (determinant == 0.0f) continue; // C is parallel with A or B

						// Find the intersection point of those planes
						Vector3 intersect = 
							(planeA.DistanceFromOrigin * (planeB.Normal.Cross(planeC.Normal)) +
							 planeB.DistanceFromOrigin * (planeC.Normal.Cross(planeA.Normal)) +
							 planeC.DistanceFromOrigin * (planeA.Normal.Cross(planeB.Normal))) /
							determinant;

						// Check that the intersection point is inside the hull
						int d;
						for (d = 0; d < a; ++d)
						{
							ref Plane planeD = ref m_planes[d];
							if (planeD.Classify(intersect) > 0.0f)
							{
								goto rejectpoint;
							}
						}
						for (d = a + 1; d < b; ++d)
						{
							ref Plane planeD = ref m_planes[d];
							if (planeD.Classify(intersect) > 0.0f)
							{
								goto rejectpoint;
							}
						}
						for (d = b + 1; d < c; ++d)
						{
							ref Plane planeD = ref m_planes[d];
							if (planeD.Classify(intersect) > 0.0f)
							{
								goto rejectpoint;
							}
						}
						for (d = c + 1; d < numPlanes; ++d)
						{
							ref Plane planeD = ref m_planes[d];
							if (planeD.Classify(intersect) > 0.0f)
							{
								goto rejectpoint;
							}
						}

						// Record the point
						int index = o_points.Count;
						indicesPerPlane[a * indicesPerPlaneStride + (++indicesPerPlane[a * indicesPerPlaneStride])] = index;
						indicesPerPlane[b * indicesPerPlaneStride + (++indicesPerPlane[b * indicesPerPlaneStride])] = index;
						indicesPerPlane[c * indicesPerPlaneStride + (++indicesPerPlane[c * indicesPerPlaneStride])] = index;
						o_points.Add(intersect);

						// We're done
						rejectpoint:
						continue;
					}
				}
			}

            // Skip polygons if not requested
            if(o_polygons == null)
            {
                return;
            }

			// For each plane, emit the intersection points with lie on that plane with a clockwise winding
			for (int i = 0; i < numPlanes; ++i)
			{
				var indexCount = indicesPerPlane[i * indicesPerPlaneStride];
                App.Assert(indexCount <= maxPointsPerPlane);
				if (indexCount >= 3)
				{
					var firstIndex = i * indicesPerPlaneStride + 1;
                    o_polygons.Add(indexCount);
                    for (int j = firstIndex; j < firstIndex + indexCount; ++j)
                    {                           
                        var idx = indicesPerPlane[j];
                        o_polygons.Add(idx);                    
                    }

                    var norm = m_planes[i].Normal;
                    var point0 = o_points[indicesPerPlane[firstIndex]];
                    o_polygons.Sort(o_polygons.Count - (indexCount - 1), indexCount - 1, Comparer<int>.Create(delegate (int idx0, int idx1)
					{
						var v0 = o_points[idx0] - point0;
						var v1 = o_points[idx1] - point0;
						var cross = norm.Dot(v1.Cross(v0));
						if (cross < 0.0f)
						{
							return 1;
						}
						else if (cross > 0.0f)
						{
							return -1;
						}
						return 0;
					}));
				}
				else
				{
					o_polygons.Add(0);
				}
			}
		}
	}
}
