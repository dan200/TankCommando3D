using System;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;

namespace Dan200.Core.Components.Render
{
    internal struct DirectionalLightComponentData
    {
        public Colour Colour;

        [Optional(Default = false)]
        public bool CastShadows;
    }

	[RequireSystem(typeof(LightingSystem))]
	[RequireComponent(typeof(TransformComponent))]
    internal class DirectionalLightComponent : EditableComponent<DirectionalLightComponentData>, IPrepareToDraw, IUpdate
	{
		private TransformComponent m_transform;
		private DirectionalLight m_light;
        private ColourF m_colour;

        protected override void OnInit(in DirectionalLightComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            AddLight(properties);
        }

        protected override void ReInit(in DirectionalLightComponentData properties)
        {
            RemoveLight();
            AddLight(properties);
        }

        protected override void OnShutdown()
		{
            RemoveLight();
		}

        public void Update(float dt)
        {
            m_light.Colour = Entity.Visible ? m_colour : ColourF.Black; // TODO: This but better
        }

		public void PrepareToDraw(View view)
		{
			m_light.Direction = m_transform.Transform.Forward;
		}

        private void AddLight(in DirectionalLightComponentData properties)
        {
            m_colour = properties.Colour.ToColourF();
            m_light = new DirectionalLight(
                m_transform.Transform.Forward,
                m_colour,
                properties.CastShadows
            );
            var lighting = Level.GetSystem<LightingSystem>();
            if (m_light.CastShadows)
            {
                lighting.DirectionalLights.Insert(0, m_light);
            }
            else
            {
                lighting.DirectionalLights.Add(m_light);
            }
        }

        private void RemoveLight()
        {
            Level.GetSystem<LightingSystem>().DirectionalLights.Remove(m_light);
        }
    }
}
