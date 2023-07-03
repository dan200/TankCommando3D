using System;
namespace Dan200.Core.Render
{
    [Flags]
    internal enum RenderGeometryFlags
    {
        Default = 0,
        Dynamic = 1,
    }

    internal interface IRenderGeometry<TVertex> : IDisposable where TVertex : struct, IVertex
    {
        int VertexCount { get; }
        int IndexCount { get; }
        Primitive PrimitiveType { get; }

        void Update(Geometry<TVertex> geometry);
    }
}
