using Dan200.Core.Math;

namespace Dan200.Core.Render
{
	internal class ModelEffectHelper : LitEffectHelper
    {
		public const int MAX_GROUPS = 16;

        private struct Uniforms
        {
            public int ModelMatrices;
            public int NormalMatrices;
            public int UVMatrices;
            public int Colours;
            public int DiffuseTexture;
            public int SpecularTexture;
            public int NormalTexture;
            public int EmissiveTexture;
        }
        private Uniforms m_uniforms;
        private Matrix3[] m_normalMatrices;
        private IMaterial m_material;

        public Matrix4[] ModelMatrices
        {
            set
            {
                UpdateNormalMatrices(value);
				Instance.SetUniform(m_uniforms.ModelMatrices, value);
                Instance.SetUniform(m_uniforms.NormalMatrices, m_normalMatrices, 0, value.Length);
            }
        }

        public Matrix3[] UVMatrices
        {
            set
            {
                Instance.SetUniform(m_uniforms.UVMatrices, value);
            }
        }

        public ColourF[] Colours
        {
            set
            {
                Instance.SetUniform(m_uniforms.Colours, value);
            }
        }

        public IMaterial Material
        {
            set
            {
                var material = value;
                if (m_material != material)
                {
                    Instance.SetUniform(m_uniforms.DiffuseTexture, material.DiffuseTexture);
                    Instance.SetUniform(m_uniforms.SpecularTexture, material.SpecularTexture);
                    Instance.SetUniform(m_uniforms.NormalTexture, material.NormalTexture);
                    Instance.SetUniform(m_uniforms.EmissiveTexture, material.EmissiveTexture);
                    m_material = value;
                }
            }
        }

        public ModelEffectHelper(IRenderer renderer, ShaderDefines defines) : base(renderer, Effect.Get("shaders/model.effect"), defines)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrices = Instance.GetUniformLocation("modelMatrices");
            m_uniforms.NormalMatrices = Instance.GetUniformLocation("normalMatrices");
            m_uniforms.UVMatrices = Instance.GetUniformLocation("uvMatrices");
            m_uniforms.Colours = Instance.GetUniformLocation("colours");
            m_uniforms.DiffuseTexture = Instance.GetUniformLocation("diffuseTexture");
            m_uniforms.SpecularTexture = Instance.GetUniformLocation("specularTexture");
            m_uniforms.NormalTexture = Instance.GetUniformLocation("normalTexture");
            m_uniforms.EmissiveTexture = Instance.GetUniformLocation("emissiveTexture");
            m_material = null;
        }

        private void UpdateNormalMatrices(Matrix4[] modelMatrices)
        {
            if (m_normalMatrices == null || m_normalMatrices.Length < modelMatrices.Length)
            {
                m_normalMatrices = new Matrix3[modelMatrices.Length];
            }
            for (int i = 0; i < modelMatrices.Length; ++i)
            {
                m_normalMatrices[i] = modelMatrices[i].Rotation.InvertAffine();
            }
        }
    }
}
