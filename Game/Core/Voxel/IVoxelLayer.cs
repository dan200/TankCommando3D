using Dan200.Core.Math;

namespace Dan200.Core.Voxel
{
    internal interface IVoxelLayer<TVoxelData>
    {
        TVoxelData Read(Vector3I pos);
        void Write(Vector3I pos, TVoxelData data);
    }
}
