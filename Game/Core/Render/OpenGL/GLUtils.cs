using Dan200.Core.Main;
using System;
using Dan200.Core.Math;
using System.IO;
using System.Reflection;
using System.Diagnostics;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render.OpenGL
{
    internal class OpenGLException : Exception
    {
        public OpenGLException(ErrorCode error) : this(error.ToString())
        {
        }

        public OpenGLException(string message) : base(message)
        {
        }
    }

    internal static class GLUtils
    {
        [Conditional("DEBUG_OPENGL")]
        public static void CheckError()
        {
            if (App.Debug)
            {
#if GLES
				var error = GL.GetErrorCode();
#else
                var error = GL.GetError();
#endif
                if (error != ErrorCode.NoError)
                {
                    throw new OpenGLException(error);
                }
            }
        }

		public static int GenTexture()
		{
			int texture;
			GL.GenTextures(1, out texture);
			GLUtils.CheckError();
			return texture;
		}

		public static int CreateBlankTexture(int width, int height, bool filter, bool wrap, ColourSpace colourSpace)
		{
			int texture = GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
			ClearTexture(width, height, colourSpace);
			SetParameters(filter, wrap);
			GLUtils.CheckError();
			return texture;
		}

		public static void ClearTexture(int width, int height, ColourSpace colourSpace)
		{
			var internalFormat = (colourSpace == ColourSpace.SRGB) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
			GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GLUtils.CheckError();
		}

		public static int CreateTextureFromBitmap(Bitmap bitmap, bool filter, bool wrap)
		{
			int texture = GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
			UpdateTextureFromBitmap(texture, bitmap);
			SetParameters(filter, wrap);
			return texture;
		}

		public static unsafe void UpdateTextureFromBitmap(int tex, Bitmap bitmap)
		{
			GL.BindTexture(TextureTarget.Texture2D, tex);
			using (var bits = bitmap.Lock())
			{
				try
				{
#if !GLES
					GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
					GL.PixelStore(PixelStoreParameter.UnpackRowLength, bits.Stride / bits.BytesPerPixel);
#endif
					var format = (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb;
					var internalFormat = (bits.ColourSpace == ColourSpace.SRGB) ?
						((bits.BytesPerPixel == 4) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Srgb8) :
						((bits.BytesPerPixel == 4) ? PixelInternalFormat.Rgba8 : PixelInternalFormat.Rgb8);
					GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, bits.Width, bits.Height, 0, format, PixelType.UnsignedByte, new IntPtr(bits.Data));
					GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
				}
				finally
				{
					GLUtils.CheckError();
				}
			}
		}

		public static void SetParameters(bool filter, bool wrap)
		{
			GL.TexParameter(
				TextureTarget.Texture2D,
				TextureParameterName.TextureMinFilter,
				filter ? (int)TextureMinFilter.LinearMipmapLinear : (int)TextureMinFilter.Nearest
			);
			GL.TexParameter(
				TextureTarget.Texture2D,
				TextureParameterName.TextureMagFilter,
				filter ? (int)TextureMagFilter.Linear : (int)TextureMagFilter.Nearest
			);
			GL.TexParameter(
				TextureTarget.Texture2D,
				TextureParameterName.TextureWrapS,
				wrap ? (int)TextureWrapMode.Repeat : (int)TextureWrapMode.ClampToEdge
			);
			GL.TexParameter(
				TextureTarget.Texture2D,
				TextureParameterName.TextureWrapT,
				wrap ? (int)TextureWrapMode.Repeat : (int)TextureWrapMode.ClampToEdge
			);
			GLUtils.CheckError();
		}

		private static int RoundUp(int n, int multiple)
		{
			return ((n + multiple - 1) / multiple) * multiple;
		}

		public static int Std140SizeOf(Type type)
		{
			if (type == typeof(float) || type == typeof(int))
			{
				return 4;
			}
			else if (type == typeof(Vector2) || type == typeof(UnitVector2))
			{
				return 8;
			}
			else if (type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(UnitVector3) || type == typeof(UnitVector4))
			{
				return 16;
			}
            else if (type == typeof(Matrix3))
            {
                return 48;
            }
            else if (type == typeof(Matrix4))
			{
				return 64;
			}
			else
			{
				throw new InvalidDataException(string.Format(
					"Unsupported type {0} for Uniform", type.Name
				));
			}
		}

		public static int Std140ArraySizeOf(Type type, int arraySize)
		{
			if (type == typeof(Matrix3))
			{
				return Std140ArraySizeOf(typeof(Vector3), 3 * arraySize);
			}
			else if (type == typeof(Matrix4))
			{
				return Std140ArraySizeOf(typeof(Vector4), 4 * arraySize);
			}
			else
			{
				return RoundUp(Std140SizeOf(type), 16) * arraySize;
			}
		}
	}
}

