using System.Runtime.InteropServices;
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    [StructLayout(LayoutKind.Sequential)]
	internal struct CameraUniformData : IUniformData
	{
		public Matrix4 ViewMatrix;
		public Matrix4 ProjectionMatrix;
		public Vector3 CameraPosition;
	}
	
	internal abstract class WorldEffectHelper : EffectHelper
    {
        private struct Uniforms
        {
            public int CameraBlock;
        }
        private Uniforms m_uniforms;

		public UniformBlock<CameraUniformData> CameraBlock
        {
            set
            {
				Instance.SetUniformBlock(m_uniforms.CameraBlock, value);
            }
        }

        protected WorldEffectHelper(IRenderer renderer, Effect effect, ShaderDefines defines) : base(renderer, effect, defines)
        {
        }

        protected override void LookupUniforms()
        {
			m_uniforms.CameraBlock = Instance.GetUniformBlockLocation("CameraData");
        }
    }
}
