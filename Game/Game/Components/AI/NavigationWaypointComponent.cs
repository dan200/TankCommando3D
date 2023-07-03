using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;
using Dan200.Game.Systems.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct NavigationWaypointComponentData
    {
    }

    [RequireComponentOnAncestor(typeof(NavGraphComponent))]
    [RequireComponent(typeof(TransformComponent))]
    internal class NavigationWaypointComponent : EditableComponent<NavigationWaypointComponentData>
    {
        private NavGraphComponent m_navigation;
        private NavGraph.Node m_node;

        public NavGraph.Node Node
        {
            get
            {
                return m_node;
            }
        }

        protected override void OnInit(in NavigationWaypointComponentData properties)
        {
            m_navigation = Entity.GetComponentOnAncestor<NavGraphComponent>();
            m_node = null;
            Reset(properties);
        }

        protected override void Reset(in NavigationWaypointComponentData properties)
        {
            if(m_node != null)
            {
                m_navigation.NavGraph.RemoveNode(m_node);
            }
            var transform = Entity.GetComponent<TransformComponent>();
            m_node = m_navigation.AddWaypointNode(transform.Position + 2.0f * Vector3.YAxis);
        }

        protected override void OnShutdown()
        {
            m_navigation.NavGraph.RemoveNode(m_node);
        }
    }
}
