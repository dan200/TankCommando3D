using Dan200.Core.Geometry;
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class Camera
    {
        public Matrix4 Transform;
        public Vector3 Velocity;
        public float FOV;
        public float AspectRatio;
        public float NearPlane = 0.1f;

        public Vector3 Position
        {
            get
            {
                return Transform.Position;
            }
        }

        public Matrix4 ProjectionMatrix
        {
            get;
            private set;
        }

        public Matrix4 ViewMatrix
        {
            get;
            private set;
        }

        public InfiniteFrustum ViewFrustum
        {
            get;
            private set;
        }

        public Camera()
        {
            Transform = Matrix4.Identity;
            Velocity = Vector3.Zero;
			FOV = 60.0f * Mathf.DEGREES_TO_RADIANS;
            AspectRatio = 1.0f;
            UpdateMatrices();
        }

        public void UpdateMatrices()
        {
			ProjectionMatrix = Matrix4.CreatePerspective(FOV, AspectRatio, NearPlane, float.PositiveInfinity);
            ViewMatrix = Transform.InvertAffine();
            ViewFrustum = GenerateFrustum();
        }

        private InfiniteFrustum GenerateFrustum()
        {
            var top = NearPlane * Mathf.Tan(0.5f * FOV);
            var right = top * AspectRatio;

            var pos = Transform.Position;
            var f = Transform.Forward;
            var r = Transform.Right;
            var u = Transform.Up;

            var leftNorm = (-right * f - NearPlane * r).Normalise();
            var rightNorm = (-right * f + NearPlane * r).Normalise();
            var topNorm = (-top * f + NearPlane * u).Normalise();
            var bottomNorm = (-top * f - NearPlane * u).Normalise();

            return new InfiniteFrustum(
                new Plane(leftNorm, pos.Dot(leftNorm)),
                new Plane(rightNorm, pos.Dot(rightNorm)),
                new Plane(topNorm, pos.Dot(topNorm)),
                new Plane(bottomNorm, pos.Dot(bottomNorm)),
                new Plane(-f, pos.Dot(-f) - NearPlane)
            );
        }
    }
}
