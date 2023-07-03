using Dan200.Core.Assets;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Dan200.Core.Render.OpenGL;
using System.Collections.Generic;
using Dan200.Core.Main;
using System.Globalization;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
using GLShaderType = OpenTK.Graphics.OpenGL.ShaderType;
#endif


namespace Dan200.Core.Render.OpenGL
{
	internal class OpenGLShaderInclude : OpenGLShaderBase
	{		
        public static OpenGLShaderInclude Get(string path)
        {
            return Assets.Assets.Get<OpenGLShaderInclude>(path);
        }

		public static object LoadData(Stream stream, string path)
		{
			return LoadData(stream, path, ShaderType.Include);
		}

		public OpenGLShaderInclude(string path, object data) : base(path, data, ShaderType.Include)
		{
		}
    }
}
