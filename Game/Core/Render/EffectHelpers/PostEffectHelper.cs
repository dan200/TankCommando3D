
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
	internal class PostEffectHelper : EffectHelper
    {
        private struct Uniforms
        {
            public int Texture;
            public int ViewportSize;
            public int Gamma;
            public int Saturation;
        }
        private Uniforms m_uniforms;

        public ITexture Texture
        {
            set
            {
                Instance.SetUniform(m_uniforms.Texture, value);
                Instance.SetUniform(m_uniforms.ViewportSize, new Vector2(value.Width, value.Height));
            }
        }

        public float Gamma
        {
            set
            {
                Instance.SetUniform(m_uniforms.Gamma, value);
            }
        }

        public float Saturation
        {
            set
            {
                Instance.SetUniform(m_uniforms.Saturation, value);
            }
        }

		public PostEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/post.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            m_uniforms.Texture = Instance.GetUniformLocation("inputTexture");
            m_uniforms.ViewportSize = Instance.GetUniformLocation("viewportSize");
            m_uniforms.Gamma = Instance.GetUniformLocation("gamma");
            m_uniforms.Saturation = Instance.GetUniformLocation("saturation");
        }
    }
}
