using Dan200.Core.Components;
using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerSpawnPointComponentData
    {
    }

    [RequireComponent(typeof(TransformComponent))]
    internal class PlayerSpawnPointComponent : Component<PlayerSpawnPointComponentData>
    {
        private TransformComponent m_transform;

        public Matrix4 Transform
        {
            get
            {
                return m_transform.Transform;
            }
        }

        protected override void OnInit(in PlayerSpawnPointComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
        }

        protected override void OnShutdown()
        {
        }
    }
}
