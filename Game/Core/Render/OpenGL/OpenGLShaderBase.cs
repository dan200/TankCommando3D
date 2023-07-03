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
	internal abstract class OpenGLShaderBase : Shader
	{
		internal class CompiledShader
		{
			public int ShaderID;
		}

		private struct ShaderPart
		{
			public string Text;
			public bool IsIncludePath;

			public ShaderPart(string text, bool isIncludePath)
			{
				Text = text;
				IsIncludePath = isIncludePath;
			}
		}

		private readonly string m_path;
		private readonly ShaderType m_type;
		private readonly Dictionary<ShaderDefines, CompiledShader> m_compiledShaders;
		private ShaderPart[] m_parts;
		private string[] m_compiledParts;
		private HashSet<string> m_compiledDependencies;

		public override string Path
		{
			get
			{
				return m_path;
			}
		}

        public override ShaderType Type
        {
            get
            {
                return m_type;
            }
        }

		protected static object LoadData(Stream stream, string path, ShaderType type)
		{
			return Preprocess(stream, path, type);
		}

		protected OpenGLShaderBase(string path, object data, ShaderType type)
		{
			m_path = path;
			m_type = type;
			m_compiledShaders = new Dictionary<ShaderDefines, CompiledShader>();
			Load(data);
			Assets.Assets.OnAssetsReloaded += OnAssetsReloaded;
		}

		public override void Reload(object data)
		{
			Unload();
			Load(data);
		}

		public override void Dispose()
		{
			Assets.Assets.OnAssetsReloaded -= OnAssetsReloaded;
			Unload();
		}

		private void OnAssetsReloaded(AssetLoadEventArgs e)
		{
			bool recompile = false;
			if (m_compiledDependencies != null)
			{
				foreach (var dependency in m_compiledDependencies)
				{
					if (e.Paths.Contains(dependency))
					{
						recompile = true;
						break;
					}
				}
			}
			if (recompile)
			{
				m_compiledParts = null;
				m_compiledDependencies = null;
				foreach (var pair in m_compiledShaders)
				{
					var defines = pair.Key;
					var shader = pair.Value;
					GL.DeleteShader(shader.ShaderID);
					shader.ShaderID = -1;
					Compile(pair.Key);
				}
			}
		}

		private static ShaderPart[] Preprocess(Stream stream, string path, ShaderType type)
		{
			var dirPath = AssetPath.GetDirectoryName(path);
			var parts = new List<ShaderPart>();
			var currentPart = new StringBuilder();

			// Add the source
			int lineNumber = 0;
			int fileNumber = 0;
			currentPart.AppendLine("#line " + lineNumber + " " + fileNumber);
			var reader = new StreamReader(stream, Encoding.UTF8);
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (line.StartsWith("#include \"", StringComparison.InvariantCulture) &&
					line.EndsWith("\"", StringComparison.InvariantCulture))
				{
					// #include line:
					// Finish the previous part
					parts.Add(new ShaderPart(currentPart.ToString(), false));
					currentPart.Clear();

					// Add the #include
					var pathStart = line.IndexOf('"') + 1;
					var pathEnd = line.LastIndexOf('"');
					App.Assert(pathStart >= 0 && pathEnd >= pathStart);
					var fullPath = AssetPath.Combine(dirPath, line.Substring(pathStart, pathEnd - pathStart));
					parts.Add(new ShaderPart(fullPath, true));

					// Start the new part
					currentPart.AppendLine("#line " + lineNumber + " " + fileNumber);
				}
				else
				{
					// Normal line:
					currentPart.AppendLine(line);
				}
				++lineNumber;
			}

			// Finish the final part
			parts.Add(new ShaderPart(currentPart.ToString(), false));
			return parts.ToArray();
		}

		private void AddHeaderParts(List<string> o_parts, ShaderDefines defines)
		{
			o_parts.Add("#version 140\n");
			o_parts.Add(defines.ToString());
#if GLES
            o_parts.Add("#define GLES\n");
			if(m_type == ShaderType.Vertex)
			{
				o_parts.Add("precision highp float;\n");
			}
			else
			{
				o_parts.Add("precision mediump float;\n");
			}
#endif
		}

		private void AddParts(List<string> o_parts, HashSet<string> o_dependencies)
		{
			foreach (var part in m_parts)
			{
				if (part.IsIncludePath)
				{
					var shader = OpenGLShaderInclude.Get(part.Text);
					o_dependencies.Add(part.Text);
					shader.AddParts(o_parts, o_dependencies);
				}
				else
				{
					o_parts.Add(part.Text);
				}
			}
		}

		public CompiledShader Compile(ShaderDefines defines)
		{
			App.Assert(m_type != ShaderType.Include, "Only vertex and fragment shaders can be compiled directly");
			CompiledShader result;
			if (!m_compiledShaders.TryGetValue(defines, out result) || result.ShaderID < 0)
			{
				// Get the part list
				if (m_compiledParts == null)
				{
					// First compile: build the part list
					var parts = new List<string>();
					var dependencies = new HashSet<string>();
					AddHeaderParts(parts, defines);
					AddParts(parts, dependencies);
					m_compiledParts = parts.ToArray();
					m_compiledDependencies = dependencies;
				}
				else
				{
					// Subsequent compile: just change the defines
					m_compiledParts[1] = defines.ToString();
				}

				// Create the shader
				int shaderID;
				switch (m_type)
				{
					case ShaderType.Fragment:
						shaderID = GL.CreateShader(GLShaderType.FragmentShader);
						break;
					case ShaderType.Vertex:
						shaderID = GL.CreateShader(GLShaderType.VertexShader);
						break;
					default:
						throw new Exception();
				}

				// Compile the shader
				unsafe
				{
					GL.ShaderSource(shaderID, m_compiledParts.Length, m_compiledParts, (int*)0);
				}
				GL.CompileShader(shaderID);
				CheckCompileResult(shaderID);

				// Store the shader
				if (result == null)
				{
					result = new CompiledShader();
					m_compiledShaders.Add(defines, result);
				}
				result.ShaderID = shaderID;
			}
			return result;
		}

        private void Load(object data)
        {
			// Store shader (don't compile until needed)
			var parts = (ShaderPart[])data;
			m_parts = parts;
        }

        private void Unload()
        {
			foreach(var pair in m_compiledShaders)
			{
				GL.DeleteShader(pair.Value.ShaderID);
				GLUtils.CheckError();
			}
			m_compiledShaders.Clear();
			m_compiledParts = null;
			m_compiledDependencies = null;
        }

        private static Regex s_atiErrorRegex = new Regex("^ERROR: ([0-9]+):", RegexOptions.Compiled);
        private static Regex s_nvidiaErrorRegex = new Regex("^([0-9]+)\\(", RegexOptions.Compiled);

        private void ReplaceFilePaths(string[] io_lines, string[] paths)
        {
            for (int i = 0; i < io_lines.Length; ++i)
            {
                var line = io_lines[i];
                var match = s_atiErrorRegex.Match(line) ?? s_nvidiaErrorRegex.Match(line);
                if (match != null && match.Success)
                {
                    var group = match.Groups[1];
                    var fileNumber = int.Parse(group.Value);
                    if (fileNumber >= 0 && fileNumber < paths.Length)
                    {
                        var filePath = paths[fileNumber];
                        line = line.Substring(0, group.Index) + filePath + line.Substring(group.Index + group.Length);
                    }
                }
                io_lines[i] = line;
            }
        }

        private void CheckCompileResult(int shader)
        {
            int status = 0;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                var logLines = GL.GetShaderInfoLog(shader).Split('\r', '\n');
                ReplaceFilePaths(logLines, new string[] { m_path });
                throw new OpenGLException("Errors compiling shader:\n" + string.Join(Environment.NewLine, logLines));
            }
            GLUtils.CheckError();
        }
    }
}
