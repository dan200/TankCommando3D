using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Dan200.Core.Util;
using Dan200.Core.Multiplayer;
using Dan200.Core.Lua;
using Dan200.Core.Components;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Components.Core;

namespace Dan200.Core.Level
{
    internal struct EntityCreationInfo
    {
        public int ID;
        public BitField Components;
        public Dictionary<int, LuaTable> ComponentProperties;

        public void AddComponent(int id, LuaTable properties)
        {
            Components[id] = true;
            ComponentProperties[id] = properties;
        }

        public void RemoveComponent(int id)
        {
            Components[id] = false;
            ComponentProperties.Remove(id);
        }
    }

    internal class EntityCollection : IReadOnlyCollection<Entity>
    {
        private ComponentCollection m_components;
        private List<Entity> m_entities;
        private Dictionary<int, Entity> m_entitiesByID;
        private int m_nextUnusedID;

        public Level Level
        {
            get
            {
                return m_components.Level;
            }
        }

        public int Count
        {
            get
            {
                return m_entities.Count;
            }
        }

        public EntityCollection(Level level, ComponentCollection components)
        {
            App.Assert(level == components.Level);
            m_components = components;
            m_entities = new List<Entity>();
            m_entitiesByID = new Dictionary<int, Entity>();
            m_nextUnusedID = 1;
        }

        public Entity Lookup(int id)
        {
            Entity entity;
            if (m_entitiesByID.TryGetValue(id, out entity))
            {
                return entity;
            }
            return null;
        }

        public Entity Create()
        {
            return Create(AssignID());
        }

        public Entity Create(int id)
        {
            App.Assert(id != 0, "Attempt to add an entity with ID 0");
            App.Assert(!m_entitiesByID.ContainsKey(id), "Attempt to reuse entity ID " + id);

            // Create the entity
            var entity = new Entity();

            // Add the entity
            m_entities.Add(entity);
            m_entitiesByID.Add(id, entity);
            entity.Init(id, m_components);
            if (id >= m_nextUnusedID)
            {
                m_nextUnusedID = id + 1;
            }

            // Return the entity
            return entity;
        }

        public void Create(List<EntityCreationInfo> entities)
        {
            // Create the entities
            var usedComponents = new BitField();
            foreach (var entityInfo in entities)
            {
                Create(entityInfo.ID);
                usedComponents |= entityInfo.Components;
            }

            foreach (var componentID in usedComponents)
            {
				// Add the components
                foreach (var entityInfo in entities)
                {
                    if (entityInfo.Components[componentID])
                    {
                        var entity = Lookup(entityInfo.ID);
                        entity.AddComponent(componentID, null, false);
                    }
                }

				// Initialise the components
				// Doing this after addition allows different components of the same type to get each other during Init()
				foreach (var entityInfo in entities)
				{
					if (entityInfo.Components[componentID])
					{
						// Initialise
						var entity = Lookup(entityInfo.ID);
						var component = entity.GetComponent(componentID);
						component.Init(entity, entityInfo.ComponentProperties[componentID]);
					}
				}

				// Notify listeners
				// Do this after all components are added to avoid listeners accessing uninitialised members
				foreach (var entityInfo in entities)
				{
					if (entityInfo.Components[componentID])
					{
						var entity = Lookup(entityInfo.ID);
						var component = entity.GetComponent(componentID);
						foreach (var listener in entity.GetComponentsWithInterface<IComponentListener>())
						{
							if (listener != component)
							{
								listener.OnComponentAdded(component);
							}
						}
                    }
                }
            }
        }

		private void AddChildren(HierarchyComponent hierarchy, List<Entity> o_entities)
		{
			if (hierarchy != null)
			{
				foreach (var child in hierarchy.Children)
				{
					o_entities.Add(child);
					AddChildren(child.GetComponent<HierarchyComponent>(), o_entities);
				}
			}
		}

