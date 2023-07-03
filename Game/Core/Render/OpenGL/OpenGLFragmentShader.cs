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
	internal class OpenGLFragmentShader : OpenGLShaderBase
	{		
        public static OpenGLFragmentShader Get(string path)
        {
            return Assets.Assets.Get<OpenGLFragmentShader>(path);
        }

		public static object LoadData(Stream stream, string path)
		{
			return LoadData(stream, path, ShaderType.Fragment);
		}

		public OpenGLFragmentShader(string path, object data) : base(path, data, ShaderType.Fragment)
		{
		}
    }
}
