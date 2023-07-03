using Dan200.Core.Assets;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Multiplayer;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System.Collections.Generic;
using System.Linq;
using Dan200.Core.Interfaces;
using Dan200.Core.Systems;
using System;

namespace Dan200.Core.Components.Physics
{
    internal struct SphereCollisionComponentData
    {
        public float Radius;
        public CollisionGroup CollisionGroup;
    }

    internal class SphereCollisionComponent : CollisionComponent<SphereCollisionComponentData>
    {
        private SphereCollisionComponentData m_properties;

        protected override void OnInit(in SphereCollisionComponentData properties)
        {
            m_properties = properties;
			base.OnInit(properties);
        }

        protected override void Reset(in SphereCollisionComponentData properties)
        {
            m_properties = properties;
            base.Reset(properties);
        }

        protected override void OnShutdown()
        {
			base.OnShutdown();
        }

		protected override void CreateShapes(PhysicsWorld world, List<PhysicsShape> o_shapes)
		{
            var shape = world.CreateSphere(
                Matrix4.Identity,
                m_properties.Radius
			);
			shape.Group = m_properties.CollisionGroup;
			shape.UserData = Entity;
			o_shapes.Add(shape);
		}    
	}
}
