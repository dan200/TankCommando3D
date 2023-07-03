using System.Runtime.InteropServices;
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CableUniforms : IUniformData
    {
        public Matrix4 StartModelMatrix;
        public Matrix4 EndModelMatrix;
    }

	internal class CableEffectHelper : LitEffectHelper
    {
        private struct Uniforms
        {
            public int CableData;
        }
        private Uniforms m_uniforms;

        public UniformBlock<CableUniforms> CableData
        {
            set
            {
                Instance.SetUniformBlock(m_uniforms.CableData, value);
            }
        }

		public CableEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/cable.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
			m_uniforms.CableData = Instance.GetUniformBlockLocation("CableData");
        }
    }
}
