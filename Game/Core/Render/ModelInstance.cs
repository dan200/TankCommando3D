
using Dan200.Core.Animation;
using Dan200.Core.Math;


namespace Dan200.Core.Render
{
    internal class ModelInstance
    {
        private Model m_model;
        private Matrix4 m_baseTransform;
        private Matrix4[] m_groupTransforms;
        private Matrix4[] m_fullTransforms;
        private bool m_baseVisible;
        private bool[] m_visibility;
        private Matrix3[] m_uvTransforms;
        private ColourF m_baseColour;
        private ColourF[] m_groupColours;
        private ColourF[] m_fullColours;
        private IMaterial[] m_materialOverrides;

        private IAnimation m_animation;
        private float m_animTime;
        private bool m_offscreen;
        private bool m_shadowOffscreen;

        public Model Model
        {
            get
            {
                return m_model;
            }
        }

        public Matrix4 Transform
        {
            get
            {
                return m_baseTransform;
            }
            set
            {
                m_baseTransform = value;
                for (int i = 0; i < m_groupTransforms.Length; ++i)
                {
					m_fullTransforms[i] = m_baseTransform.ToWorld(m_groupTransforms[i]);
                }
            }
        }

        public bool Visible
        {
            get
            {
                return m_baseVisible;
            }
            set
            {
                m_baseVisible = value;
            }
        }

        public bool Offscreen
        {
            get
            {
                return m_offscreen;
            }
        }

		public bool ShadowOffscreen
		{
			get
			{
				return m_shadowOffscreen;
			}
		}

        public ColourF Colour
        {
            get
            {
                return m_baseColour;
            }
            set
            {
                if (m_baseColour != value)
                {
                    m_baseColour = value;
                    for (int i = 0; i < m_groupColours.Length; ++i)
                    {
                        m_fullColours[i] = m_groupColours[i] * m_baseColour;
                    }
                }
            }
        }

        public IAnimation Animation
        {
            get
            {
                return m_animation;
            }
            set
            {
                m_animation = value;
            }
        }

        public float AnimTime
        {
            get
            {
                return m_animTime;
            }
            set
            {
                m_animTime = value;
            }
        }

        public ModelInstance(Model model, Matrix4 transform)
        {
            m_model = model;
            m_baseTransform = transform;
            m_baseColour = ColourF.White;
            m_groupTransforms = new Matrix4[model.GroupCount];
            m_fullTransforms = new Matrix4[model.GroupCount];
            m_baseVisible = true;
            m_visibility = new bool[model.GroupCount];
            m_uvTransforms = new Matrix3[model.GroupCount];
            m_groupColours = new ColourF[model.GroupCount];
            m_fullColours = new ColourF[model.GroupCount];
            m_materialOverrides = new IMaterial[model.GroupCount];
            for (int i = 0; i < m_groupTransforms.Length; ++i)
            {
                m_groupTransforms[i] = Matrix4.Identity;
                m_fullTransforms[i] = transform;
                m_visibility[i] = true;
                m_uvTransforms[i] = Matrix3.Identity;
                m_groupColours[i] = ColourF.White;
                m_fullColours[i] = m_baseColour;
                m_materialOverrides[i] = null;
            }
            m_animation = null;
            m_animTime = 0.0f;
            m_offscreen = false;
        }

        public bool SetGroupVisible(string groupName, bool visible)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                bool wasVisible = m_visibility[index];
                m_visibility[index] = visible;
                return wasVisible;
            }
            return false;
        }

        public void SetGroupTransform(string groupName, in Matrix4 transform)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_groupTransforms[index] = transform;
				m_fullTransforms[index] = m_baseTransform.ToWorld(transform);
            }
        }

        public void SetGroupUVTransform(string groupName, in Matrix3 uvTransform)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_uvTransforms[index] = uvTransform;
            }
        }

        public void SetGroupColour(string groupName, ColourF colour)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_groupColours[index] = colour;
                m_fullColours[index] = colour * m_baseColour;
            }
        }

        public void SetGroupMaterialOverride(string groupName, IMaterial material)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_materialOverrides[index] = material;
            }
        }

        public void FrustumCull(Camera camera, UnitVector3 lightDir)
        {
            if (m_baseVisible)
            {
                var sphere = m_model.BoundingSphere;
                sphere.Center = Transform.ToWorldPos(sphere.Center);
                m_offscreen = camera.ViewFrustum.Classify(sphere) >= 0.0f;
                m_shadowOffscreen = m_offscreen && camera.ViewFrustum.ClassifyShadow(sphere, lightDir) >= 0.0f;
            }
        }

        public void Animate()
        {
            if (m_baseVisible && !m_offscreen && m_animation != null)
            {
                for (int i = 0; i < m_model.GroupCount; ++i)
                {
                    var partName = m_model.GetGroupName(i);
                    m_animation.Animate(partName, m_animTime, out m_visibility[i], out m_groupTransforms[i], out m_uvTransforms[i], out m_groupColours[i]);
					m_fullTransforms[i] = m_baseTransform.ToWorld(m_groupTransforms[i]);
                    m_fullColours[i] = (m_groupColours[i].ToLinear() * m_baseColour.ToLinear()).ToSRGB();
                }
            }
        }

        public void Draw(IRenderer renderer, ModelEffectHelper effect)
        {
            if (m_baseVisible && !m_offscreen)
            {
                m_model.Draw(renderer, effect, m_fullTransforms, m_visibility, m_uvTransforms, m_fullColours, m_materialOverrides);
            }
        }

        public void DrawShadows(IRenderer renderer, ModelShadowEffectHelper effect)
        {
            if (m_baseVisible && !m_shadowOffscreen)
            {
                m_model.DrawShadows(renderer, effect, m_fullTransforms, m_visibility);
            }
        }
    }
}

