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
    internal struct ModelComponentData
    {
        public string Model;

        [Optional(Default = RenderPass.Opaque)]
        public RenderPass RenderPass;
    }

    [RequireSystem(typeof(LightingSystem))]
    [RequireComponent(typeof(TransformComponent))]
    internal class ModelComponent : EditableComponent<ModelComponentData>, IPrepareToDraw, IDraw, IDrawable<ModelEffectHelper>, IDrawable<ModelShadowEffectHelper>
    {
        private TransformComponent m_transform;
        private ModelInstance m_modelInstance;
		private RenderPass m_renderPass;

        public ModelInstance Instance
        {
            get
            {
                return m_modelInstance;
            }
        }

        protected override void OnInit(in ModelComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            ReInit(properties);
        }

        protected override void ReInit(in ModelComponentData properties)
        {
            var model = Model.Get(properties.Model);
            m_modelInstance = new ModelInstance(model, m_transform.Transform);
            m_renderPass = properties.RenderPass;
        }

        protected override void OnShutdown()
        {
            m_modelInstance = null;
        }

        public void PrepareToDraw(View view)
        {
			m_modelInstance.Transform = m_transform.Transform;
			m_modelInstance.FrustumCull(view.Camera, Vector3.YAxis);
        }

		public void AddToDrawQueue(DrawQueue queue)
		{
			if (!m_modelInstance.Offscreen)
			{
				queue.Add<ModelEffectHelper>(this, m_renderPass);
			}
			if (!m_modelInstance.ShadowOffscreen)
			{
				queue.AddShadow<ModelShadowEffectHelper>(this);
			}
		}

		public void Draw(IRenderer renderer, ModelEffectHelper effect)
        {
			m_modelInstance.Draw(renderer, effect);
        }

        public void Draw(IRenderer renderer, ModelShadowEffectHelper shadowEffect)
        {
            m_modelInstance.DrawShadows(renderer, shadowEffect);
        }
    }
}
