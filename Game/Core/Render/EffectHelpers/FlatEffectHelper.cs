using Dan200.Core.Math;

namespace Dan200.Core.Render
{
    internal class FlatEffectHelper : WorldEffectHelper
    {
        private struct Uniforms
        {
            public int ModelMatrix;
        }
        private Uniforms m_uniforms;

        public Matrix4 ModelMatrix
        {
            set
            {
                Instance.SetUniform(m_uniforms.ModelMatrix, ref value);
            }
        }

        public FlatEffectHelper(IRenderer renderer) : base(renderer, Effect.Get("shaders/flat.effect"), ShaderDefines.Empty)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrix = Instance.GetUniformLocation("modelMatrix");
        }
    }
}
