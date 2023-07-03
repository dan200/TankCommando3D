using System;
using System.Collections.Generic;
using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Components.Render;
using Dan200.Core.GUI;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Game;

namespace Dan200.Game.Components.Editor
{
    internal struct EditorComponentData
    {
        public string Prefab;
        public LuaTable Properties;
    }

    [RequireSystem(typeof(GUISystem))]
    [RequireComponentOnAncestor(typeof(PhysicsWorldComponent))]
    [AfterComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(ModelComponent))]
    internal class EditorComponent : Component<EditorComponentData>, IUpdate
    {
        private TransformComponent m_transform;
        private bool m_selected;

        private EntityPrefab m_prefab;
        private LuaTable m_properties;

        private PhysicsObject m_physics;
        private PhysicsBox m_shape;

        public bool Selected
        {
            get
            {
                return m_selected;
            }
            set
            {
                if(m_selected != value)
                {
                    if(m_selected)
                    {
                        RemoveManipulators();
                    }
                    m_selected = value;
                    if (m_selected)
                    {
                        AddManipulators();
                    }
                }
            }
        }

        public bool Hover
        {
            get;
            set;
        }

        public EntityPrefab Prefab
        {
            get
            {
                return m_prefab;
            }
        }

        public LuaTable Properties
        {
            get
            {
                return m_properties;
            }
        }

        protected override void OnInit(in EditorComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            m_prefab = EntityPrefab.Get( properties.Prefab );
            m_properties = properties.Properties;

            RemoveInvalidProperties();

            if (m_transform != null)
            {
                var physicsWorldComponent = Entity.GetComponentOnAncestor<PhysicsWorldComponent>();
                m_physics = physicsWorldComponent.World.CreateStaticObject(PhysicsMaterial.Default);
                m_physics.UserData = Entity;
                m_physics.Transform = m_transform.Transform;

                var model = Entity.GetComponent<ModelComponent>();
                if (model != null)
                {
                    var boundingBox = model.Instance.Model.BoundingBox;
                    m_shape = physicsWorldComponent.World.CreateBox(
                        Matrix4.CreateTranslation(boundingBox.Center),
                        boundingBox.Size
                    );
                }
                else
                {
                    m_shape = physicsWorldComponent.World.CreateBox(
                        Matrix4.Identity,
                        new Vector3(0.5f, 0.5f, 0.5f)
                    );
                }
                m_shape.Group = CollisionGroup.EditorSelectable;
                m_shape.UserData = Entity;
                m_physics.AddShape(m_shape);
            }
        }

        protected override void OnShutdown()
        {
            if(m_selected)
            {
                RemoveManipulators();
                m_selected = false;
            }
            if(m_physics != null)
            {
                m_physics.RemoveShape(m_shape);
                m_shape.Dispose();
                m_shape = null;

                m_physics.Dispose();
                m_physics = null;
            }
        }

        private void AddManipulators()
        {
            foreach (var editable in Entity.GetComponentsWithInterface<IEditable>())
            {
                editable.AddManipulators(this);
            }
        }

        private void RemoveManipulators()
        {
            foreach (var editable in Entity.GetComponentsWithInterface<IEditable>())
            {
                editable.RemoveManipulators(this);
            }
        }

        public void Update(float dt)
        {
            if (m_physics != null && m_transform != null)
            {
                m_physics.Transform = m_transform.Transform;
            }
            if (m_transform != null)
            {
                var shapeTransform = m_transform.Transform.ToWorld(m_shape.Transform);
                App.DebugDraw.DrawCross(shapeTransform, 0.25f, Colour.White);
                if (Selected || Hover)
                {
                    App.DebugDraw.DrawBox(shapeTransform, m_shape.Size, Colour.White);
                }
            }
        }

        private void RemoveInvalidProperties()
        {
            List<LuaValue> propertiesToRemove = null;
            foreach(var property in m_properties)
            {
                var key = property.Key;
                if(!key.IsString() ||
                   !(key.ToString() == "Name" || m_prefab.Properties.ContainsKey(key.GetString())))
                {
                    if(propertiesToRemove == null)
                    {
                        propertiesToRemove = new List<LuaValue>(m_properties.Count);
                    }
                    propertiesToRemove.Add(key);
                }
            }
            if(propertiesToRemove != null)
            {
                foreach(var key in propertiesToRemove)
                {
                    m_properties[key] = LuaValue.Nil;
                }
            }
        }

        public void ResetFromProperties()
        {
            var infos = new List<EntityCreationInfo>();
            m_prefab.SetupCreationInfo(Entity.ID, m_properties, infos);
            App.Assert(infos.Count >= 1);

            var resettableComponents = ComponentRegistry.GetComponentsImplementingInterface<IResettable>();
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                var entity = Level.Entities.Lookup(info.ID);
                App.Assert(entity != null);
                foreach(var componentID in (info.Components & resettableComponents))
                {
                    var component = entity.GetComponent(componentID) as IResettable;
                    App.Assert(component != null);
                    component.Reset(info.ComponentProperties[componentID]);
                }
            }
        }
    }
}
