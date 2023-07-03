using System;
using System.Collections.Generic;
using Dan200.Core.Math;

namespace Dan200.Core.Voxel
{
    internal class SparseVoxelLayer<TVoxelData> : IVoxelLayer<TVoxelData> where TVoxelData : IEquatable<TVoxelData>
    {
        private Dictionary<Vector3I, TVoxelData> m_data;

        public SparseVoxelLayer()
        {
            m_data = new Dictionary<Vector3I, TVoxelData>();
        }

        public TVoxelData Read(Vector3I pos)
        {
            TVoxelData result;
            if (m_data.TryGetValue(pos, out result))
            {
                return result;
            }
            return result;
        }

        public void Write(Vector3I pos, TVoxelData data)
        {
            if (data.Equals(default(TVoxelData)))
            {
                m_data.Remove(pos);
            }
            else
            {
                m_data[pos] = data;
            }
        }
    }
}
