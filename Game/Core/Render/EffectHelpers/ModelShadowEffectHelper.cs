using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class ModelShadowEffectHelper : ShadowEffectHelper
    {
        private struct Uniforms
        {
            public int ModelMatrices;
        }
        private Uniforms m_uniforms;

        public Matrix4[] ModelMatrices
        {
            set
            {
                Instance.SetUniform(m_uniforms.ModelMatrices, value);
            }
        }

		public ModelShadowEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/model_shadows.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrices = Instance.GetUniformLocation("modelMatrices");
        }
    }
}
