using Dan200.Core.Assets;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Dan200.Core.Render
{
	internal enum ShaderType
	{
		Vertex,
		Fragment,
		Include,
	}

    internal abstract class Shader : IBasicAsset
    {
		public abstract string Path { get; }
        public abstract ShaderType Type { get; }

        public abstract void Reload(object data);
		public abstract void Dispose();
    }
}
