using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Game.Components.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct PatrolRouteComponentData
    {
        public string[] Waypoints;
    }

    [RequireSystem(typeof(NameSystem))]
    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(NavigationWaypointComponent))]
    internal class PatrolRouteComponent : EditableComponent<PatrolRouteComponentData>, IDebugDraw, IUpdate
    {
        private TransformComponent m_transform;
        private NavigationWaypointComponent[] m_waypoints;

        public NavigationWaypointComponent[] Waypoints
        {
            get
            {
                return m_waypoints;
            }
        }

        protected override void OnInit(in PatrolRouteComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            ReInit(properties);
        }

        protected override void ReInit(in PatrolRouteComponentData properties)
        {
            // Gather the waypoints
            var nameSystem = Level.GetSystem<NameSystem>();
            m_waypoints = new NavigationWaypointComponent[properties.Waypoints.Length];
            int missingCount = 0;
            for(int i=0; i<properties.Waypoints.Length; ++i)
            {
                var waypointName = properties.Waypoints[i];
                var entity = nameSystem.Lookup(waypointName);
                if(entity == null)
                {
                    App.LogError("Waypoint {0} does not exist", waypointName);
                    missingCount++;
                    continue;
                }

                var waypointComponent = entity.GetComponent<NavigationWaypointComponent>();
                if(waypointComponent == null)
                {
                    App.LogError("Entity {0} does not have waypoint component", waypointName);
                    missingCount++;
                    continue;
                }

                m_waypoints[i] = waypointComponent;
            }

            // If we're not in the level editor, trim the invalid waypoints so other code doesn't have to deal with them
            if(!Level.InEditor && missingCount > 0)
            {
                var trimmedWaypoints = new NavigationWaypointComponent[m_waypoints.Length - missingCount];
                int i = 0;
                foreach (var waypoint in m_waypoints)
                {
                    if(waypoint != null)
                    {
                        trimmedWaypoints[i++] = waypoint;
                    }
                }
                m_waypoints = trimmedWaypoints;
            }
        }

        protected override void OnShutdown()
        {
        }

        public void DebugDraw()
        {
            var lastPos = m_transform.Position;
            foreach(var waypoint in m_waypoints)
            {
                if(waypoint != null)
                {
                    var pos = waypoint.Entity.GetComponent<TransformComponent>().Position;
                    App.DebugDraw.DrawLine(lastPos, pos, Colour.Red);
                    lastPos = pos;
                }
            }
        }

        public void Update(float dt)
        {
            if(Level.InEditor)
            {
                var editorComponent = Entity.GetComponent<EditorComponent>();
                if(editorComponent != null && editorComponent.Selected)
                {
                    DebugDraw();
                }
            }
        }
    }
}
