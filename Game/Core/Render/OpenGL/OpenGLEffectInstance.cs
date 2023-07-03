using Dan200.Core.Assets;
using System;
using System.Linq;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Render.OpenGL;
using Dan200.Core.Util;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

using Dan200.Core.Math;

namespace Dan200.Core.Render.OpenGL
{
	internal class OpenGLEffectInstance : EffectInstance
    {
        private Effect m_effect;
		private ShaderDefines m_defines;
        private int m_program;
        private Dictionary<string, int> m_knownUniformLocations;
		private Dictionary<string, int> m_knownUniformBlockIndices;
        private Dictionary<string, int> m_knownAttributeLocations;
		internal Dictionary<int, int> m_textureUnits; // location -> unit
		internal Dictionary<int, int> m_uniformBlockUnits; // location -> unit
        internal List<ITexture> m_textures;
		internal List<IUniformBlock> m_uniformBlocks;
		private bool m_active;

        public override Effect Effect
        {
            get
            {
                return m_effect;
            }
        }

		public int ProgramID
        {
            get
            {
                return m_program;
            }
        }

		public bool Active
		{
			get
			{
				return m_active;
			}
			set
			{
				m_active = value;
			}
		}

		public override event StructEventHandler<EffectInstance> OnUniformLocationsChanged;

		public OpenGLEffectInstance(Effect effect, ShaderDefines defines)
        {
            m_effect = effect;
			m_defines = defines;
            m_knownUniformLocations = new Dictionary<string, int>();
			m_knownUniformBlockIndices = new Dictionary<string, int>();
            m_knownAttributeLocations = new Dictionary<string, int>();
            m_textureUnits = new Dictionary<int, int>();
			m_uniformBlockUnits = new Dictionary<int, int>();
            m_textures = new List<ITexture>();
			m_uniformBlocks = new List<IUniformBlock>();

            Assets.Assets.OnAssetsReloaded += OnAssetsReloaded;
            Assets.Assets.OnAssetsUnloaded += OnAssetsUnloaded;
            Load();
        }

        public override void Dispose()
        {
            Unload();
            Assets.Assets.OnAssetsReloaded -= OnAssetsReloaded;
            Assets.Assets.OnAssetsUnloaded -= OnAssetsUnloaded;
        }

        private void OnAssetsReloaded(AssetLoadEventArgs e)
        {
            if (e.Paths.Contains(m_effect.Path) ||
                e.Paths.Contains(m_effect.VertexShaderPath) ||
                e.Paths.Contains(m_effect.FragmentShaderPath))
            {
                Unload();
                Load();
				if (OnUniformLocationsChanged != null)
				{
					OnUniformLocationsChanged.Invoke(this, StructEventArgs.Empty);
				}
            }
        }

        private void OnAssetsUnloaded(AssetLoadEventArgs e)
        {
            if (e.Paths.Contains(m_effect.Path) ||
                e.Paths.Contains(m_effect.VertexShaderPath) ||
                e.Paths.Contains(m_effect.FragmentShaderPath))
            {
                throw new InvalidOperationException(string.Format("Unloaded effect {0} while in use", m_effect.Path));
            }
        }

        private void Load()
        {
			var defines = m_defines;
			var vertex = OpenGLVertexShader.Get(m_effect.VertexShaderPath).Compile(defines);
			var fragment = OpenGLFragmentShader.Get(m_effect.FragmentShaderPath).Compile(defines);
            m_program = GL.CreateProgram();

			GL.AttachShader(m_program, vertex.ShaderID);
			GL.AttachShader(m_program, fragment.ShaderID);
            GL.LinkProgram(m_program);
            GLUtils.CheckError();

            CheckLinkResult(m_program);
            GLUtils.CheckError();
        }

        private void Unload()
        {
            GL.DeleteProgram(m_program);
            m_knownUniformLocations.Clear();
			m_knownUniformBlockIndices.Clear();
            m_knownAttributeLocations.Clear();
            m_textureUnits.Clear();
            m_uniformBlockUnits.Clear();
            m_textures.Clear();
            m_uniformBlocks.Clear();
            GLUtils.CheckError();
        }

        public override int GetUniformLocation(string name)
        {
            int result;
            if (!m_knownUniformLocations.TryGetValue(name, out result))
            {
				result = GL.GetUniformLocation(m_program, name);
                m_knownUniformLocations[name] = result;
                GLUtils.CheckError();
            }
            return result;
        }

		public override int GetUniformBlockLocation(string name)
		{
			int result;
			if (!m_knownUniformBlockIndices.TryGetValue(name, out result))
			{
				result = GL.GetUniformBlockIndex(m_program, name);
				m_knownUniformBlockIndices[name] = result;
				GLUtils.CheckError();
			}
			return result;
		}

        public override int GetAttributeLocation(string name)
        {
            int result;
            if (!m_knownAttributeLocations.TryGetValue(name, out result))
            {
				result = GL.GetAttribLocation(m_program, name);
                m_knownAttributeLocations[name] = result;
                GLUtils.CheckError();
            }
            return result;
        }

