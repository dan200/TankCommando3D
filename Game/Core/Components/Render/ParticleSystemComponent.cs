using System;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Render;

namespace Dan200.Core.Components.Render
{
    internal struct ParticleSystemComponentData
    {
    }

	[RequireComponent(typeof(TransformComponent))]
    internal class ParticleSystemComponent : Component<ParticleSystemComponentData>, IPrepareToDraw, IDraw
    {
        private TransformComponent m_transform;

        protected override void OnInit(in ParticleSystemComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            // TODO
        }

        protected override void OnShutdown()
        {
            // TODO
        }

        public void PrepareToDraw(View view)
        {
            // TODO
        }

		public void AddToDrawQueue(DrawQueue queue)
		{
			// TODO
		}
    }
}
