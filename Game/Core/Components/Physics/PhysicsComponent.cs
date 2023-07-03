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
using Dan200.Core.Interfaces.Physics;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Systems;
using System;
using Dan200.Core.Script;
using Dan200.Core.Serialisation;
using Dan200.Core.Components.Core;

namespace Dan200.Core.Components.Physics
{
    internal unsafe struct CollisionStartEventArgs
    {
        public readonly Entity HitEntity;
        private readonly bool* m_pIgnoreCollision;

        public bool IgnoreCollision
        {
            get
            {
                App.Assert(m_pIgnoreCollision != null);
                return *m_pIgnoreCollision;
            }
            set
            {
                App.Assert(m_pIgnoreCollision != null);
                *m_pIgnoreCollision = value;
            }
        }

        public CollisionStartEventArgs(Entity hitEntity, bool *pIgnoreCollision)
        {
            HitEntity = hitEntity;
            m_pIgnoreCollision = pIgnoreCollision;
        }
    }

    internal struct CollisionEndEventArgs
    {
        public readonly Entity HitEntity;

        public CollisionEndEventArgs(Entity hitEntity)
        {
            HitEntity = hitEntity;
        }
    }

    internal struct PhysicsComponentData
    {
        [Optional(Default = 1.0f)]
        [Range(Min = 0.0f)]
        public float Mass;

        [Optional]
        public string Material;

        [Optional(Default = false)]
        public bool Static;

        [Optional(Default = false)]
        public bool Kinematic;

        [Optional(Default = true)]
        public bool AutoSleep;

        [Optional(Default = false)]
        public bool IgnoreGravity;

        [Optional(Default = true)]
        public bool StartAwake;

        [Optional]
        public Vector3 InitialVelocity;

        [Optional]
        public Vector3 InitialAngularVelocity;
    }

    [RequireComponentOnAncestor(typeof(PhysicsWorldComponent))]
    [RequireComponent(typeof(TransformComponent))]
	[AfterComponent(typeof(HierarchyComponent))]
    internal class PhysicsComponent : EditableComponent<PhysicsComponentData>, IUpdate, IPhysicsUpdate, IHierarchyListener
    {
        private PhysicsWorldComponent m_world;
        private TransformComponent m_transform;

        private PhysicsObject m_object;
        private Matrix4 m_lastStepTransform;
        private bool m_isRoot;
        private bool m_kinematic;

        private HashSet<Entity> m_lastCollidingEntities;
        private HashSet<Entity> m_collidingEntities;
        private HashSet<Entity> m_ignoredEntities;

        public PhysicsObject Object
        {
            get
            {
                return m_object;
            }
        }

        public event StructEventHandler<PhysicsComponent, CollisionStartEventArgs> OnCollisionStart;
        public event StructEventHandler<PhysicsComponent, CollisionEndEventArgs> OnCollisionEnd;

        protected override void OnInit(in PhysicsComponentData properties)
        {
            m_world = Entity.GetComponentOnAncestor<PhysicsWorldComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();

            var materialPath = properties.Material;
            var material = (materialPath != null) ? PhysicsMaterial.Get(materialPath) : PhysicsMaterial.Default;

            bool isStatic = properties.Static || Level.InEditor;
            m_object = isStatic ? m_world.World.CreateStaticObject(material) : m_world.World.CreateObject(material);
            m_object.Kinematic = properties.Kinematic;
            m_object.Transform = m_transform.Transform;
            m_object.Mass = properties.Mass;
            m_object.OnContact += OnContact;
            m_object.UserData = Entity;
            m_object.Awake = properties.StartAwake;
            m_object.AutoSleep = properties.AutoSleep;
            m_object.IgnoreGravity = properties.IgnoreGravity;
            m_object.Velocity = properties.InitialVelocity;
            m_object.AngularVelocity = properties.InitialAngularVelocity;
			m_lastStepTransform = m_object.Transform;

            m_lastCollidingEntities = new HashSet<Entity>();
            m_collidingEntities = new HashSet<Entity>();
            m_ignoredEntities = new HashSet<Entity>();
			m_isRoot = true;
            m_kinematic = properties.Kinematic;

            var hierarchy = Entity.GetComponent<HierarchyComponent>();
			if (hierarchy != null)
			{
				UpdateIsRoot(hierarchy.Parent);
			}
        }

        protected override void Reset(in PhysicsComponentData properties)
        {
            App.Assert(Level.InEditor);
            App.Assert(m_object.Static);
            m_object.Transform = m_transform.Transform;
            m_lastStepTransform = m_object.Transform;

            var hierarchy = Entity.GetComponent<HierarchyComponent>();
            if (hierarchy != null)
            {
                UpdateIsRoot(hierarchy.Parent);
            }
        }

