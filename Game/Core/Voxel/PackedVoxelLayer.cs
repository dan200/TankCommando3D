using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Math;

namespace Dan200.Core.Voxel
{
    internal class PackedVoxelLayer<TVoxelData> : IVoxelLayer<TVoxelData>
    {
        private static readonly Vector3I DEFAULT_CHUNK_SIZE = new Vector3I(16, 16, 16);

        public class Chunk
        {
            public Vector3I Size
            {
                get
                {
                    return new Vector3I(
                        Data.GetLength(0),
                        Data.GetLength(1),
                        Data.GetLength(2)
                    );
                }
            }

            public readonly Vector3I Origin;
            public readonly TVoxelData[,,] Data;
            public uint Version;

            public Chunk(Vector3I origin, Vector3I size)
            {
                Origin = origin;
                Data = new TVoxelData[size.X, size.Y, size.Z];
                Version = 0;
            }

            public Chunk(Vector3I origin, TVoxelData[,,] data)
            {
                Origin = origin;
                Data = data;
                Version = 0;
            }
        }

        private Vector3I m_chunkSize;
        private Dictionary<Vector3I, Chunk> m_chunks;
        private TVoxelData m_defaultValue;

        public struct ChunkCollection : IEnumerable<Chunk>
        {
            public struct Enumerator : IEnumerator<Chunk>
            {
                private Dictionary<Vector3I, Chunk>.Enumerator m_enumerator;

                public Chunk Current
                {
                    get
                    {
                        return m_enumerator.Current.Value;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }
                
                public Enumerator(PackedVoxelLayer<TVoxelData> owner)
                {
                    m_enumerator = owner.m_chunks.GetEnumerator();
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    return m_enumerator.MoveNext();
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private PackedVoxelLayer<TVoxelData> m_owner;

            public ChunkCollection(PackedVoxelLayer<TVoxelData> owner)
            {
                m_owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner);
            }

            IEnumerator<Chunk> IEnumerable<Chunk>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public ChunkCollection Chunks
        {
            get
            {
                return new ChunkCollection(this);
            }
        }

        public Vector3I ChunkSize
        {
            get
            {
                return m_chunkSize;
            }
        }

        public PackedVoxelLayer() : this(DEFAULT_CHUNK_SIZE)
        {            
        }

        public PackedVoxelLayer(Vector3I chunkSize, TVoxelData defaultValue=default(TVoxelData))
        {
            m_chunkSize = chunkSize;
            m_chunks = new Dictionary<Vector3I, Chunk>();
            m_defaultValue = defaultValue;
        }

        public TVoxelData Read(Vector3I pos)
        {
            var chunk = GetChunk(pos, false);
            if (chunk != null)
            {
                var offset = pos - chunk.Origin;
                return chunk.Data[offset.X, offset.Y, offset.Z];
            }
            return m_defaultValue;
        }

        public void Write(Vector3I pos, TVoxelData data)
        {
            var chunk = GetChunk(pos, true);
            if (chunk != null)
            {
                var offset = pos - chunk.Origin;
                ref TVoxelData value = ref chunk.Data[offset.X, offset.Y, offset.Z];
                if (!value.Equals(data))
                {
                    value = data;
                    chunk.Version++;
                }
            }
        }

        private int GetChunkOrigin(int pos, int chunkSize)
        {
            return (pos % chunkSize >= 0) ?
                (pos - (pos % chunkSize)) :
                (pos - (pos % chunkSize) - chunkSize);
        }

        public Vector3I GetChunkOrigin(Vector3I pos)
        {
            return new Vector3I(
                GetChunkOrigin(pos.X, m_chunkSize.X),
                GetChunkOrigin(pos.Y, m_chunkSize.Y),
                GetChunkOrigin(pos.Z, m_chunkSize.Z)
            );
        }

        public Chunk GetChunk(Vector3I pos, bool createIfMissing=false)
        {
            var origin = GetChunkOrigin(pos);

            Chunk chunk;
            if (m_chunks.TryGetValue(origin, out chunk))
            {
                return chunk;
            }
            else if (createIfMissing)
            {
                chunk = new Chunk(origin, m_chunkSize);
                if (!m_defaultValue.Equals(default(TVoxelData)))
                {
                    for (int x = 0; x < m_chunkSize.X; ++x)
                    {
                        for (int y = 0; y < m_chunkSize.Y; ++y)
                        {
                            for (int z = 0; z < m_chunkSize.Z; ++z)
                            {
                                chunk.Data[x, y, z] = m_defaultValue;
                            }
                        }
                    }
                }
                m_chunks[origin] = chunk;
                return chunk;
            }
            else
            {
                return null;
            }
        }
    }
}
