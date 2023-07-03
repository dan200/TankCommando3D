using System;
using Dan200.Core.Animation;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;

namespace Dan200.Core.Components.Render
{
    internal struct MapComponentData
    {
        public string Map;
        public int MapEntityIndex;

        [Optional(Default = RenderPass.Opaque)]
        public RenderPass RenderPass;
    }

    [RequireSystem(typeof(LightingSystem))]
    [RequireComponent(typeof(TransformComponent))]
    internal class MapComponent : EditableComponent<MapComponentData>, IPrepareToDraw, IDraw, IDrawable<MapEffectHelper>
    {
        private TransformComponent m_transform;
        private Map m_map;
        private int m_entityIndex;
		private RenderPass m_renderPass;

        protected override void OnInit(in MapComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            ReInit(properties);
        }

        protected override void ReInit(in MapComponentData properties)
        {
            m_map = Map.Get(properties.Map);
            m_entityIndex = properties.MapEntityIndex;
            m_renderPass = properties.RenderPass;
        }

        protected override void OnShutdown()
        {
        }

        public void PrepareToDraw(View view)
        {
        }

		public void AddToDrawQueue(DrawQueue queue)
		{
			queue.Add<MapEffectHelper>(this, m_renderPass);
		}

		public void Draw(IRenderer renderer, MapEffectHelper effect)
        {
            m_map.DrawEntity(renderer, effect, m_entityIndex, m_transform.Transform);
        }
    }
}
