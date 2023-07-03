using System;
using Dan200.Core.Main;

#if GLES
using OpenTK.Graphics.ES20;
using BufferUsageHint = OpenTK.Graphics.ES20.BufferUsage;
using VertexAttribIPointerType = OpenTK.Graphics.ES20.VertexAttribPointerType;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render.OpenGL
{
    internal class OpenGLRenderGeometry<TVertex> : IRenderGeometry<TVertex> where TVertex : struct, IVertex
    {
        private int m_vertexBuffer;
        private int m_indexBuffer;

        private int m_vertexCount;
        private int m_indexCount;
        private Primitive m_primitiveType;
        private RenderGeometryFlags m_flags;

        public int GLVertexBuffer
        {
            get
            {
                return m_vertexBuffer;
            }
        }

        public int GLIndexBuffer
        {
            get
            {
                return m_indexBuffer;
            }
        }

        public int VertexCount
        {
            get
            {
                return m_vertexCount;
            }
        }

        public int IndexCount
        {
            get
            {
                return m_indexCount;
            }
        }

        public Primitive PrimitiveType
        {
            get
            {
                return m_primitiveType;
            }
        }

        public OpenGLRenderGeometry(Geometry<TVertex> geometry, RenderGeometryFlags flags)
        {
            m_flags = flags;
            GL.GenBuffers(1, out m_vertexBuffer);
            GL.GenBuffers(1, out m_indexBuffer);
            Update(geometry);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref m_vertexBuffer);
            GL.DeleteBuffers(1, ref m_indexBuffer);
        }

        public void Update(Geometry<TVertex> geometry)
        {
            Update(geometry, 0, geometry.VertexCount, 0, geometry.IndexCount);
        }

        public unsafe void Update(Geometry<TVertex> geometry, int firstVertex, int vertexCount, int firstIndex, int indexCount)
        {
            m_primitiveType = geometry.PrimitiveType;

            var layout = VertexLayout.Get<TVertex>();
            var bufferUsageHint = ((m_flags & RenderGeometryFlags.Dynamic) != 0) ?
                BufferUsageHint.DynamicDraw :
                BufferUsageHint.StaticDraw;

            App.Assert(firstVertex >= 0 && vertexCount >= 0 && firstVertex + vertexCount <= geometry.VertexCount);
            App.Assert(firstVertex == 0); // TODO: Support this using the C# 7.3 "unmanaged" constraint
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexCount * layout.Stride), geometry.VertexData, bufferUsageHint);
            m_vertexCount = vertexCount;

            App.Assert(firstIndex >= 0 && indexCount >= 0 && firstIndex + indexCount <= geometry.IndexCount);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, m_indexBuffer);
            fixed (ushort* pIndices = geometry.IndexData)
            {
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexCount * sizeof(ushort)), (IntPtr)(pIndices + firstIndex), bufferUsageHint);
            }
            m_indexCount = indexCount;
        }
    }
}
