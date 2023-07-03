using Dan200.Core.Math;


namespace Dan200.Core.Render
{
    internal class ParticleEffectHelper : WorldEffectHelper
    {
        private struct Uniforms
        {
            public int ModelMatrix;
            public int Time;
            public int StopTime;
            public int Texture;
            public int Lifetime;
            public int EmitterRate;
            public int EmitterPos;
            public int EmitterPosRange;
            public int EmitterVel;
            public int EmitterVelRange;
            public int Gravity;
            public int InitialRadius;
            public int FinalRadius;
            public int InitialColour;
            public int FinalColour;
        }
        private Uniforms m_uniforms;

        public Matrix4 ModelMatrix
        {
            set
            {
                Instance.SetUniform(m_uniforms.ModelMatrix, ref value);
            }
        }

        public float Time
        {
            set
            {
                Instance.SetUniform(m_uniforms.Time, value);
            }
        }

        public float StopTime
        {
            set
            {
                Instance.SetUniform(m_uniforms.StopTime, value);
            }
        }

        public ParticleStyle Style
        {
            set
            {
                var style = value;
                Instance.SetUniform(m_uniforms.Texture, Texture.Get(style.Texture, false));
                Instance.SetUniform(m_uniforms.Lifetime, style.Lifetime);
                Instance.SetUniform(m_uniforms.EmitterRate, style.EmitterRate);
                Instance.SetUniform(m_uniforms.EmitterPos, style.Position);
                Instance.SetUniform(m_uniforms.EmitterPosRange, style.PositionRange);
                Instance.SetUniform(m_uniforms.EmitterVel, style.Velocity);
                Instance.SetUniform(m_uniforms.EmitterVelRange, style.VelocityRange);
                Instance.SetUniform(m_uniforms.Gravity, style.Gravity);
                Instance.SetUniform(m_uniforms.InitialRadius, style.Radius);
                Instance.SetUniform(m_uniforms.FinalRadius, style.FinalRadius);
                Instance.SetUniform(m_uniforms.InitialColour, style.Colour);
                Instance.SetUniform(m_uniforms.FinalColour, style.FinalColour);
            }
        }

        public ParticleEffectHelper(IRenderer renderer) : base(renderer, Effect.Get("shaders/particles.effect"), ShaderDefines.Empty)
        {
        }

        protected override void LookupUniforms()
        {
            base.LookupUniforms();
            m_uniforms.ModelMatrix = Instance.GetUniformLocation("modelMatrix");
            m_uniforms.Time = Instance.GetUniformLocation("time");
            m_uniforms.StopTime = Instance.GetUniformLocation("stopTime");
            m_uniforms.Texture = Instance.GetUniformLocation("particleTexture");
            m_uniforms.Lifetime = Instance.GetUniformLocation("lifetime");
            m_uniforms.EmitterRate = Instance.GetUniformLocation("emitterRate");
            m_uniforms.EmitterPos = Instance.GetUniformLocation("emitterPos");
            m_uniforms.EmitterPosRange = Instance.GetUniformLocation("emitterPosRange");
            m_uniforms.EmitterVel = Instance.GetUniformLocation("emitterVel");
            m_uniforms.EmitterVelRange = Instance.GetUniformLocation("emitterVelRange");
            m_uniforms.Gravity = Instance.GetUniformLocation("gravity");
            m_uniforms.InitialRadius = Instance.GetUniformLocation("initialRadius");
            m_uniforms.FinalRadius = Instance.GetUniformLocation("finalRadius");
            m_uniforms.InitialColour = Instance.GetUniformLocation("initialColour");
            m_uniforms.FinalColour = Instance.GetUniformLocation("finalColour");
        }
    }
}
