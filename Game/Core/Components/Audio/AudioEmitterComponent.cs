using System;
using Dan200.Core.Animation;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Core.Audio;
using Dan200.Core.Math;
using Dan200.Core.Main;
using Dan200.Core.Components.Physics;
using Dan200.Core.Serialisation;
using Dan200.Core.Components.Core;

namespace Dan200.Core.Components.Audio
{
    internal struct AudioEmmitterComponentData
    {
        [Range(Min = 0.0f)]
        [Optional(Default = 0.0f)]
        public float MinRange;

        [Range(Min = 0.0f)]
        public float MaxRange;
    }

    [RequireSystem(typeof(AudioSystem))]
	[RequireComponent(typeof(TransformComponent))]
    internal class AudioEmitterComponent : Component<AudioEmmitterComponentData>, IUpdate, IDebugDraw
    {
        private TransformComponent m_transform;
        private AudioEmitter m_emitter;

        protected override void OnInit(in AudioEmmitterComponentData properties)
        {
            var audio = Level.GetSystem<AudioSystem>().Audio;
            m_transform = Entity.GetComponent<TransformComponent>();
            m_emitter = new AudioEmitter(audio);
			m_emitter.Position = m_transform.Position;
            m_emitter.Velocity = m_transform.Velocity;
            m_emitter.MinRange = properties.MinRange;
            m_emitter.MaxRange = properties.MaxRange;
        }

        protected override void OnShutdown()
        {
            m_emitter.Dispose();
        }

        public void Update(float dt)
        {
			m_emitter.Position = m_transform.Position;
            m_emitter.Velocity = m_transform.Velocity;
            m_emitter.Update();
        }

        public void DebugDraw()
        {
            App.DebugDraw.DrawSphere(m_emitter.Position, m_emitter.MaxRange, Colour.Magenta);
        }

		public ISoundPlayback PlaySound(Sound sound, bool looping=false, AudioCategory category=AudioCategory.Sound)
        {
            return m_emitter.PlaySound(sound, looping);
        }

        public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate, AudioCategory category=AudioCategory.Sound)
        {
            return m_emitter.PlayCustom(source, channels, sampleRate);
        }
    }
}
