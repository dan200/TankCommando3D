using Dan200.Core.Math;
using System.Runtime.InteropServices;

namespace Dan200.Core.Render
{
    internal interface IVertex
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ModelVertex : IVertex
    {
        [VertexAttribute("groupIndex")]
        public byte GroupIndex;

        [VertexAttribute("position")]
        public Vector3 Position;

        [VertexAttribute("normal")]
        public UnitVector3 Normal;

        [VertexAttribute("texCoord")]
        public Vector2 TexCoord;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MapVertex : IVertex
    {
        [VertexAttribute("position")]
        public Vector3 Position;

        [VertexAttribute("normal")]
        public UnitVector3 Normal;

        [VertexAttribute("texCoord")]
        public Vector2 TexCoord;
    }

    [StructLayout(LayoutKind.Sequential)]
	internal struct CableVertex : IVertex
	{
		[VertexAttribute("position")]
		public Vector3 Position;

		[VertexAttribute("normal")]
		public UnitVector3 Normal;
	}

    [StructLayout(LayoutKind.Sequential)]
    internal struct ShadowVertex : IVertex
    {
        [VertexAttribute("groupIndex")]
        public byte GroupIndex;

		[VertexAttribute("position")]
		public Vector3 Position;

		[VertexAttribute("push")]
		public float Push;

        [VertexAttribute("a")]
        public Vector3 A;

		[VertexAttribute("b")]
		public Vector3 B;

		[VertexAttribute("c")]
		public Vector3 C;

		[VertexAttribute("d")]
		public Vector3 D;
    }

	[StructLayout(LayoutKind.Sequential)]
	internal struct CableShadowVertex : IVertex
	{
		[VertexAttribute("position")]
		public Vector3 Position;

		[VertexAttribute("push")]
		public float Push;

		[VertexAttribute("a")]
		public Vector3 A;

		[VertexAttribute("b")]
		public Vector3 B;

		[VertexAttribute("c")]
		public Vector3 C;

		[VertexAttribute("d")]
		public Vector3 D;
	}

    [StructLayout(LayoutKind.Sequential)]
    internal struct ParticleVertex : IVertex
    {
        [VertexAttribute("particleIndex")]
        public short ParticleIndex;

        [VertexAttribute("position")]
        public Vector3 Position;

        [VertexAttribute("texCoord")]
        public Vector2 TexCoord;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FlatVertex : IVertex
    {
        [VertexAttribute("position")]
        public Vector3 Position;

        [VertexAttribute("colour")]
        public Colour Colour;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenVertex : IVertex
    {
        [VertexAttribute("position")]
        public Vector2 Position;

        [VertexAttribute("texCoord")]
        public Vector2 TexCoord;

		[VertexAttribute("colour")]
		public Colour Colour;
    }
}
