using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
    internal struct Plane
    {
        public UnitVector3 Normal;
        public float DistanceFromOrigin;

        public Plane(UnitVector3 normal, float distanceFromOrigin)
        {
            Normal = normal;
            DistanceFromOrigin = distanceFromOrigin;
        }

        public Plane(UnitVector3 normal, Vector3 origin)
        {
            Normal = normal;
            DistanceFromOrigin = normal.Dot(origin);
        }

        public float Classify(Vector3 position)
        {
            return position.Dot(Normal) - DistanceFromOrigin;
        }

        public float Classify(Sphere sphere)
        {
            return sphere.Center.Dot(Normal) - DistanceFromOrigin - sphere.Radius;
        }

        public float ClassifyShadow(Sphere sphere, UnitVector3 lightDir)
        {
            return Mathf.Min(Classify(sphere), lightDir.Dot(Normal));
        }
    }
}
