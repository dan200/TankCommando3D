using System;
using Dan200.Core.Animation;
using Dan200.Core.Components.Render;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.Misc
{
    internal struct AnimationComponentData
    {
        [Optional]
        public string Animation;
    }

	[RequireComponent(typeof(ModelComponent))]
    internal class AnimationComponent : Component<AnimationComponentData>, IUpdate, IPrepareToDraw
    {
        private ModelComponent m_model;

        protected override void OnInit(in AnimationComponentData properties)
        {
            m_model = Entity.GetComponent<ModelComponent>();
            if (properties.Animation != null)
            {
                m_model.Instance.Animation = LuaAnimation.Get(properties.Animation);
                m_model.Instance.AnimTime = 0.0f;
                m_model.Instance.Animate();
            }
        }

        protected override void OnShutdown()
        {
            m_model.Instance.Animation = null;
        }

        public void Update(float dt)
        {
            m_model.Instance.AnimTime += dt;
        }

        public void PrepareToDraw(View view)
        {
            if(!m_model.Instance.Offscreen)
            {
                m_model.Instance.Animate();
            }
        }
    }
}