        public override void SetUniform(int location, int value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                GL.Uniform1(location, value);
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, float value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                GL.Uniform1(location, value);
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Vector2 value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                GL.Uniform2(location, value.X, value.Y);
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Vector3 value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                GL.Uniform3(location, value.X, value.Y, value.Z);
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Vector4 value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
                GLUtils.CheckError();
            }
        }

		public override void SetUniform(int location, ColourF value, ColourSpace space = ColourSpace.SRGB)
        {
            if (location >= 0)
            {
                var linearValue = (space == ColourSpace.SRGB) ? value.ToLinear() : value;
                GL.UseProgram(m_program);
                GL.Uniform4(location, linearValue.R, linearValue.G, linearValue.B, linearValue.A);
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, ref Matrix3 value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Matrix3* pvalue = &value)
                    {
                        GL.UniformMatrix3(location, 1, false, (float*)pvalue);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, ref Matrix4 value)
        {
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Matrix4* pvalue = &value)
                    {
                        GL.UniformMatrix4(location, 1, false, (float*)pvalue);
                    }
                }
                GLUtils.CheckError();
            }
        }

		public override void SetUniform(int location, int[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (int* pvalue = value)
                    {
                        GL.Uniform1(location, count, pvalue + start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, float[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (float* pvalue = value)
                    {
                        GL.Uniform1(location, count, pvalue + start);
                    }
                }
                GLUtils.CheckError();
            }
        }


        public override void SetUniform(int location, Vector2[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Vector2* pvalue = value)
                    {
                        GL.Uniform2(location, count, (float*)pvalue + 2 * start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Vector3[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Vector3* pvalue = value)
                    {
                        GL.Uniform3(location, count, (float*)pvalue + 3 * start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Vector4[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Vector4* pvalue = value)
                    {
                        GL.Uniform4(location, count, (float*)pvalue + 4 * start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, ColourF[] value, int start, int count, ColourSpace space = ColourSpace.SRGB)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (ColourF* pValue = value)
                    {
                        if (space == ColourSpace.SRGB)
                        {
                            ColourF* pLinearValue = stackalloc ColourF[count];
                            for(int i=0; i<count; ++i)
                            {
                                pLinearValue[i] = pValue[i + start].ToLinear();
                            }
                            GL.Uniform4(location, count, (float*)pLinearValue);
                        }
                        else
                        {
                            GL.Uniform4(location, count, (float*)(pValue + start));
                        }
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Matrix3[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Matrix3* pvalue = value)
                    {
                        GL.UniformMatrix3(location, count, false, (float*)pvalue + 9 * start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, Matrix4[] value, int start, int count)
        {
            App.Assert(start >= 0 && count >= 0 && start + count <= value.Length);
            if (location >= 0)
            {
                GL.UseProgram(m_program);
                unsafe
                {
                    fixed (Matrix4* pvalue = value)
                    {
                        GL.UniformMatrix4(location, count, false, (float*)pvalue + 16 * start);
                    }
                }
                GLUtils.CheckError();
            }
        }

        public override void SetUniform(int location, ITexture texture)
        {
            if (location >= 0)
            {
                // Get the texture unit for this uniform
                int unit;
                if (m_textureUnits.TryGetValue(location, out unit))
                {
                    // Set the texture
                    if (m_textures[unit] == texture)
                    {
                        return;
                    }
                    m_textures[unit] = texture;
                }
                else
                {
                    // Assign a new texture unit and set the texture
                    unit = m_textures.Count;
                    m_textureUnits.Add(location, unit);
                    m_textures.Add(texture);

                    // Set the uniform
                    GL.UseProgram(m_program);
                    GL.Uniform1(location, unit);
                    GLUtils.CheckError();
                }

                // Bind the texture
                if (m_active)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + unit);
					GL.BindTexture(TextureTarget.Texture2D, ((IOpenGLTexture)texture).TextureID);
                    GLUtils.CheckError();
                }
            }
        }

		public override void SetUniformBlock(int location, IUniformBlock block)
		{
			if (location >= 0)
			{
				// Get the uniform block unit for this uniform block
				int unit;
				if (m_uniformBlockUnits.TryGetValue(location, out unit))
                {
                    // Set the uniform block
					if (m_uniformBlocks[unit] == block)
                    {
                        return;
                    }
					m_uniformBlocks[unit] = block;
                }
                else
                {
                    // Assign a new uniform block unit and set the uniform block
					unit = m_uniformBlocks.Count;
					m_uniformBlockUnits.Add(location, unit);
					m_uniformBlocks.Add(block);

                    // Set the uniform
                    GL.UseProgram(m_program);
					GL.UniformBlockBinding(m_program, location, unit);
                    GLUtils.CheckError();
                }

				// Bind the uniform
				if (m_active)
				{
					GL.BindBuffer(BufferTarget.UniformBuffer, block.GLUniformBuffer);
					GL.BindBufferBase(BufferTarget.UniformBuffer, unit, block.GLUniformBuffer);
					GLUtils.CheckError();
				}
			}
		}

        private static void CheckLinkResult(int program)
        {
            int status = 0;
            GL.GetProgram(program, ProgramParameter.LinkStatus, out status);
            if (status == 0)
            {
                var log = GL.GetProgramInfoLog(program);
                throw new OpenGLException("Errors linking shader:\n" + log);
            }
            GLUtils.CheckError();
        }
    }
}
