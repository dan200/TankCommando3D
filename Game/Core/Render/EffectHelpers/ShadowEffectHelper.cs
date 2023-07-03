using Dan200.Core.Math;

namespace Dan200.Core.Render
{
	internal abstract class ShadowEffectHelper : WorldEffectHelper
    {
        private struct Uniforms
        {
            public int LightPosition;
			public int LightDirection;
        }
		private Uniforms m_uniforms;

		public Vector3 LightPosition
        {
            set
            {
                Instance.SetUniform(m_uniforms.LightPosition, value);
            }
        }

		public UnitVector3 LightDirection
		{
			set
			{
				Instance.SetUniform(m_uniforms.LightDirection, value);
			}
		}

        protected ShadowEffectHelper(IRenderer renderer, Effect effect, ShaderDefines defines) : base(renderer, effect, defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
			m_uniforms.LightPosition = Instance.GetUniformLocation("lightPosition");
            m_uniforms.LightDirection = Instance.GetUniformLocation("lightDirection");
        }
    }
}
