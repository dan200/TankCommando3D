using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Game.Components.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal struct TrackableComponentData
    {
        [Optional(Default = ".")]
        public string IconTransformPath;

        [Optional(Default = ".")]
        public string HealthPath;
    }

    [RequireSystem(typeof(NameSystem))]
    [AfterComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(HealthComponent))]
    [AfterComponent(typeof(NameComponent))]
    internal class TrackableComponent : Component<TrackableComponentData>
    {
        private TransformComponent m_transform;
        private HealthComponent m_health;

        public Vector3 Position
        {
            get
            {
                return m_transform.Position;
            }
        }

        public bool IsDead
        {
            get
            {
                return m_health.IsDead;
            }
        }

        protected override void OnInit(in TrackableComponentData properties)
        {
            m_transform = Level.GetSystem<NameSystem>().Lookup(properties.IconTransformPath, Entity).GetComponent<TransformComponent>();
            m_health = Level.GetSystem<NameSystem>().Lookup(properties.HealthPath, Entity).GetComponent<HealthComponent>();
        }

        protected override void OnShutdown()
        {
        }
    }
}
