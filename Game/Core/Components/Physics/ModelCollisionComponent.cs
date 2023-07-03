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
using Dan200.Core.Geometry;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.Physics
{
    internal struct ModelCollisionComponentData
    {
        public string Model;
        public CollisionGroup CollisionGroup;

        [Optional(Default = true)]
        public bool BoundingBoxOnly;
    }

    internal class ModelCollisionComponent : CollisionComponent<ModelCollisionComponentData>
    {
        private Model m_model;
        private ModelCollisionComponentData m_properties;

        protected override void OnInit(in ModelCollisionComponentData properties)
        {
            m_model = Model.Get(properties.Model);
            m_properties = properties;
			base.OnInit(properties);
            Assets.Assets.OnAssetsReloaded += OnAssetsReloaded;
        }

        protected override void Reset(in ModelCollisionComponentData properties)
        {
            m_model = Model.Get(properties.Model);
            m_properties = properties;
            base.Reset(properties);
        }

        protected override void OnShutdown()
        {
            Assets.Assets.OnAssetsReloaded -= OnAssetsReloaded;
			base.OnShutdown();
        }

		protected override void CreateShapes(PhysicsWorld world, List<PhysicsShape> o_shapes)
		{
			if (m_properties.BoundingBoxOnly)
			{
				var aabb = m_model.BoundingBox;
				if (aabb.Volume > 0.0f)
				{
					var shape = world.CreateBox(
						Matrix4.CreateTranslation(aabb.Center),
						aabb.Size
					);
					shape.Group = m_properties.CollisionGroup;
					shape.UserData = Entity;
					o_shapes.Add(shape);
				}
			}
			else
			{
				for (int i = 0; i < m_model.GroupCount; ++i)
				{
					var aabb = m_model.GetGroupBoundingBox(i);
					if (aabb.Volume > 0.0f)
					{				
						var shape = world.CreateBox(
							Matrix4.CreateTranslation(aabb.Center),
							aabb.Size
						);
						shape.Group = m_properties.CollisionGroup;
						shape.UserData = Entity;
						o_shapes.Add(shape);
					}
				}
			}
		}    

		private void OnAssetsReloaded(AssetLoadEventArgs e)
        {
            if (e.Paths.Contains(m_model.Path))
            {
                RebuildShapes();
            }
        }
	}
}
