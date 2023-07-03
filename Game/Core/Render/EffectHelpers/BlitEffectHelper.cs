
namespace Dan200.Core.Render
{
	internal class BlitEffectHelper : EffectHelper
    {
        private struct Uniforms
        {
            public int Texture;
        }
        private Uniforms m_uniforms;

        public ITexture Texture
        {
            set
            {
                Instance.SetUniform(m_uniforms.Texture, value);
            }
        }

        public BlitEffectHelper(IRenderer renderer) : base(renderer, Effect.Get("shaders/blit.effect"), ShaderDefines.Empty)
        {
        }

        protected override void LookupUniforms()
        {
            m_uniforms.Texture = Instance.GetUniformLocation("inputTexture");
        }
    }
}
