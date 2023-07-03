using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
    internal struct Frustum
    {
        public Plane Left;
        public Plane Right;
        public Plane Top;
        public Plane Bottom;
        public Plane Near;
        public Plane Far;

        public Frustum(Plane left, Plane right, Plane top, Plane bottom, Plane near, Plane far)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
            Near = near;
            Far = far;
        }

        public float Classify(Vector3 point)
        {
            return Max(
                Left.Classify(point),
                Right.Classify(point),
                Top.Classify(point),
                Bottom.Classify(point),
                Near.Classify(point),
                Far.Classify(point)
            );
        }

        public float Classify(Sphere sphere)
        {
            return Max(
                Left.Classify(sphere),
                Right.Classify(sphere),
                Top.Classify(sphere),
                Bottom.Classify(sphere),
                Near.Classify(sphere),
                Far.Classify(sphere)
            );
        }

        public float ClassifyShadow(Sphere sphere, UnitVector3 lightDir)
        {
            return Max(
                Left.ClassifyShadow(sphere, lightDir),
                Right.ClassifyShadow(sphere, lightDir),
                Top.ClassifyShadow(sphere, lightDir),
                Bottom.ClassifyShadow(sphere, lightDir),
                Near.ClassifyShadow(sphere, lightDir),
                Far.ClassifyShadow(sphere, lightDir)
            );
        }

        private static float Max(float a, float b, float c, float d, float e, float f)
        {
            var ab = (a > b) ? a : b;
            var cd = (c > d) ? c : d;
            var ef = (e > f) ? e : f;
            var abcd = (ab > cd) ? ab : cd;
            return (abcd > ef) ? abcd : ef;
        }
    }
}
