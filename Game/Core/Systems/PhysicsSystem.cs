using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Script;
using Dan200.Core.Main;
using Dan200.Core.Assets;
using Dan200.Core.Physics;
using Dan200.Core.Interfaces;
using Dan200.Core.Util;
using Dan200.Core.Math;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Systems
{
    internal struct PhysicsSystemData
    {
        [Optional(0.0f, -9.8f, 0.0f)]
        public Vector3 Gravity;
    }

    internal class PhysicsSystem : System<PhysicsSystemData>, IUpdate, IDebugDraw
    {
        private PhysicsWorld m_world;

        public PhysicsWorld World
        {
            get
            {
                return m_world;
            }
        }

        protected override void OnInit(in PhysicsSystemData properties)
        {
            // Create the physics world
            m_world = new PhysicsWorld();
            m_world.Gravity = properties.Gravity;
        }

        protected override void OnShutdown()
        {
            m_world.Dispose();
            m_world = null;
        }

        public void Update(float dt)
        {
            // Update entities (pre-physics)
            foreach (var component in Level.GetComponentsWithInterface<IUpdatePrePhysics>())
            {
                component.UpdatePrePhysics(dt);
            }

            // Update physics
            int steps = m_world.Update(dt);
            for (int i = 0; i < steps; ++i)
            {
                foreach (var component in Level.GetComponentsWithInterface<IPhysicsUpdate>())
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
