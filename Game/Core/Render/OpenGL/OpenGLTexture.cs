using System;
using System.IO;
using Dan200.Core.Assets;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render.OpenGL
{
	internal class OpenGLTexture : Texture, IOpenGLTexture
	{
		private string m_path;
		private int m_texture;
		private int m_width;
		private int m_height;
		private bool m_filter;
		private bool m_wrap;

		public override string Path
		{
			get
			{
				return m_path;
			}
		}

		public override int Width
		{
			get
			{
				return m_width;
			}
		}

		public override int Height
		{
			get
			{
				return m_height;
			}
		}

		public override bool Filter
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

		public override bool Wrap
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

		public int TextureID
		{
			get
			{
				return m_texture;
			}
		}

		public static object LoadData(Stream stream, string path)
		{
			var bitmap = new Bitmap(stream);
			if (path.EndsWith(".norm.png", StringComparison.Ordinal))
			{
				bitmap.ColourSpace = ColourSpace.Linear;
			}
			return bitmap;
		}

		public OpenGLTexture(string path, object data)
		{
			m_path = path;
			m_filter = false;
			m_wrap = true;
			Load(data);
		}

		public override void Reload(object data)
		{
			Unload();
			Load(data);
		}

		public override void Dispose()
		{
			Unload();
		}

		private void Load(object data)
		{
			var bitmap = (Bitmap)data;
			m_texture = GLUtils.CreateTextureFromBitmap(bitmap, m_filter, m_wrap);
			m_width = bitmap.Width;
			m_height = bitmap.Height;
		}

		private void Unload()
		{
			GL.DeleteTextures(1, ref m_texture);
			m_texture = -1;
		}
	}
}

