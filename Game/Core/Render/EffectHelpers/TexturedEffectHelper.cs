using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class TexturedEffectHelper : WorldEffectHelper
    {
        private struct Uniforms
        {
            public int ModelMatrix;
            public int Texture;
        }
        private Uniforms m_uniforms;

        public Matrix4 ModelMatrix
        {
            set
            {
                Instance.SetUniform(m_uniforms.ModelMatrix, ref value);
            }
        }

        public ITexture Texture
        {
            set
            {
                Instance.SetUniform(m_uniforms.Texture, value);
            }
        }

        public TexturedEffectHelper(IRenderer renderer) : base(renderer, Effect.Get("shaders/textured.effect"), ShaderDefines.Empty)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrix = Instance.GetUniformLocation("modelMatrix");
            m_uniforms.Texture = Instance.GetUniformLocation("_texture");
        }
    }
}
