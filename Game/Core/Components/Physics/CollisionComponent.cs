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
using Dan200.Core.Components.Core;

namespace Dan200.Core.Components.Physics
{
	[RequireSystem(typeof(PhysicsSystem))]
	[RequireComponent(typeof(TransformComponent))]
	[AfterComponent(typeof(HierarchyComponent))]
	[AfterComponent(typeof(PhysicsComponent))]
    internal abstract class CollisionComponent<TComponentData> : EditableComponent<TComponentData>, IAncestryListener, IComponentListener where TComponentData : struct
    {
		private PhysicsWorld m_world;
		private TransformComponent m_transform;
		private PhysicsComponent m_physics;
		private Matrix4 m_fromPhysicsTransform;
		private List<PhysicsShape> m_shapes;

		private PhysicsComponent FindRootPhysicsComponent(Entity entityToIgnore, out Matrix4 o_fromPhysicsTransform)
		{
			// Find the most distant ancestor with an unbroken chain of transform component
			var entity = Entity;
			var transformComponent = m_transform;
			o_fromPhysicsTransform = Matrix4.Identity;
			while (true)
			{
				var parent = HierarchyComponent.GetParent(entity);
				if (parent != null && parent != entityToIgnore)
				{
					var transform = parent.GetComponent<TransformComponent>();
					if (transform != null)
					{
						o_fromPhysicsTransform = transformComponent.LocalTransform.ToWorld(o_fromPhysicsTransform);
						entity = parent;
						transformComponent = transform;
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}

			// Get it's physics component
			return entity.GetComponent<PhysicsComponent>();
		}

        protected override void OnInit(in TComponentData properties)
        {
			m_world = Level.GetSystem<PhysicsSystem>().World;
			m_transform = Entity.GetComponent<TransformComponent>();
			m_physics = FindRootPhysicsComponent(null, out m_fromPhysicsTransform);
			m_shapes = new List<PhysicsShape>();
			RebuildShapes();
        }

        protected override void ReInit(in TComponentData properties)
        {
            RebuildShapes();
        }

        protected override void OnShutdown()
        {
			// Remove the old shapes
			if (m_physics != null)
			{
				var obj = m_physics.Object;
				foreach (var shape in m_shapes)
				{
					obj.RemoveShape(shape);
				}
				m_physics = null;
			}

			// Delete the old shapes
			foreach (var shape in m_shapes)
			{
				shape.Dispose();
			}
			m_shapes.Clear();
			m_shapes = null;
        }

		private void FindPhysics(Entity entityToIgnore=null)
		{
			Matrix4 newTransform;
			var newPhysics = FindRootPhysicsComponent(entityToIgnore, out newTransform);
			MoveShapes(newPhysics, newTransform);
		}

		public void OnAncestryChanged()
		{
			FindPhysics();
		}

		public void OnComponentAdded(ComponentBase component)
		{
			if (component is PhysicsComponent)
			{
	            FindPhysics();
			}
		}

		public void OnComponentAdded(Entity ancestor, ComponentBase component)
		{
			if (component is PhysicsComponent || component is TransformComponent)
			{
				FindPhysics();
			}
		}

		public void OnComponentRemoved(ComponentBase component)
		{
			if (component == m_physics)
			{
				MoveShapes(null, Matrix4.Identity);
			}
		}

		public void OnComponentRemoved(Entity ancestor, ComponentBase component)
		{
			if (component == m_physics)
			{
                MoveShapes(null, Matrix4.Identity);
			}
			else if(component is TransformComponent)
			{
				FindPhysics(ancestor);
			}
		}

		private void MoveShapes(PhysicsComponent newPhysics, Matrix4 newTransform)
		{
			if (newPhysics != m_physics)
			{
				if (m_physics != null)
				{
					var obj = m_physics.Object;
					obj.RemoveShapes(m_shapes);
				}
				var oldToNewTransform = m_fromPhysicsTransform.InvertAffine() * newTransform;
				foreach (var shape in m_shapes)
				{
					shape.Transform = shape.Transform * oldToNewTransform;
				}
				if (newPhysics != null)
				{
					var obj = newPhysics.Object;
					obj.AddShapes(m_shapes);
				}
				m_physics = newPhysics;
				m_fromPhysicsTransform = newTransform;
			}
		}

		protected abstract void CreateShapes(PhysicsWorld world, List<PhysicsShape> o_shapes);

        protected void RebuildShapes()
        {
			// Remove the old shapes
			var obj = (m_physics != null) ? m_physics.Object : null;
			if (obj != null)
			{
				obj.RemoveShapes(m_shapes);
			}

			// Delete the old shapes
			foreach (var shape in m_shapes)
			{
				shape.Dispose();
			}
			m_shapes.Clear();

			// Create the new shapes
			CreateShapes(m_world, m_shapes);
			foreach (var shape in m_shapes)
			{
				shape.Transform = m_fromPhysicsTransform.ToWorld(shape.Transform);
			}

			// Add the new shapes
			if (obj != null)
			{
				obj.AddShapes(m_shapes);
			}
        }
	}
}
