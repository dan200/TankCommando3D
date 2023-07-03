using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
    internal struct Sphere
    {
        public static readonly Sphere Zero = new Sphere();

        public Vector3 Center;
        public float Radius;

        public float Volume
        {
            get
            {
                return (4.0f / 3.0f) * Mathf.PI * Radius * Radius * Radius;
            }
        }

        public Sphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public void MoveAndExpandToFit(Vector3 pos)
        {
            var diff = (pos - Center);
            var d2 = diff.LengthSquared;
            if (d2 > Radius * Radius)
            {
                var d = Mathf.Sqrt(d2);
                var expand = 0.5f * (d - Radius);
                Radius += expand;
                Center += (diff / d) * expand;
            }
        }

        public void MoveAndExpandToFit(Sphere s)
        {
            var diff = (s.Center - Center);
            var d = diff.Length + s.Radius;
            if (d > Radius)
            {
                var expand = 0.5f * (d - Radius);
                Radius += expand;
                Center += (diff / (d - s.Radius)) * expand;
            }
        }
    }
}
