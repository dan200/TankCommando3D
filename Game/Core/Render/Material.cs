using Dan200.Core.Assets;

namespace Dan200.Core.Render
{
    internal class Material : IMaterial
    {
        public static Material Default = new Material();

        public string DiffuseTexturePath;
        public string SpecularTexturePath;
        public string NormalTexturePath;
        public string EmissiveTexturePath;

        private Texture m_diffuseTexture;
        private Texture m_specularTexture;
        private Texture m_normalTexture;
        private Texture m_emisiveTexture;

        public ITexture DiffuseTexture
        {
            get
            {
                if (m_diffuseTexture == null)
                {
                    if (DiffuseTexturePath != null)
                    {
                        m_diffuseTexture = Texture.Get(DiffuseTexturePath, false);
                    }
                    else
                    {
                        m_diffuseTexture = Texture.White;
                    }
                }
                return m_diffuseTexture;
            }
        }

        public ITexture SpecularTexture
        {
            get
            {
                if (m_specularTexture == null)
                {
                    if (SpecularTexturePath != null)
                    {
                        m_specularTexture = Texture.Get(SpecularTexturePath, false);
                    }
                    else if (DiffuseTexturePath != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(DiffuseTexturePath, "spec.png");
                        if (Assets.Assets.Exists<Texture>(potentialPath))
                        {
                            m_specularTexture = Texture.Get(potentialPath, false);
                        }
                        else
                        {
							m_specularTexture = Texture.Black;
                        }
                    }
                    else
                    {
						m_specularTexture = Texture.Black;
                    }
                }
                return m_specularTexture;
            }
        }

        public ITexture NormalTexture
        {
            get
            {
                if (m_normalTexture == null)
                {
                    if (NormalTexturePath != null)
                    {
                        m_normalTexture = Texture.Get(NormalTexturePath, false);
                    }
                    else if (DiffuseTexturePath != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(DiffuseTexturePath, "norm.png");
                        if (Assets.Assets.Exists<Texture>(potentialPath))
                        {
                            m_normalTexture = Texture.Get(potentialPath, false);
                        }
                        else
                        {
                            m_normalTexture = Texture.Flat;
                        }
                    }
                    else
                    {
                        m_normalTexture = Texture.Flat;
                    }
                }
                return m_normalTexture;
            }
        }

        public ITexture EmissiveTexture
        {
            get
            {
                if (m_emisiveTexture == null)
                {
                    if (EmissiveTexturePath != null)
                    {
                        m_emisiveTexture = Texture.Get(EmissiveTexturePath, false);
                    }
                    else if (DiffuseTexturePath != null)
                    {
                        var potentialPath = AssetPath.ChangeExtension(DiffuseTexturePath, "emit.png");
                        if (Assets.Assets.Exists<Texture>(potentialPath))
                        {
                            m_emisiveTexture = Texture.Get(potentialPath, false);
                        }
                        else
                        {
                            m_emisiveTexture = Texture.Black;
                        }
                    }
                    else
                    {
                        m_emisiveTexture = Texture.Black;
                    }
                }
                return m_emisiveTexture;
            }
        }

        public Material()
        {
        }
    }
}
