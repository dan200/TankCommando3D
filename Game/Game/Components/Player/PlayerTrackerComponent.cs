using Dan200.Core.Level;
using Dan200.Core.Serialisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Util;
using Dan200.Core.Interfaces;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Math;
using Dan200.Core.Systems;
using Dan200.Core.Geometry;
using Dan200.Core.Physics;
using Dan200.Game.GUI;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerTrackerComponentData
    {
        [Range(Min = 0.0)]
        public float ConfirmTime;
    }

    [RequireSystem(typeof(PhysicsSystem))]
    [RequireComponent(typeof(PlayerMovementComponent))]
    [RequireComponent(typeof(PlayerSettingsComponent))]
    internal class PlayerTrackerComponent : Component<PlayerTrackerComponentData>, IUpdate, IDebugDraw
    {
        private PhysicsSystem m_physics;
        private PlayerMovementComponent m_movement;
        private PlayerSettingsComponent m_settings;
        private PlayerTrackerComponentData m_properties;

        private Dictionary<TrackableComponent, float> m_glimpsedTargets;
        private List<TrackableComponent> m_confirmedTargets;

        protected override void OnInit(in PlayerTrackerComponentData properties)
        {
            m_physics = Level.GetSystem<PhysicsSystem>();
            m_movement = Entity.GetComponent<PlayerMovementComponent>();
            m_settings = Entity.GetComponent<PlayerSettingsComponent>();
            m_properties = properties;
            App.Assert(properties.ConfirmTime > 0.0f);

            m_glimpsedTargets = new Dictionary<TrackableComponent, float>(); // trackable -> confirm progress
            m_confirmedTargets = new List<TrackableComponent>();
        }

        protected override void OnShutdown()
        {
        }

        private bool IsTargetVisible(TrackableComponent component)
        {
            if(!component.Dead)
            {
                var eyeTransform = m_movement.EyeTransform;
                var targetPos = component.Position;
                var targetDir = (component.Position - eyeTransform.Position).SafeNormalise(eyeTransform.Forward);

                // Check in FOV
                var dotLimit = Mathf.Cos( 10.0f * Mathf.DEGREES_TO_RADIANS );
                if(targetDir.Dot(eyeTransform.Forward) < dotLimit)
                {
                    return false;
                }

                // Check for occlusion
                RaycastResult result;
                if(m_physics.World.Raycast(new Ray(eyeTransform.Position, targetPos), CollisionGroup.Environment | CollisionGroup.Prop, out result))
                {
                    return result.Shape.UserData == component.Entity;
                }
                return true;

            }
            return false;
        }

        public void Update(float dt)
        {
            // Lose existing targets
            if (m_glimpsedTargets.Count > 0)
            {
                List<TrackableComponent> removedTargets = null;
                List<TrackableComponent> grownTargets = null;
                var growth = dt / m_properties.ConfirmTime;
                foreach (var target in m_glimpsedTargets.Keys)
                {
                    if(!IsTargetVisible(target))
                    {
                        if(removedTargets == null)
                        {
                            removedTargets = new List<TrackableComponent>(m_glimpsedTargets.Count);
                        }
                        removedTargets.Add(target);
                    }
                    else
                    {
                        var progress = m_glimpsedTargets[target];
                        if(progress + growth >= 1.0f)
                        {
                            if (removedTargets == null)
                            {
                                removedTargets = new List<TrackableComponent>(m_glimpsedTargets.Count);
                            }
                            removedTargets.Add(target);
                            m_confirmedTargets.Add(target);
                        }
                        else
                        {
                            if (grownTargets == null)
                            {
                                grownTargets = new List<TrackableComponent>(m_glimpsedTargets.Count);
                            }
                            grownTargets.Add(target);
                        }
                    }
                }
                if(removedTargets != null)
                {
                    foreach(var target in removedTargets)
                    {
                        m_glimpsedTargets.Remove(target);
                    }
                }
                if (grownTargets != null)
                {
                    foreach (var target in grownTargets)
                    {
                        m_glimpsedTargets[target] += growth;
                    }
                }
            }

            // Find new targets
            foreach (var trackable in Level.GetComponents<TrackableComponent>())
            {
                if(!m_confirmedTargets.Contains(trackable) && !m_glimpsedTargets.ContainsKey(trackable) && IsTargetVisible(trackable))
                {
                    m_glimpsedTargets.Add(trackable, 0.0f);
                }
            }
        }

        public void DebugDraw()
        {
            foreach(var target in m_glimpsedTargets.Keys)
            {
                App.DebugDraw.DrawSphere(target.Position, 1.0f, Colour.Yellow);
            }
            foreach (var target in m_confirmedTargets)
            {
                App.DebugDraw.DrawSphere(target.Position, 1.0f, Colour.Green);
            }
        }

        public void PopulaterTrackerGUI(TrackerDisplay tracker)
        {
            tracker.Blips.Clear();
            foreach (var target in m_glimpsedTargets.Keys)
            {
                if (!target.IsDead)
                {
                    var blip = new TrackerDisplay.Blip();
                    blip.Position = target.Position;
                    blip.Confirmed = false;
                    tracker.Blips.Add(blip);
                }
            }
            foreach (var target in m_confirmedTargets)
            {
                if (!target.IsDead)
                {
                    var blip = new TrackerDisplay.Blip();
                    blip.Position = target.Position;
                    blip.Confirmed = true;
                    tracker.Blips.Add(blip);
                }
            }
        }
    }
}
