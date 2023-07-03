
using Dan200.Core.Math;
using Dan200.Core.Render.OpenGL;

namespace Dan200.Core.Render
{
	internal class ScreenEffectHelper : EffectHelper
    {
        private struct Uniforms
        {
            public int ScreenSize;
            public int Texture;
        }
        private Uniforms m_uniforms;

        public Vector2 ScreenSize
        {
            set
            {
                Instance.SetUniform(m_uniforms.ScreenSize, value);
            }
        }

        public ITexture Texture
        {
            set
            {
                Instance.SetUniform(m_uniforms.Texture, value);
            }
        }

        public ScreenEffectHelper(IRenderer renderer) : base(renderer, Effect.Get("shaders/screen.effect"), ShaderDefines.Empty)
        {
        }

        protected override void LookupUniforms()
        {
            m_uniforms.ScreenSize = Instance.GetUniformLocation("screenSize");
            m_uniforms.Texture = Instance.GetUniformLocation("elementTexture");
        }
    }
}