		public void Destroy(Entity entity, bool includeChildren=true)
        {
            App.Assert(entity.Level == Level, "The entity is from another level");
            App.Assert(!entity.Dead, "The entity is already dead");
            App.Assert(m_entities.Contains(entity));
			App.Assert(m_entitiesByID.ContainsKey(entity.ID));

			var hierarchy = includeChildren ? entity.GetComponent<HierarchyComponent>() : null;
			if (hierarchy != null && hierarchy.HasChildren)
			{
				// Destroy the entity and its children
				var allEntities = new List<Entity>();
				allEntities.Add(entity);
				Destroy(allEntities, true);
			}
			else
			{
				// Destroy just this entity
				var id = entity.ID;
				entity.Shutdown();
                m_entities.UnorderedRemove(entity);
                m_entitiesByID.Remove(id);
            }
        }

		public void Destroy(List<Entity> entities, bool includeChildren=true)
		{
			// Find the components used by the entities
			var initialCount = entities.Count;
			var usedComponents = new BitField();
			for (int i = 0; i < initialCount; ++i)
			{
				var entity = entities[i];
                App.Assert(entity.Level == Level, "The entity is from another level");
				App.Assert(!entity.Dead, "The entity is already dead");
				App.Assert(entities.IndexOf(entity, i + 1) < 0, "The same entity has been requested for deletion twice");
				App.Assert(m_entities.Contains(entity));
				App.Assert(m_entitiesByID.ContainsKey(entity.ID));
				usedComponents |= entity.ComponentsMask;
			}

			if (includeChildren)
			{
				// Add the entities children and their components
				for (int i = 0; i < initialCount; ++i)
				{
					var entity = entities[i];
					AddChildren(entity.GetComponent<HierarchyComponent>(), entities);
				}
				for (int i = initialCount; i < entities.Count; ++i)
				{
					var entity = entities[i];
					usedComponents |= entity.ComponentsMask;
				}
			}

			// Remove them all one by one, in reverse order
			foreach (var componentID in usedComponents.Reversed)
			{
                foreach (var entity in entities)
                {
					if (entity.ComponentsMask[componentID])
                    {
						entity.RemoveComponent(componentID);
                    }
                }
			}

			// Destroy the entities
			foreach (var entity in entities)
			{
				if (!entity.Dead) // Finding children might have caused us to find an entity that was already in our list
				{
                    App.Assert(m_entities.Contains(entity));
					var id = entity.ID;
					entity.Shutdown();
                    m_entities.UnorderedRemove(entity);
					m_entitiesByID.Remove(id);
				}
			}
		}

        internal void Clear()
        {
			// Remove all the components (in reverse order)
            if (m_components.GetComponentCount() > 0)
            {
                var entitiesWithComponent = new List<Entity>();
                for (int componentID = ComponentRegistry.ComponentCount - 1; componentID >= 0; --componentID)
                {
                    var count = m_components.GetComponentCount(componentID);
                    if (count > 0)
                    {
						entitiesWithComponent.Clear();
                        foreach (var component in m_components.GetComponents(componentID))
                        {
                            entitiesWithComponent.Add(component.Entity);
                        }
                        foreach (var component in m_components.GetNewComponents(componentID))
                        {
                            entitiesWithComponent.Add(component.Entity);
                        }
                        foreach (var entity in entitiesWithComponent)
                        {
							entity.RemoveComponent(componentID);
                        }
                    }
                }
                App.Assert(m_components.GetComponentCount() == 0);
            }

			// Shutdown all the entities
            foreach (var entity in m_entities)
            {
                entity.Shutdown();
            }
            m_entities.Clear();
            m_entitiesByID.Clear();
        }

        public List<Entity>.Enumerator GetEnumerator()
        {
            return m_entities.GetEnumerator();
        }

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int AssignID()
        {
            return AssignIDs(1);
        }

        public int AssignIDs(int count)
        {
            App.Assert(count >= 0);
            int first = m_nextUnusedID;
            m_nextUnusedID += count;
            return first;
        }
    }
}
