using Dan200.Core.Main;
using Dan200.Core.Math;

namespace Dan200.Core.Geometry
{
    internal struct AABB
    {
        public static readonly AABB Zero = new AABB();

        public Vector3 Min;
        public Vector3 Max;

        public Vector3 Size
        {
            get
            {
                return Max - Min;
            }
        }

        public Vector3 Center
        {
            get
            {
                return 0.5f * (Min + Max);
            }
        }

        public float Volume
        {
            get
            {
                var size = Size;
                return size.X * size.Y * size.Z;
            }
        }

        public AABB(Vector3 min, Vector3 max)
        {
            App.Assert(max.X >= min.X);
            App.Assert(max.Y >= min.Y);
            App.Assert(max.Z >= min.Z);
            Min = min;
            Max = max;
        }

        public void ExpandToFit(Vector3 pos)
        {
			Min.X = Mathf.Min(pos.X, Min.X);
            Min.Y = Mathf.Min(pos.Y, Min.Y);
            Min.Z = Mathf.Min(pos.Z, Min.Z);
			Max.X = Mathf.Max(pos.X, Max.X);
            Max.Y = Mathf.Max(pos.Y, Max.Y);
            Max.Z = Mathf.Max(pos.Z, Max.Z);
        }

        public void ExpandToFit(AABB aabb)
        {
            Min.X = Mathf.Min(aabb.Min.X, Min.X);
            Min.Y = Mathf.Min(aabb.Min.Y, Min.Y);
            Min.Z = Mathf.Min(aabb.Min.Z, Min.Z);
            Max.X = Mathf.Max(aabb.Max.X, Max.X);
            Max.Y = Mathf.Max(aabb.Max.Y, Max.Y);
            Max.Z = Mathf.Max(aabb.Max.Z, Max.Z);
        }
    }
}
