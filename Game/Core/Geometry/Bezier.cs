using System;
using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
	internal struct Bezier
	{
		public Vector3 P0;
		public Vector3 P1;
		public Vector3 P2;
		public Vector3 P3;

		public Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
		{
			P0 = p0;
			P1 = p1;
			P2 = p2;
			P3 = p3;
		}

		public Vector3 Sample(float t)
		{
			return
				Mathf.Cube(1.0f - t) * P0 +
				3.0f * Mathf.Square(1.0f - t) * t * P1 +
				3.0f * (1.0f - t) * Mathf.Square(t) * P2 +
				Mathf.Cube(t) * P3;
		}

		public Vector3 SampleDerivative(float t)
		{
			return
				3.0f * Mathf.Square(1.0f - t) * (P1 - P0) +
				6.0f * (1.0f - t) * t * (P2 - P1) +
				3.0f * Mathf.Square(t) * (P3 - P2);
		}
	}
}
