using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
    internal struct Ray
    {
        public Vector3 Origin;
        public UnitVector3 Direction;
        public float Length;

        public Ray(Vector3 origin, UnitVector3 direction, float length)
        {
            Origin = origin;
            Direction = direction;
            Length = length;
        }

        public Ray(Vector3 origin, Vector3 destination)
        {
            var journey = destination - origin;
            Origin = origin;
            Direction = journey.SafeNormalise(Vector3.ZAxis);
            Length = journey.Length;
        }
    }
}

