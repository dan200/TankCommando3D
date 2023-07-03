using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Game.Components.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct SightTargetComponentData
    {
    }

    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(PlayerMovementComponent))]
    internal class SightTargetComponent : Component<SightTargetComponentData>
    {
        private TransformComponent m_transform;
        private PlayerMovementComponent m_player;

        public Vector3 Position
        {
            get
            {
                if(m_player != null)
                {
                    return m_player.EyePos - 0.3f * Vector3.YAxis;
                }
                return m_transform.Position;
            }
        }

        protected override void OnInit(in SightTargetComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            m_player = Entity.GetComponent<PlayerMovementComponent>();
        }

        protected override void OnShutdown()
        {
        }
    }
}
