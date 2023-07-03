using System;
using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Interfaces.Physics;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.Physics
{
    internal struct PhysicsWorldComponentData
    {
        [Optional(0.0f, -9.8f, 0.0f)]
        public Vector3 Gravity;
    }

    [RequireComponent(typeof(HierarchyComponent))]
    internal class PhysicsWorldComponent : EditableComponent<PhysicsWorldComponentData>, IUpdate, IDebugDraw
    {
        private HierarchyComponent m_hierarchy;
        private PhysicsWorld m_world;

        public PhysicsWorld World
        {
            get
            {
                return m_world;
            }
        }

        protected override void OnInit(in PhysicsWorldComponentData properties)
        {
            m_hierarchy = Entity.GetComponent<HierarchyComponent>();
            m_world = new PhysicsWorld();
            Reset(properties);
        }

        protected override void Reset(in PhysicsWorldComponentData properties)
        {
            m_world.Gravity = properties.Gravity;
        }

        protected override void OnShutdown()
        {
            m_world.Dispose();
            m_world = null;
        }

        public void Update(float dt)
        {
            int steps = m_world.Update(dt);
            for (int i = 0; i < steps; ++i)
            {
                foreach (var component in m_hierarchy.GetDescendantsWithInterface<IPhysicsUpdate>())
                {
                    component.PhysicsUpdate(PhysicsWorld.STEP_TIME);
                }
                m_world.Step();
            }
        }

        public void DebugDraw()
        {
            // Debug draw
            m_world.DebugDraw();
        }
    }
}
