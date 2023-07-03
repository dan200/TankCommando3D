using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Geometry;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Game.Components.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct SightComponentData
    {
        [Range(Min = 0.0)]
        public float Range;

        [Range(Min = 0.0)]
        public float FOVX;

        [Range(Min = 0.0)]
        public float FOVY;

        [Optional]
        public string EyeTransformPath;
    }

    [RequireSystem(typeof(NameSystem))]
    [RequireComponentOnAncestor(typeof(PhysicsWorldComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(NameComponent))]
    internal class SightComponent : Component<SightComponentData>, IUpdate, IDebugDraw
    {
        private PhysicsWorldComponent m_physics;
        private TransformComponent m_transform;
        private SightComponentData m_properties;
        private List<Entity> m_visibleTargets;
        
        public IReadOnlyCollection<Entity> VisibleTargets
        {
            get
            {
                return m_visibleTargets;
            }
        }

        protected override void OnInit(in SightComponentData properties)
        {
            m_physics = Entity.GetComponentOnAncestor<PhysicsWorldComponent>();
            if(properties.EyeTransformPath != null)
            {
                m_transform = Level.GetSystem<NameSystem>().Lookup(properties.EyeTransformPath, Entity).GetComponent<TransformComponent>();
            }
            else
            {
                m_transform = Entity.GetComponent<TransformComponent>();
            }
            m_properties = properties;
            m_visibleTargets = new List<Entity>();
        }

        protected override void OnShutdown()
        {
        }

        private bool CheckClearPathToTarget(Vector3 pos, Vector3 targetPos, Entity targetEntity)
        {
            var ray = new Ray(pos, targetPos);
            RaycastResult result;
            if (m_physics.World.Raycast(ray, CollisionGroup.Environment | CollisionGroup.Prop, out result))
            {
                var entity = result.Shape.UserData as Entity;
                return (entity == targetEntity);
            }
            else
            {
                return true;
            }
        }

        public void Update(float dt)
        {
            var transform = m_transform.Transform;
            var pos = transform.Position;
            var fwd = transform.Forward;
            var dotProductLimit = Mathf.Cos(0.5f * m_properties.FOVX * Mathf.DEGREES_TO_RADIANS);
            var yScale = m_properties.FOVX / m_properties.FOVY;
            var range = m_properties.Range;

            m_visibleTargets.Clear();
            foreach (var target in Level.GetComponents<SightTargetComponent>())
            {
                var targetPos = target.Position;
                var vectorToTarget = target.Position - pos;
                if(vectorToTarget.Length <= range)
                {
                    var directionToTarget = vectorToTarget.WithY(vectorToTarget.Y * yScale).SafeNormalise(fwd);
                    if(directionToTarget.Dot(fwd) >= dotProductLimit)
                    {
                        if(CheckClearPathToTarget(pos, targetPos, target.Entity))
                        {
                            m_visibleTargets.Add(target.Entity);
                        }
                    }
                }
            }
        }

        public void DebugDraw()
        {
            var transform = m_transform.Transform;
            var pos = transform.Position;
            var fwd = transform.Forward;
            var radius = Mathf.Tan(0.5f * m_properties.FOVX * Mathf.DEGREES_TO_RADIANS) * m_properties.Range;
            App.DebugDraw.DrawCone(pos + fwd * m_properties.Range, pos, radius, (m_visibleTargets.Count > 0) ? Colour.Green : Colour.White);
            foreach(var target in m_visibleTargets)
            {
                var targetPos = target.GetComponent<SightTargetComponent>().Position;
                App.DebugDraw.DrawLine(pos, targetPos, Colour.Green);
                App.DebugDraw.DrawCross(targetPos, 1.0f, Colour.Green);
            }
        }
    }
}