        protected override void OnShutdown()
        {
			App.Assert(m_object.Shapes.Count == 0);
            m_object.Dispose();
			m_object = null;
        }

        public void PhysicsUpdate(float dt)
        {
            if (m_collidingEntities.Count > 0 || m_lastCollidingEntities.Count > 0)
            {
                // Emit messages for collisions ended last tick
                foreach (var entity in m_lastCollidingEntities)
                {
                    if (!m_collidingEntities.Contains(entity))
                    {
                        if (OnCollisionEnd != null)
                        {
                            var args = new CollisionEndEventArgs(entity);
                            OnCollisionEnd.Invoke(this, args);
                        }
                        m_ignoredEntities.Remove(entity);
                    }
                }

                // Setup the collision lists for this frame
                var temp = m_lastCollidingEntities;
                m_lastCollidingEntities = m_collidingEntities;
                m_collidingEntities = temp;
                m_collidingEntities.Clear();
            }

            // Record pre-motion position
            if (!m_object.Static)
            {
                m_lastStepTransform = m_object.Transform;
            }
        }

        private unsafe void OnContact(PhysicsObject sender, ContactEventArgs e)
        {
            var hitEntity = (Entity)e.HitObject.UserData;
            if(m_collidingEntities.Add(hitEntity) && !m_lastCollidingEntities.Contains(hitEntity))
            {
                if (OnCollisionStart != null)
                {
                    bool ignoreCollision = false;
                    var args = new CollisionStartEventArgs(hitEntity, &ignoreCollision);
                    OnCollisionStart.Invoke(this, args);
                    if(ignoreCollision)
                    {
                        e.IgnoreContacts = true;
                        m_ignoredEntities.Add(hitEntity);
                    }
                }
            }
            else if(m_ignoredEntities.Contains(hitEntity))
            {
                e.IgnoreContacts = true;
            }
        }

		private void UpdateTransform(float dt)
		{
			if (!m_object.Kinematic)
			{
                // Set transform from physics transform
                App.Assert(m_isRoot);
                m_transform.LocalTransform = Matrix4.Lerp(
                    m_lastStepTransform, 
                    m_object.Transform, 
                    m_world.World.CurrentStepFraction
                );
                m_transform.LocalVelocity = m_object.Velocity;
                m_transform.LocalAngularVelocity = m_object.AngularVelocity;
			}
			else
			{
				// Set physics transform from transform
				m_object.Transform = m_transform.Transform;
                m_object.Velocity = m_transform.Velocity;
                m_object.AngularVelocity = m_transform.AngularVelocity;
			}
		}

		public void Update(float dt)
		{
			if (!m_object.Static)
			{
				UpdateTransform(dt);
			}
		}

		public void OnParentChanged(Entity oldParent, Entity newParent)
		{
			UpdateIsRoot(newParent);
		}

        private PhysicsComponent FindRootPhysicsComponent(Entity entity)
        {
            // Find the most distant ancestor with an unbroken chain of transform components
            App.Assert(entity != null);
            App.Assert(entity.GetComponent<TransformComponent>() != null);
            while (true)
            {
                var parent = HierarchyComponent.GetParent(entity);
                if (parent != null && parent.GetComponent<TransformComponent>() != null)
                {
                    entity = parent;
                }
                else
                {
                    break;
                }
            }

            // Get it's physics component
            return entity.GetComponent<PhysicsComponent>();
        }

        private void UpdateIsRoot(Entity parent)
		{
			if (parent == null || parent.GetComponent<TransformComponent>() == null)
			{
				// We are the root
				if (!m_isRoot)
				{
                    // Make dynamic
					m_isRoot = true;
					m_object.Kinematic = m_kinematic;
					m_object.Awake = true;
					UpdateTransform(0.0f);
				}
			}
			else
			{
				// We are no longer the root
				if (m_isRoot)
				{
                    // Make kinematic
                    m_isRoot = false;
					m_object.Kinematic = true;
                    UpdateTransform(0.0f);

                    // Transfer our old momentum to our new root
                    var rootPhysics = FindRootPhysicsComponent(parent);
                    if (rootPhysics != null)
                    {
                        App.Assert(rootPhysics != this);
                        var momentum = m_object.Mass * m_object.Velocity;
                        var centreOfMass = m_object.CenterOfMass;
                        rootPhysics.Object.ApplyImpulseAtPos(momentum, centreOfMass);
                    }
                }
            }
		}
	}
}
