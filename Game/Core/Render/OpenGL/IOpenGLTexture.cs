using System;

namespace Dan200.Core.Render.OpenGL
{
	internal interface IOpenGLTexture : ITexture
	{
		int TextureID { get; }
	}
}
