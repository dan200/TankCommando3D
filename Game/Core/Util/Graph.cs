using Dan200.Core.Main;
using Dan200.Core.Math;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Core.Util
{
    internal delegate float PathCostEstimator<TNodeData>(in TNodeData origin, in TNodeData destination);

    internal class Graph<TNodeData>
    {
        public class Node
        {
            public readonly TNodeData Data;
            internal readonly Dictionary<Node, float> Links;
            internal readonly List<Node> BackLinks;

            public Node(in TNodeData data)
            {
                Data = data;
                Links = new Dictionary<Node, float>();
                BackLinks = new List<Node>();
            }
        }

        private List<Node> m_nodes;

        public IReadOnlyCollection<Node> Nodes
        {
            get
            {
                return m_nodes;
            }
        }

        public Graph()
        {
            m_nodes = new List<Node>();
        }

        public void Clear()
        {
            m_nodes.Clear();
        }

        public Node AddNode(in TNodeData data)
        {
            var node = new Node(data);
            m_nodes.Add(node);
            return node;
        }

        public void RemoveNode(Node node)
        {
            App.Assert(m_nodes.Contains(node));
            foreach(var backlink in node.BackLinks)
            {
                App.Assert(m_nodes.Contains(backlink));
                App.Assert(backlink.Links.ContainsKey(node));
                backlink.Links.Remove(node);
            }
            foreach(var link in node.Links.Keys)
            {
                App.Assert(m_nodes.Contains(link));
                App.Assert(link.BackLinks.Contains(node));
                link.BackLinks.Remove(node);
            }
            m_nodes.Remove(node);
        }

        public void AddLink(Node origin, Node destination, float cost)
        {
            App.Assert(origin != destination);
            App.Assert(m_nodes.Contains(origin));
            App.Assert(m_nodes.Contains(destination));
            App.Assert(!origin.Links.ContainsKey(destination));
            App.Assert(!destination.BackLinks.Contains(origin));
            origin.Links.Add(destination, cost);
            destination.BackLinks.Add(origin);
        }

        public void RemoveLink(Node origin, Node destination)
        {
            App.Assert(origin != destination);
            App.Assert(m_nodes.Contains(origin));
            App.Assert(m_nodes.Contains(destination));
            App.Assert(origin.Links.ContainsKey(destination));
            App.Assert(destination.BackLinks.Contains(origin));
            origin.Links.Remove(destination);
            destination.BackLinks.Remove(origin);
        }

        public void AddSymetricLink(Node origin, Node destination, float cost)
        {
            AddLink(origin, destination, cost);
            AddLink(destination, origin, cost);
        }

        public void RemoveSymetricLink(Node origin, Node destination)
        {
            RemoveLink(origin, destination);
            RemoveLink(destination, origin);
        }

        private struct VisitedNodeInfo
        {
            public Node CameFrom;
            public float CostSoFar;
        }

        [ThreadStatic]
        private static Dictionary<Node, VisitedNodeInfo> s_visitedNodes;

        [ThreadStatic]
        private static PriorityQueue<Node> s_openNodes;

        private static void ReconstructPath(Dictionary<Node, VisitedNodeInfo> visitedNodeInfo, Node current, List<Node> o_route)
        {
            int startIndex = o_route.Count;
            VisitedNodeInfo previous;
            while (visitedNodeInfo.TryGetValue(current, out previous) && previous.CameFrom != null)
            {
                o_route.Add(current);
                current = previous.CameFrom;
            }
            o_route.Reverse(startIndex, o_route.Count - startIndex);
        }

        public bool FindRoute(Node origin, Node destination, List<Node> o_route, PathCostEstimator<TNodeData> estimator)
        {
            App.Assert(m_nodes.Contains(origin));
            App.Assert(m_nodes.Contains(destination));

            // Create our working data structures once only, to save on garbage generation
            if (s_visitedNodes == null)
            {
                s_visitedNodes = new Dictionary<Node, VisitedNodeInfo>();
                s_openNodes = new PriorityQueue<Node>();
            }
            else
            {
                App.Assert(s_visitedNodes.Count == 0);
                App.Assert(s_openNodes.Count == 0);
            }

            try
            {
                // Initialise the search space with the origin
                VisitedNodeInfo originInfo;
                originInfo.CameFrom = null;
                originInfo.CostSoFar = 0.0f;
                s_visitedNodes.Add(origin, originInfo);
                s_openNodes.Enqueue(origin, estimator.Invoke(origin.Data, destination.Data));

                // Until the search space is empty..
                while (s_openNodes.Count > 0)
                {
                    // Find the node with the lowest estimate to the destination
                    var current = s_openNodes.Dequeue();

                    // If the node is the destination itself, we're done
                    if (current == destination)
                    {
                        // Emit path information
                        ReconstructPath(s_visitedNodes, current, o_route);
                        return true;
                    }

                    // Else, for each one of the node's neighbours...
                    var costSoFar = s_visitedNodes[current].CostSoFar;
                    foreach (var pair in current.Links)
                    {
                        // Calculate the updated cost so far
                        var neighbour = pair.Key;
                        var linkCost = pair.Value;
                        App.Assert(linkCost >= estimator.Invoke(current.Data, neighbour.Data), "The estimator has overestimated the cost of a link!");
                        float newCostSoFar = costSoFar + linkCost;

                        // If this is the first or quickest route we've found to this neighbour...
                        if (!s_visitedNodes.ContainsKey(neighbour) || newCostSoFar < s_visitedNodes[neighbour].CostSoFar)
                        {
                            // Store the new route info
                            VisitedNodeInfo neighbourInfo;
                            neighbourInfo.CameFrom = current;
                            neighbourInfo.CostSoFar = newCostSoFar;
                            s_visitedNodes[neighbour] = neighbourInfo;

                            // (Re)add the route to the search space with a new cost estimate
                            s_openNodes.UpdatePriority(neighbour, newCostSoFar + estimator.Invoke(neighbour.Data, destination.Data));
                        }
                    }
                }

                // Ran out of search space without locating the destination
                return false;
            }
            finally
            {
                // Clear the data structures when we're done so we don't leave dangling node references
                s_visitedNodes.Clear();
                s_openNodes.Clear();
            }
        }
    }
}
