using Dan200.Core.Math;
using System.Collections.Generic;

namespace Dan200.Core.Voxel
{
    internal class VoxelWorld
    {
        private List<object> m_layers;
        private Vector3 m_tileSize;

        public Vector3 TileSize
        {
            get
            {
                return m_tileSize;
            }
        }

        public VoxelWorld(Vector3 tileSize)
        {
            m_tileSize = tileSize;
            m_layers = new List<object>();
        }

        public void AddLayer<TVoxelData>(IVoxelLayer<TVoxelData> layer)
        {
            m_layers.Add(layer);
        }
    }
}
