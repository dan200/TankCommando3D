
#if GLES
using OpenTK.Graphics.ES20;
#else
using Dan200.Core.Render.OpenGL;
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render
{
	internal class BitmapTexture : ITexture, IOpenGLTexture
    {
        private int m_texture;

        private Bitmap m_bitmap;
        private bool m_filter;
        private bool m_wrap;

		public int TextureID
        {
            get
            {
                return m_texture;
            }
        }

        public Bitmap Bitmap
        {
            get
            {
                return m_bitmap;
            }
        }

        public int Width
        {
            get
            {
                return m_bitmap.Width;
            }
        }

        public int Height
        {
            get
            {
                return m_bitmap.Height;
            }
        }

        public bool Filter
        {
            get
            {
                return m_filter;
            }
            set
            {
                if (m_filter != value)
                {
                    m_filter = value;
                    GL.BindTexture(TextureTarget.Texture2D, m_texture);
                    GLUtils.SetParameters(m_filter, m_wrap);
                }
            }
        }

        public bool Wrap
        {
            get
            {
                return m_wrap;
            }
            set
            {
                if (m_wrap != value)
                {
                    m_wrap = value;
                    GL.BindTexture(TextureTarget.Texture2D, m_texture);
                    GLUtils.SetParameters(m_filter, m_wrap);
                }
            }
        }

        public BitmapTexture(Bitmap bitmap)
        {
            m_filter = false;
            m_wrap = true;
            m_bitmap = bitmap;
            m_texture = GLUtils.CreateTextureFromBitmap(bitmap, m_filter, m_wrap);
        }

        public void Dispose()
        {
            GL.DeleteTextures(1, ref m_texture);
            m_texture = -1;
			m_bitmap = null;
        }

        public void Update()
        {
            GLUtils.UpdateTextureFromBitmap(m_texture, m_bitmap);
        }
    }
}

