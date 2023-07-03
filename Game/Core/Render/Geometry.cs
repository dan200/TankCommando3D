using System;

using Dan200.Core.Math;

using Dan200.Core.Main;
using System.IO;
using System.Collections.Generic;
using Dan200.Core.Util;

namespace Dan200.Core.Render
{
    internal class Geometry<TVertex> where TVertex : struct, IVertex
    {
        private Primitive m_primitiveType;

        private TVertex[] m_vertexData;
        private int m_vertexPos;
        private int m_vertexCount;

        private ushort[] m_indexData;
        private int m_indexPos;
        private int m_indexCount;

        public Primitive PrimitiveType
        {
            get
            {
                return m_primitiveType;
            }
        }

        public TVertex[] VertexData
        {
            get
            {
                return m_vertexData;
            }
        }
       
        public int VertexCount
        {
            get
            {
                return m_vertexCount;
            }
        }

        public int VertexPos
        {
            get
            {
                return m_vertexPos;
            }
            set
            {
                App.Assert(value >= 0 && value <= m_vertexCount);
                m_vertexPos = value;
            }
        }

        public ushort[] IndexData
        {
            get
            {
                return m_indexData;
            }
        }
       
        public int IndexCount
        {
            get
            {
                return m_indexCount;
            }
        }

        public int IndexPos
        {
            get
            {
                return m_indexPos;
            }
            set
            {
                App.Assert(value >= 0 && value <= m_indexCount);
                m_indexPos = value;
            }
        }

        public Geometry(Primitive primitiveType, int vertexCountHint = 32, int indexCountHint = 32)
        {
            m_primitiveType = primitiveType;

            m_vertexData = new TVertex[vertexCountHint];
            m_vertexPos = 0;
            m_vertexCount = 0;

            m_indexData = new ushort[indexCountHint];
            m_indexPos = 0;
            m_indexCount = 0;
        }

        public void Clear()
        {
            m_vertexPos = 0;
            m_vertexCount = 0;
            m_indexPos = 0;
            m_indexCount = 0;
        }

        public ref TVertex AddVertex()
        {
            int pos = m_vertexPos;
            if (pos >= m_vertexData.Length)
            {
                Array.Resize(ref m_vertexData, System.Math.Max(m_vertexData.Length * 2, 32));
            }
            m_vertexData[pos] = default(TVertex);
            m_vertexPos++;
            m_vertexCount = System.Math.Max(m_vertexPos, m_vertexCount);
            return ref m_vertexData[pos];
        }

        public ref TVertex AddVertex(in TVertex vertex)
        {
            ref TVertex result = ref AddVertex();
            result = vertex;
            return ref result;
        }

        public ref TVertex GetVertex(int vertexIndex)
        {
            App.Assert(vertexIndex >= 0 && vertexIndex < m_vertexCount);
            return ref m_vertexData[vertexIndex];
        }

        public ref ushort AddIndex(int index=0)
        {
            App.Assert(index >= 0 && index < m_vertexCount && index <= ushort.MaxValue);
            int pos = m_indexPos;
            if (pos >= m_indexData.Length)
            {
                Array.Resize(ref m_indexData, System.Math.Max(m_indexData.Length * 2, 32));
            }
            m_indexData[pos] = (ushort)index;
            m_indexPos++;
            m_indexCount = System.Math.Max(m_indexPos, m_indexCount);
            return ref m_indexData[pos];
        }

        public ref ushort GetIndex(int indexIndex)
        {
            App.Assert(indexIndex >= 0 && indexIndex < m_indexCount);
            return ref m_indexData[indexIndex];
        }
    }

    internal static class GeometryExtensions
    {
        public static void AddVertex(
            this Geometry<ModelVertex> geometry,
            byte groupIndex,
            Vector3 position,
            UnitVector3 normal,
            Vector2 texCoord
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.GroupIndex = groupIndex;
            vertex.Position = position;
            vertex.Normal = normal;
            vertex.TexCoord = texCoord;
        }

        public static void AddVertex(
            this Geometry<ShadowVertex> geometry,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            byte groupIndex,
            Vector3 position,
            float push
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.GroupIndex = groupIndex;
            vertex.Position = position;
            vertex.Push = push;
            vertex.A = a;
            vertex.B = b;
            vertex.C = c;
            vertex.D = d;
        }

        public static void AddVertex(
            this Geometry<CableShadowVertex> geometry,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 position,
            float push
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.Position = position;
            vertex.Push = push;
            vertex.A = a;
            vertex.B = b;
            vertex.C = c;
            vertex.D = d;
        }

        public static void AddVertex(
            this Geometry<ParticleVertex> geometry,
            short particleIndex,
            Vector3 position,
            Vector2 texCoord
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.ParticleIndex = particleIndex;
            vertex.Position = position;
            vertex.TexCoord = texCoord;
        }

        public static void AddVertex(
            this Geometry<FlatVertex> geometry,
            Vector3 position,
            Colour colour
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.Position = position;
            vertex.Colour = colour.ToLinear();
        }

        public static void AddVertex(
            this Geometry<ScreenVertex> geometry,
            Vector2 position,
            Vector2 texCoord,
            Colour colour
        )
        {
            ref var vertex = ref geometry.AddVertex();
            vertex.Position = position;
            vertex.TexCoord = texCoord;
            vertex.Colour = colour.ToLinear();
        }

        public static void ExportToOBJ(
            this Geometry<ModelVertex> geometry,
            string name,
            TextWriter writer
        )
        {
            writer.WriteLine("# OBJ File");
            writer.WriteLine("mtllib " + name + ".mtl");
            writer.WriteLine("o " + name);
            for (int i = 0; i < geometry.VertexCount; ++i)
            {
                var vertex = geometry.GetVertex(i);
                var position = vertex.Position;
                writer.WriteLine("v " + position.X + " " + position.Y + " " + -position.Z);
            }
            for (int i = 0; i < geometry.VertexCount; ++i)
            {
                var vertex = geometry.GetVertex(i);
                var texCoord = vertex.TexCoord;
                writer.WriteLine("vt " + texCoord.X + " " + (1.0f - texCoord.Y));
            }
            for (int i = 0; i < geometry.VertexCount; ++i)
            {
                var vertex = geometry.GetVertex(i);
                var normal = vertex.Normal;
                writer.WriteLine("vn " + normal.X + " " + normal.Y + " " + -normal.Z);
            }
            writer.WriteLine("usemtl " + name);
            writer.WriteLine("s off");
            int stride = geometry.PrimitiveType.GetVertexCount();
            for (int i = 0; i < geometry.IndexCount; i += stride)
            {
                writer.Write("f ");
                for (int j = 0; j < stride; ++j)
                {
                    int vert = geometry.GetIndex(i + j);
                    writer.Write((vert + 1) + "/" + (vert + 1) + "/" + (vert + 1));
                    if (j < stride - 1)
                    {
                        writer.Write(" ");
                    }
                }
                writer.WriteLine();
            }
        }
    }
}

