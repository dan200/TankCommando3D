using System.Runtime.InteropServices;
using Dan200.Core.Main;
using Dan200.Core.Math;

namespace Dan200.Core.Render
{
	internal abstract class LitEffectHelper : WorldEffectHelper
    {
		private struct DirectionalLightUniforms
		{
			public int Colour;
			public int Direction;
		}

		private struct PointLightUniforms
		{
			public int Colour;
			public int Position;
			public int Range;
		}

		private unsafe struct Uniforms
        {
            public int AmbientLightColour;
			public DirectionalLightUniforms[] DirectionalLightUniforms;
			public PointLightUniforms[] PointLightUniforms;
        }
        private Uniforms m_uniforms;

        public ColourF AmbientLightColour
        {
            set
            {
                Instance.SetUniform(m_uniforms.AmbientLightColour, value);
            }
        }

		internal unsafe class DirectionalLightCollection
		{
			private LitEffectHelper m_owner;

			public int Count
			{
				get
				{
					return m_owner.m_uniforms.DirectionalLightUniforms.Length;
				}
			}

			public DirectionalLight this[int index]
			{
				set
				{
					App.Assert(index >= 0 && index < m_owner.m_uniforms.DirectionalLightUniforms.Length);
					ref DirectionalLightUniforms uniforms = ref m_owner.m_uniforms.DirectionalLightUniforms[index];
					m_owner.Instance.SetUniform(uniforms.Colour, value.Colour);
					m_owner.Instance.SetUniform(uniforms.Direction, value.Direction);
				}
			}

			public DirectionalLightCollection(LitEffectHelper owner)
			{
				m_owner = owner;
			}
		}

		public DirectionalLightCollection DirectionalLights
		{
			get
			{
				return new DirectionalLightCollection(this);
			}
		}

		internal unsafe class PointLightCollection
		{
			private LitEffectHelper m_owner;

			public int Count
			{
				get
				{
					return m_owner.m_uniforms.PointLightUniforms.Length;
				}
			}

			public PointLight this[int index]
			{
				set
				{
					App.Assert(index >= 0 && index < m_owner.m_uniforms.PointLightUniforms.Length);
					ref PointLightUniforms uniforms = ref m_owner.m_uniforms.PointLightUniforms[index];
					m_owner.Instance.SetUniform(uniforms.Colour, value.Colour);
					m_owner.Instance.SetUniform(uniforms.Position, value.Position);
					m_owner.Instance.SetUniform(uniforms.Range, value.Range);
				}
			}

			public PointLightCollection(LitEffectHelper owner)
			{
				m_owner = owner;
			}
		}

		public PointLightCollection PointLights
		{
			get
			{
				return new PointLightCollection(this);
			}
		}

		protected LitEffectHelper(IRenderer renderer, Effect effect, ShaderDefines defines) : base(renderer, effect, defines)
        {
			int numPointLights = defines.Get("NUM_POINT_LIGHTS") ?? 0;
			int numDirectionalLights = defines.Get("NUM_DIRECTIONAL_LIGHTS") ?? 0;
			m_uniforms.PointLightUniforms = new PointLightUniforms[numPointLights];
			m_uniforms.DirectionalLightUniforms = new DirectionalLightUniforms[numDirectionalLights];
			LookupUniforms();
        }

		protected override unsafe void LookupUniforms()
        {
            base.LookupUniforms();

            m_uniforms.AmbientLightColour = Instance.GetUniformLocation("ambientLightColour");
			if (m_uniforms.DirectionalLightUniforms != null)
			{
				for (int i = 0; i < m_uniforms.DirectionalLightUniforms.Length; ++i)
				{
					ref DirectionalLightUniforms uniforms = ref m_uniforms.DirectionalLightUniforms[i];
					uniforms.Colour = Instance.GetUniformLocation("directional_lights[" + i + "].colour");
					uniforms.Direction = Instance.GetUniformLocation("directional_lights[" + i + "].direction");
				}
			}
			if (m_uniforms.PointLightUniforms != null)
			{
				for (int i = 0; i < m_uniforms.PointLightUniforms.Length; ++i)
				{
					ref PointLightUniforms uniforms = ref m_uniforms.PointLightUniforms[i];
					uniforms.Colour = Instance.GetUniformLocation("point_lights[" + i + "].colour");
					uniforms.Position = Instance.GetUniformLocation("point_lights[" + i + "].position");
					uniforms.Range = Instance.GetUniformLocation("point_lights[" + i + "].range");
				}
			}
        }
    }
}
