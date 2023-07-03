using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class CableShadowEffectHelper : ShadowEffectHelper
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

		public CableShadowEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/cable_shadows.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
			m_uniforms.CableData = Instance.GetUniformBlockLocation("CableData");
        }
    }
}
