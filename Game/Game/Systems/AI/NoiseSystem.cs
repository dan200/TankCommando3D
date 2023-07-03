using System;
using System.Collections.Generic;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Game.Systems.AI
{
    struct Noise
    {
        public Entity Origin;
        public Vector3 Position;
        public float Radius;
    }

    internal struct NoiseSystemData
    {
    }

    internal class NoiseSystem : System<NoiseSystemData>, IUpdate, IDebugDraw
    {
        private List<Noise> m_newNoises;
        private List<Noise> m_previousNoises;

        public IReadOnlyCollection<Noise> RecentNoises
        {
            get
            {
                return m_previousNoises;
            }
        }

        protected override void OnInit(in NoiseSystemData properties)
        {
            m_newNoises = new List<Noise>();
            m_previousNoises = new List<Noise>();
        }

        protected override void OnShutdown()
        {
        }

        public void MakeNoise(in Noise noise)
        {
            App.Assert(noise.Origin != null);
            App.Assert(noise.Radius > 0.0f);
            m_newNoises.Add(noise);
        }

        public void Update(float dt)
        {
            var previousNoises = m_previousNoises;
            m_previousNoises = m_newNoises;
            m_newNoises = m_previousNoises;
            m_newNoises.Clear();
        }

        public void DebugDraw()
        {
            foreach(var noise in m_previousNoises)
            {
                App.DebugDraw.DrawSphere(noise.Position, noise.Radius, Colour.Magenta);
            }
        }
    }
}
