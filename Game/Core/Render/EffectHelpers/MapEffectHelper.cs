using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class MapEffectHelper : LitEffectHelper
    {
        private struct Uniforms
        {
            public int ModelMatrix;
            public int Texture;
            public int TextureSize;
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
                Instance.SetUniform(m_uniforms.TextureSize, new Vector2(value.Width, value.Height));
            }
        }

        public MapEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/map.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrix = Instance.GetUniformLocation("modelMatrix");
            m_uniforms.Texture = Instance.GetUniformLocation("_texture");
            m_uniforms.TextureSize = Instance.GetUniformLocation("textureSize");
        }
    }
}
