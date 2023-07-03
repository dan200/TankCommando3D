using Dan200.Core.Math;

namespace Dan200.Core.Physics
{
    internal struct Contact
    {
		public PhysicsShape Shape;
        public Vector3 Position;
        public UnitVector3 Normal;
        public float Depth;
    }
}
