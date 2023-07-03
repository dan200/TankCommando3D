using System;
namespace Dan200.Core.Render
{
	internal enum StencilTest : byte
	{
		Always = 0,
		EqualTo,
		LessThan,
		LessThanOrEqualTo,
		GreaterThan,
		GreaterThanOrEqualTo,
	}

	internal enum StencilOp : byte
	{
		Keep = 0,
		Replace,
		Increment,
		Decrement,
	}

	internal struct StencilParameters
	{
		public StencilTest Test;
		public byte RefValue;

		public StencilOp StencilTestFailOp;
		public StencilOp DepthTestFailOp;
		public StencilOp PassOp;

		public StencilOp BackfaceStencilTestFailOp;
		public StencilOp BackfaceDepthTestFailOp;
		public StencilOp BackfacePassOp;
	}

	internal interface IRenderer
	{
		EffectInstance CurrentEffect { get; set; }
		RenderTexture Target { get; set; }
		BlendMode BlendMode { get; set; }
		Rect Viewport { get; set; }
		bool ColourWrite { get; set; }
		bool DepthWrite { get; set; }
		bool DepthTest { get; set; }
		bool StencilTest { get; set; }
		StencilParameters StencilParameters { get; set; }
		bool CullBackfaces { get; set; }
		RenderStats RenderStats { get; }

		void MakeCurrent();
		void ResetViewport();

		void Clear(ColourF colour, byte stencil=0);
		void ClearColourOnly(ColourF colour);
		void ClearDepthOnly();
		void ClearStencilOnly(byte stencil=0);

        EffectInstance Instantiate(Effect effect, ShaderDefines defines);

        IRenderGeometry<TVertex> Upload<TVertex>(Geometry<TVertex> geometry, RenderGeometryFlags flags = RenderGeometryFlags.Default) where TVertex : struct, IVertex;
        void Draw<TVertex>(IRenderGeometry<TVertex> geometry) where TVertex : struct, IVertex;
        void DrawRange<TVertex>(IRenderGeometry<TVertex> geometry, int start, int count) where TVertex : struct, IVertex;

		Bitmap Capture();
		void Present();
	}
}
