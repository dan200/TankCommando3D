using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Systems.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct NavigatorComponentData
    {
    }

    [RequireComponentOnAncestor(typeof(NavGraphComponent))]
    [RequireComponent(typeof(TransformComponent))]
    internal class NavigatorComponent : Component<NavigatorComponentData>
    {
        private NavGraphComponent m_navigation;
        private TransformComponent m_transform;

        private List<NavGraph.Node> m_currentRoute;
        private NavGraph.Node m_currentNode;

        private NavGraph.Node m_currentLinkStart;
        private NavGraph.Node m_currentLinkEnd;

        public NavGraph.Node CurrentWaypoint
        {
            get
            {
                return m_currentNode;
            }
        }

        public NavGraph.Node NextWaypoint
        {
            get
            {
                return m_currentRoute.FirstOrDefault();
            }
        }

        public NavGraph.Node DestinationWaypoint
        {
            get
            {
                return m_currentRoute.LastOrDefault();
            }
        }

        protected override void OnInit(in NavigatorComponentData properties)
        {
            m_navigation = Entity.GetComponentOnAncestor<NavGraphComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();

            m_currentRoute = new List<NavGraph.Node>();
            m_currentNode = null;

            m_currentLinkStart = null;
            m_currentLinkEnd = null;
        }

        protected override void OnShutdown()
        {
            if (m_currentLinkStart != null && m_currentLinkStart.Data.Type == NavGraph.NodeType.Origin)
            {
                m_navigation.NavGraph.RemoveNode(m_currentLinkStart);
                m_currentLinkStart = null;
            }
        }

        public void ClearRoute()
        {
            m_currentRoute.Clear();
        }

        public bool AppendRoute(NavGraph.Node destination)
        {
            NavGraph.Node origin;
            bool originIsNew;
            if(m_currentRoute.Count > 0)
            {
                origin = m_currentRoute.Last();
                originIsNew = false;
            }
            else if(m_currentNode != null)
            {
                origin = m_currentNode;
                originIsNew = false;
            }
            else if(m_currentLinkStart != null)
            {
                App.Assert(m_currentLinkEnd != null);
                var nodeData = new NavGraph.NodeData();
                nodeData.Position = m_transform.Position + 2.0f * Vector3.YAxis;
                nodeData.Type = NavGraph.NodeType.Origin;
                origin = m_navigation.NavGraph.AddNode(nodeData);
                if (m_currentLinkStart.Data.Type != NavGraph.NodeType.Origin)
                {
                    m_navigation.NavGraph.AddLink(origin, m_currentLinkStart, (m_currentLinkStart.Data.Position - origin.Data.Position).WithY(0.0f).Length);
                }
                App.Assert(m_currentLinkEnd.Data.Type != NavGraph.NodeType.Origin);
                m_navigation.NavGraph.AddLink(origin, m_currentLinkEnd, (m_currentLinkEnd.Data.Position - origin.Data.Position).WithY(0.0f).Length);
                originIsNew = true;
            }
            else
            {
                origin = m_navigation.AddOriginNode(m_transform.Position + 2.0f * Vector3.YAxis);
                originIsNew = true;
            }

            bool result = m_navigation.NavGraph.FindRoute(origin, destination, m_currentRoute, NavGraph.EuclideanEstimator);
            if(originIsNew)
            {
                if (result)
                {
                    App.Assert(m_currentNode == null);
                    App.Assert(m_currentRoute.Count > 0);
                    if (m_currentLinkStart != null && m_currentLinkStart.Data.Type == NavGraph.NodeType.Origin)
                    {
                        m_navigation.NavGraph.RemoveNode(m_currentLinkStart);
                    }
                    m_currentNode = origin;
                    m_currentLinkStart = origin;
                    m_currentLinkEnd = m_currentRoute.First();
                }
                else
                {
                    m_navigation.NavGraph.RemoveNode(origin);
                }
            }
            return result;
        }

        public NavGraph.Node GetCurrentWaypoint()
        {
            return m_currentNode;
        }

        public NavGraph.Node GetNextWaypointOnRoute()
        {
            return (m_currentRoute.Count > 0) ? m_currentRoute.First() : null;
        }

        public void SetArrivedAtNextWaypoint()
        {
            App.Assert(m_currentRoute.Count > 0, "There are no more waypoints remaining on the route");
            App.Assert(m_currentNode == null, "Arrived at a waypoint without departing the previous waypoint");
            m_currentNode = m_currentRoute.First();
            m_currentRoute.RemoveAt(0);
            if (m_currentRoute.Count > 0)
            {
                if(m_currentLinkStart != null && m_currentLinkStart.Data.Type == NavGraph.NodeType.Origin)
                {
                    m_navigation.NavGraph.RemoveNode(m_currentLinkStart);
                }
                m_currentLinkStart = m_currentNode;
                m_currentLinkEnd = m_currentRoute.First();
            }
        }

        public void SetDepartedCurrentWaypoint()
        {
            App.Assert(m_currentNode != null, "Tried to depart a waypoint when not at one.");
            App.Assert(m_currentRoute.Count > 0, "There are depart a waypoint with no destination");
            m_currentNode = null;
        }
    }
}
