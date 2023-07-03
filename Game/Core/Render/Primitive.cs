namespace Dan200.Core.Render
{
    internal enum Primitive
    {
        Lines = 0,
        Triangles
    }

    internal static class PrimitiveExtensions
    {
        public static int GetVertexCount(this Primitive primitive)
        {
            return (int)primitive + 2;
        }
    }
}
