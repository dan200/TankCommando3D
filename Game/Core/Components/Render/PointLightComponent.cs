using System;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;

namespace Dan200.Core.Components.Render
{
    internal struct PointLightComponentData
    {
        public Colour Colour;

        [Range(Min = 0.0f)]
        public float Range;

        [Optional(Default = false)]
        public bool CastShadows;
    }

	[RequireSystem(typeof(LightingSystem))]
	[RequireComponent(typeof(TransformComponent))]
    internal class PointLightComponent : EditableComponent<PointLightComponentData>, IPrepareToDraw, IUpdate
	{
		private TransformComponent m_transform;
		private PointLight m_light;
        private ColourF m_colour;

        protected override void OnInit(in PointLightComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            AddLight(properties);
        }

        protected override void Reset(in PointLightComponentData properties)
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
            m_light.Colour = Entity.Visible ? m_colour : ColourF.Black;
        }

		public void PrepareToDraw(View view)
		{
			m_light.Position = m_transform.Position;
		}

        private void AddLight(in PointLightComponentData properties)
        {
            m_colour = properties.Colour.ToColourF();
            m_light = new PointLight(
                m_transform.Position,
                m_colour,
                properties.Range,
                properties.CastShadows
            );
            var lighting = Level.GetSystem<LightingSystem>();
            if (m_light.CastShadows)
            {
                lighting.PointLights.Insert(0, m_light);
            }
            else
            {
                lighting.PointLights.Add(m_light);
            }
        }

        private void RemoveLight()
        {
            Level.GetSystem<LightingSystem>().PointLights.Remove(m_light);
        }
    }
}
