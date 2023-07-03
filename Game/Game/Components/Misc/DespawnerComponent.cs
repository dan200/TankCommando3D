using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Misc
{
    internal struct DespawnerComponentData
    {
        public float Timeout;
        public bool Animate;
    }

    internal class DespawnerComponent : Component<DespawnerComponentData>, IUpdate
    {
        private DespawnerComponentData m_properties;
        private float m_timer;

        protected override void OnInit(in DespawnerComponentData properties)
        {
            m_properties = properties;
            m_timer = -1.0f;
        }

        protected override void OnShutdown()
        {
        }

        public void Despawn()
        {
            if(m_timer < 0.0f)
            {
                m_timer = m_properties.Timeout;
            }
        }

        public void CancelDespawn()
        {
            m_timer = -1.0f;
            Entity.Visible = true;
        }

        public void Update(float dt)
        {
            if(m_timer >= 0.0f)
            {
                m_timer -= dt;        
                if(m_properties.Animate)
                {
                    var timerFrac = 1.0f - Mathf.Saturate(m_timer / m_properties.Timeout);
                    Entity.Visible = (Mathf.Sin(Mathf.Pow(timerFrac, 2.0f) * 20.0f * 2.0f * Mathf.PI) >= 0.0f);
                }
                if(m_timer < 0.0f)
                {
                    Level.Entities.Destroy(Entity);
                }
            }
        }
    }
}
