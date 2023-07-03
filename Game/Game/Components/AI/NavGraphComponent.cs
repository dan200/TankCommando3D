using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Components.Physics;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Core.Util;

namespace Dan200.Game.Components.AI
{
    internal class NavGraph : Graph<NavGraph.NodeData>
    {
        public enum NodeType
        {
            Waypoint,
            Origin,
        }

        internal struct NodeData
        {
            public Vector3 Position;
            public NodeType Type;
        }

        public static float EuclideanEstimator(in NodeData origin, in NodeData destination)
        {
            return (destination.Position - origin.Position).WithY(0.0f).Length;
        }

        public NavGraph()
        {
        }
    }

    internal struct NavGraphComponentData
    {
    }

    [AfterComponent(typeof(PhysicsWorldComponent))]
    internal class NavGraphComponent : EditableComponent<NavGraphComponentData>, IUpdate, IDebugDraw
    {
        private PhysicsWorldComponent m_physics;
        private NavGraph m_navGraph;

        public NavGraph NavGraph
        {
            get
            {
                return m_navGraph;
            }
        }

        protected override void OnInit(in NavGraphComponentData properties)
        {
            m_physics = Entity.GetComponent<PhysicsWorldComponent>();
            m_navGraph = new NavGraph();
        }

        protected override void Reset(in NavGraphComponentData properties)
        {
        }

        protected override void OnShutdown()
        {
        }

        public NavGraph.Node AddOriginNode(Vector3 position)
        {
            var nodeData = new NavGraph.NodeData();
            nodeData.Position = position;
            nodeData.Type = NavGraph.NodeType.Origin;
            var newNode = m_navGraph.AddNode(nodeData);
            foreach(var node in m_navGraph.Nodes)
            {
                float linkCost;
                if(node != newNode && 
                   node.Data.Type != NavGraph.NodeType.Origin &&
                   CheckLineOfSight(newNode, node, out linkCost))
                {
                    m_navGraph.AddLink(newNode, node, linkCost);
                }
            }
            return newNode;
        }
        
        public NavGraph.Node AddWaypointNode(Vector3 position)
        {
            var nodeData = new NavGraph.NodeData();
            nodeData.Position = position;
            nodeData.Type = NavGraph.NodeType.Waypoint;
            var newNode = m_navGraph.AddNode(nodeData);
            foreach (var node in m_navGraph.Nodes)
            {
                float linkCost;
                if (node != newNode &&
                    node.Data.Type != NavGraph.NodeType.Origin &&
                    CheckLineOfSight(newNode, node, out linkCost))
                {
                    m_navGraph.AddSymetricLink(node, newNode, linkCost);
                }
            }
            return newNode;
        }

        public NavGraph.Node FindWaypointNear(Vector3 position)
        {
            NavGraph.Node closestNode = null;
            float closestDistanceSq = float.MaxValue;
            foreach(var node in m_navGraph.Nodes)
            {
                if (node.Data.Type == NavGraph.NodeType.Waypoint)
                {
                    var distanceSq = (node.Data.Position - position).LengthSquared;
                    if (distanceSq < closestDistanceSq)
                    {
                        closestNode = node;
                        closestDistanceSq = distanceSq;
                    }
                }
            }
            return closestNode;
        }

        private bool CheckLineOfSight(NavGraph.Node origin, NavGraph.Node destination, out float o_cost)
        {
            var start = origin.Data.Position;
            var end = destination.Data.Position;
            var ray = new Ray(start, end);

            RaycastResult result;
            if(m_physics == null || !m_physics.World.SphereCast(ray, 1.0f, CollisionGroup.Environment, out result)) // TODO: Require physics
            {
                o_cost = (end - start).WithY(0.0f).Length;
                return true;
            }
            else
            {
                o_cost = default(float);
                return false;
            }
        }

        public void DebugDraw()
        {
            foreach(var node in m_navGraph.Nodes)
            {
                var pos = node.Data.Position;
                switch (node.Data.Type)
                {
                    case NavGraph.NodeType.Waypoint:
                    default:
                        App.DebugDraw.DrawCross(pos, 0.25f, Colour.Yellow);
                        break;
                    case NavGraph.NodeType.Origin:
                        App.DebugDraw.DrawCross(pos, 0.25f, Colour.Green);
                        break;
                }
                App.DebugDraw.DrawLine(pos, pos - 2.0f * Vector3.YAxis, Colour.Cyan);
                foreach (var linkedNode in node.Links.Keys)
                {
                    var linkedPos = linkedNode.Data.Position;
                    App.DebugDraw.DrawLine(pos, linkedPos, Colour.Green);
                }
            }
        }

        public void Update(float dt)
        {
            if(Level.InEditor)
            {
                DebugDraw();
            }
        }
    }
}
