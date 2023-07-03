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
    internal struct MapCollisionComponentData
    {
        public string Map;
        public int MapEntityIndex;
        public CollisionGroup CollisionGroup;

        [Optional(Default = true)]
        public bool BoundingBoxesOnly;
    }

    internal class MapCollisionComponent : CollisionComponent<MapCollisionComponentData>
    {
        private Map m_map;
        private MapCollisionComponentData m_properties;

        protected override void OnInit(in MapCollisionComponentData properties)
        {
            m_map = Map.Get(properties.Map);
            m_properties = properties;
			base.OnInit(properties);
            Assets.Assets.OnAssetsReloaded += OnAssetsReloaded;
        }

        protected override void Reset(in MapCollisionComponentData properties)
        {
            m_map = Map.Get(properties.Map);
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
			if (m_properties.BoundingBoxesOnly)
			{
                // AABB's are nice and simple
                foreach (var aabb in m_map.GetBoundingBoxes(m_properties.MapEntityIndex))
                {
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
			else
			{
                // Convex hulls have to be translated first,
                // because of the annoying physics requirement that they contain the origin
                var aabbs = m_map.GetBoundingBoxes(m_properties.MapEntityIndex);
                var hulls = m_map.GetHulls(m_properties.MapEntityIndex);
                App.Assert(aabbs.Count == hulls.Count);
                for(int i=0; i<aabbs.Count; ++i)
                {
                    // Translate the planes so that the current centre point becomes the origin
                    var centre = aabbs[i].Center;
                    var hull = hulls[i];
                    var newPlanes = new Plane[hull.Planes.Length];
                    for(int planeIdx=0; planeIdx<newPlanes.Length; ++planeIdx)
                    {
                        var plane = hull.Planes[planeIdx];
                        var pointOnPlane = plane.Normal * plane.DistanceFromOrigin;
                        var newPointOnPlane = pointOnPlane - centre;
                        var newPlaneDistance = newPointOnPlane.Dot(plane.Normal);
                        newPlanes[planeIdx] = new Plane(plane.Normal, newPlaneDistance);
                    }
                    var newHull = new ConvexHull(newPlanes);

                    // Create the shape from the new hull, with a translation to counter the translation
                    var shape = world.CreateConvexHull(
                        Matrix4.CreateTranslation(centre),
                        newHull
                    );
                    shape.Group = m_properties.CollisionGroup;
                    shape.UserData = Entity;
                    o_shapes.Add(shape);
                }
			}
		}    

		private void OnAssetsReloaded(AssetLoadEventArgs e)
        {
            if (e.Paths.Contains(m_map.Path))
            {
                RebuildShapes();
            }
        }
	}
}
