
using System;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal class Image : Element
    {
        private ITexture m_texture;
        private Quad m_region;
        private Colour m_colour;
        private bool m_stretch;
        private bool m_disposeTexture;

        public ITexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                if (m_texture != value)
                {
                    m_texture = value;
                    RequestRebuild();
                }
            }
        }

        public Quad Region
        {
            get
            {
                return m_region;
            }
            set
            {
                m_region = value;
                RequestRebuild();
            }
        }

        public Colour Colour
        {
            get
            {
                return m_colour;
            }
            set
            {
                m_colour = value;
            }
        }

        public bool Stretch
        {
            get
            {
                return m_stretch;
            }
            set
            {
                m_stretch = value;
                RequestRebuild();
            }
        }

        public Image(ITexture texture, float width, float height, bool disposeTexture = false) : this(texture, Quad.UnitSquare, width, height, disposeTexture)
        {
        }

        public Image(ITexture texture, Quad region, float width, float height, bool disposeTexture = false)
        {
            m_texture = texture;
            m_disposeTexture = disposeTexture;
            m_colour = Colour.White;
            m_region = region;
            m_stretch = false;
            Size = new Vector2(width, height);
        }

        public override void Dispose()
        {
            base.Dispose();
			if (m_disposeTexture && m_texture is IDisposable)
            {
				((IDisposable)m_texture).Dispose();
            }
            m_texture = null;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

		protected override void OnRebuild(GUIBuilder builder)
        {
            var origin = Position;
            if (m_stretch)
            {
				builder.AddQuad(
					origin,
					origin + Size,
					m_texture,
					m_region,
					m_colour
				);
            }
            else
            {
                float aspect = Width / Height;
                float textureAspect = (m_region.Width * m_texture.Width) / (m_region.Height * m_texture.Height);
                if (textureAspect > aspect)
                {
                    float cutoff = ((textureAspect - aspect) * 0.5f) / textureAspect;
                    builder.AddQuad(
                        origin,
						origin + Size,
						m_texture,
                        m_region.Sub(cutoff, 0.0f, 1.0f - 2.0f * cutoff, 1.0f),
						m_colour
                    );
                }
                else if (textureAspect < aspect)
                {
                    float invAspect = 1.0f / aspect;
                    float invTextureAspect = 1.0f / textureAspect;
                    float cutoff = ((invTextureAspect - invAspect) * 0.5f) / invTextureAspect;
					builder.AddQuad(
                        origin,
						origin + Size,
						m_texture,
						m_region.Sub(0.0f, cutoff, 1.0f, 1.0f - 2.0f * cutoff),
						m_colour
                    );
                }
                else
                {
					builder.AddQuad(
                        origin,
						origin + Size,
						m_texture,
                        m_region,
						m_colour
                    );
                }
            }
        }
    }
}

