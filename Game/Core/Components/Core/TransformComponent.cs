using System;
using Dan200.Core.Animation;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Interfaces.Multiplayer;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Serialisation;
using Dan200.Game.Components.Editor;
using Dan200.Core.Multiplayer;

namespace Dan200.Core.Components.Core
{
    internal struct TransformComponentData
    {
        [Optional]
        public Vector3 Position;

        [Optional]
        public Vector3 Rotation;
    }

	[AfterComponent(typeof(HierarchyComponent))]
    internal class TransformComponent : EditableComponent<TransformComponentData>, IDebugDraw, IHierarchyListener, IReplicatable
    {
		private TransformComponent m_parent;

        public Matrix4 LocalTransform;
        public Vector3 LocalVelocity;
        public Vector3 LocalAngularVelocity;

		public Vector3 LocalPosition
		{
			get
			{
				return LocalTransform.Position;
			}
			set
			{
				LocalTransform.Position = value;
			}
		}

		public Matrix3 LocalRotation
        {
            get
            {
				return LocalTransform.Rotation;
            }
            set
            {
				LocalTransform.Rotation = value;
            }
        }

        public Matrix4 Transform
		{
			get
			{
				if (m_parent != null)
				{
					var result = LocalTransform;
					var parent = m_parent;
					do
					{
						result = parent.LocalTransform.ToWorld(result);
						parent = parent.m_parent;
					}
					while (parent != null);
					return result;
				}
				else
				{
					return LocalTransform;
				}
			}
            set
            {
                if(m_parent != null)
                {
                    LocalTransform = m_parent.Transform.ToLocal(value);
                }
                else
                {
                    LocalTransform = value;
                }
            }
		}

		public Vector3 Position
		{
			get
			{
				if (m_parent != null)
				{
					return m_parent.Transform.ToWorldPos(LocalPosition);
				}
				else
				{
					return LocalPosition;
				}
			}
            set
            {
                if (m_parent != null)
                {
                    LocalPosition = m_parent.Transform.ToLocalPos(value);
                }
                else
                {
                    LocalPosition = value;
                }
            }
		}

        public Vector3 Velocity
        {
            get
            {
                if(m_parent != null)
                {
                    var result = LocalVelocity;
                    var position = LocalPosition;
                    var parent = m_parent;
                    do
                    {
                        result = parent.LocalVelocity + parent.LocalAngularVelocity.Cross(position) + parent.LocalTransform.ToWorldDir(result);
                        position = parent.LocalTransform.ToWorldPos(position);
                        parent = parent.m_parent;
                    }
                    while (parent != null);
                    return result;
                }
                else
                {
                    return LocalVelocity;
                }
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                if (m_parent != null)
                {
                    var result = LocalAngularVelocity;
                    var parent = m_parent;
                    do
                    {
                        result = parent.LocalAngularVelocity + parent.LocalTransform.ToWorldDir(result);
                        parent = parent.m_parent;
                    }
                    while (parent != null);
                    return result;
                }
                else
                {
                    return LocalAngularVelocity;
                }
            }
        }

        public Matrix4 ParentTransform
        {
            get
            {
                if (m_parent != null)
                {
                    return m_parent.Transform;
                }
                else
                {
                    return Matrix4.Identity;
                }
            }
        }

        public Vector3 ParentPosition
        {
            get
            {
                if (m_parent != null)
                {
                    return m_parent.Position;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
        }

        protected override void OnInit(in TransformComponentData properties)
        {
            var parent = HierarchyComponent.GetParent(Entity); ;
            if (parent != null)
            {
                m_parent = parent.GetComponent<TransformComponent>();
            }
            Reset(properties);
        }

        protected override void Reset(in TransformComponentData properties)
        {
            LocalTransform = Matrix4.CreateTranslationScaleRotation(
                properties.Position,
                Vector3.One,
                properties.Rotation * Mathf.DEGREES_TO_RADIANS
            );
        }

        public override void AddManipulators(EditorComponent editor)
        {
            string positionProperty = null;
            string rotationProperty = null;
            foreach(var property in editor.Prefab.Properties)
            {
                var propertyName = property.Key;
                if(propertyName == "Position") // TODO: Match on field names, not property names!
                {
                    positionProperty = propertyName;
                }
                if (propertyName == "Rotation") // TODO: Match on field names, not property names!
                {
                    rotationProperty = propertyName;
                }
            }
            if (positionProperty != null || rotationProperty != null)
            {
                var manipulator = Entity.AddComponent<TransformManipulatorComponent>(LuaTable.Empty);
                manipulator.PositionProperty = positionProperty;
                manipulator.RotationProperty = rotationProperty;
            }
        }

        public override void RemoveManipulators(EditorComponent editor)
        {
            Entity.RemoveComponent<TransformManipulatorComponent>();
        }

        protected override void OnShutdown()
        {
        }

        public void Replicate(IReplicator replicator)
        {
            replicator.Replicate(ref LocalTransform);
            replicator.Replicate(ref LocalVelocity);
            replicator.Replicate(ref LocalAngularVelocity);
        }

		public void OnParentChanged(Entity oldParent, Entity newParent)
		{
			if (newParent != null)
			{
				m_parent = newParent.GetComponent<TransformComponent>();
			}
			else
			{
				m_parent = null;
			}
		}

        public void DebugDraw()
        {
			App.DebugDraw.DrawAxisMarker(Transform, 1.0f);
            App.DebugDraw.DrawLine(Position, Position + Velocity, Colour.Magenta);
        }
    }
}
