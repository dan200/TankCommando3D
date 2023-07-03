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
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.Physics
{
    internal struct BoxCollisionComponentData
    {
        [Optional]
        public Vector3 Centre;

        public Vector3 Size;
        public CollisionGroup CollisionGroup;
    }

    internal class BoxCollisionComponent : CollisionComponent<BoxCollisionComponentData>
    {
		private BoxCollisionComponentData m_properties;

        protected override void OnInit(in BoxCollisionComponentData properties)
        {
            m_properties = properties;
			base.OnInit(properties);
        }

        protected override void Reset(in BoxCollisionComponentData properties)
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
			var shape = world.CreateBox(Matrix4.CreateTranslation(m_properties.Centre), m_properties.Size);
			shape.Group = m_properties.CollisionGroup;
			shape.UserData = Entity;
			o_shapes.Add(shape);
		}    
	}
}
