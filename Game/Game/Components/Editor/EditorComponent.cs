using System;
using System.Collections.Generic;
using Dan200.Core.Components.Core;
using Dan200.Core.Components.Render;
using Dan200.Core.GUI;
using Dan200.Core.Interfaces;
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
    [RequireSystem(typeof(PhysicsSystem))]
    [AfterComponent(typeof(TransformComponent))]
    [AfterComponent(typeof(ModelComponent))]
    internal class EditorComponent : Component<EditorComponentData>, IUpdate
    {
        private TransformComponent m_transform;

        private EntityPrefab m_prefab;
        private LuaTable m_properties;

        private PhysicsObject m_physics;
        private PhysicsBox m_shape;

        public bool Selected
        {
            get;
            set;
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

            if (m_transform != null)
            {
                var physicsSystem = Level.GetSystem<PhysicsSystem>();
                m_physics = physicsSystem.World.CreateStaticObject(PhysicsMaterial.Default);
                m_physics.UserData = Entity;
                m_physics.Transform = m_transform.Transform;

                var model = Entity.GetComponent<ModelComponent>();
                if (model != null)
                {
                    var boundingBox = model.Instance.Model.BoundingBox;
                    m_shape = physicsSystem.World.CreateBox(
                        Matrix4.CreateTranslation(boundingBox.Center),
                        boundingBox.Size
                    );
                }
                else
                {
                    m_shape = physicsSystem.World.CreateBox(
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
            if(m_physics != null)
            {
                m_physics.RemoveShape(m_shape);
                m_shape.Dispose();
                m_shape = null;

                m_physics.Dispose();
                m_physics = null;
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

        public void ReInit()
        {
            var infos = new List<EntityCreationInfo>();
            m_prefab.SetupCreationInfo(Entity.ID, m_properties, infos);
            App.Assert(infos.Count >= 1);

            var editableComponents = ComponentRegistry.GetComponentsImplementingInterface<IEditable>();
            for (int i = 0; i < infos.Count; ++i)
            {
                var info = infos[i];
                var entity = Level.Entities.Lookup(info.ID);
                App.Assert(entity != null);
                foreach(var componentID in (info.Components & editableComponents))
                {
                    var component = entity.GetComponent(componentID) as IEditable;
                    App.Assert(component != null);
                    component.ReInit(info.ComponentProperties[componentID]);
                }
            }
        }
    }
}
