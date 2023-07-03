using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Core.Math;

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Dan200.Core.Main;
using System.Collections;
using Dan200.Core.Multiplayer;
using Dan200.Core.Lua;
using Dan200.Core.Async;
using System.Reflection;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Systems;
using Dan200.Core.Audio;
using System.Text;
using Dan200.Core.Components;

namespace Dan200.Core.Level
{
    internal class Level : IDisposable
    {
		private LevelData m_data;
        private Side m_side;
        private bool m_editor;

        private Clock m_clock;
        private EntityCollection m_entities;
        private SystemCollection m_systems;
        private ComponentCollection m_components;

        public LevelData Data
        {
            get
            {
                return m_data;
            }
            set
            {
                m_data = value;
            }
        }

        public Side Side
        {
            get
            {
                return m_side;
            }
        }

        public bool InEditor
        {
            get
            {
                return m_editor;
            }
            set
            {
                m_editor = value;
            }
        }

        public Clock Clock
        {
            get
            {
                return m_clock;
            }
        }

        public BitField SystemsMask
        {
            get
            {
                return m_systems.Mask;
            }
        }           

        public EntityCollection Entities
        {
            get
            {
                return m_entities;
            }
        }

        public Level(Side side)
        {
            m_data = null;
            m_side = side;
            m_editor = false;

            m_clock = new Clock();
            m_components = new ComponentCollection(this);
            m_systems = new SystemCollection(this, m_components);
            m_entities = new EntityCollection(this, m_components);
        }

        public void Dispose()
        {
            // Remove entities
            m_entities.Clear();
            App.Assert(m_entities.Count == 0);
			App.Assert(m_components.GetComponentCount() == 0);

            // Remove systems
            m_systems.Clear();
			App.Assert(m_systems.Mask.IsEmpty);
        }

        public void PromoteNewComponents()
        {
            m_components.RemoveDeadComponents();
            m_components.PromoteNewComponents();
        }

        public void Update(float dt, TaskQueue workers)
        {
            // Update time
            m_clock.Update(dt);
            float scaledDT = dt * m_clock.Rate;

            // Update systems
            foreach(var system in m_systems.GetSystemsWithInterface<IUpdate>())
            {
                system.Update(scaledDT);
            }

            // Update components
            foreach (var component in m_components.GetComponentsWithInterface<IUpdate>())
            {
                component.Update(scaledDT);
            }
        }

		public void DebugDraw(BitField systems, BitField components)
        {
			// Debug draw systems
			if (!systems.IsEmpty)
			{
				foreach (var system in GetSystemsWithInterface<IDebugDraw>())
				{
					var id = ComponentRegistry.GetSystemID(system as SystemBase);
					if (systems[id])
					{
						system.DebugDraw();
					}
				}
			}

			// Debug draw components
			if (!components.IsEmpty)
			{
				foreach (var component in GetComponentsWithInterface<IDebugDraw>())
				{
					var id = ComponentRegistry.GetComponentID(component as ComponentBase);
					if (components[id])
					{
						component.DebugDraw();
					}
				}
			}
        }

		public TSystem AddSystem<TSystem>(TSystem system, LevelSaveData save) where TSystem : SystemBase
		{
            LuaTable properties;
            var systemID = ComponentRegistry.GetSystemID<TSystem>();
            if (save == null || !save.SystemProperties.TryGetValue(systemID, out properties))
            {
                properties = LuaTable.Empty;
            }
			return AddSystem(system, properties);
		}

		public TSystem AddSystem<TSystem>(TSystem system, LuaTable properties=null) where TSystem : SystemBase
        {
            var systemID = ComponentRegistry.GetSystemID<TSystem>();
            m_systems.Add(systemID, system);
            system.Init(this, properties ?? LuaTable.Empty);
            return system;
        }

        public void RemoveSystem<TSystem>() where TSystem : SystemBase
        {
            var systemID = ComponentRegistry.GetSystemID<TSystem>();
            m_systems.Remove(systemID);
        }

        public TSystem GetSystem<TSystem>() where TSystem : SystemBase
        {
            var id = ComponentRegistry.GetSystemID<TSystem>();
            return m_systems.Get(id) as TSystem;
        }

        public SystemCollection.Systems GetSystems()
        {
            return m_systems.GetSystems();
        }

        public SystemCollection.SystemsWithInterface<TInterface> GetSystemsWithInterface<TInterface>() where TInterface : class, IInterface
        {
            return m_systems.GetSystemsWithInterface<TInterface>();
        }

        public ComponentCollection.Components GetComponents()
		{
			return m_components.GetComponents();
		}

        public ComponentCollection.Components<TComponent> GetComponents<TComponent>() where TComponent : ComponentBase
        {
            return m_components.GetComponents<TComponent>();
        }

        public ComponentCollection.ComponentsWithInterface<TInterface> GetComponentsWithInterface<TInterface>() where TInterface : class, IInterface
        {
            return m_components.GetComponentsWithInterface<TInterface>();
        }

        private void AddComponentTask<TInterface>(TaskQueue queue, Action<TInterface> action, BitField componentsWithInterface, int componentID, int[] io_dependencies) where TInterface : class, IInterface
        {
            queue.AddTask(delegate
            {
                // Update the components
                foreach (var component in m_components.GetComponents(componentID))
                {
                    action.Invoke(component as TInterface);
                }

                // Queue up more components that are now available
                var dependants = ComponentRegistry.GetDependentComponents(componentID) & componentsWithInterface;
                if(!dependants.IsEmpty)
                {
                    foreach (var dependantID in dependants)
                    {
                        ref int dependantDependencies = ref io_dependencies[dependantID];
                        App.Assert(dependantDependencies > 0);
                        if(Interlocked.Decrement(ref dependantDependencies) == 0)
                        {
                            AddComponentTask(queue, action, componentsWithInterface, dependantID, io_dependencies);
                        }
                    }
                }
            });
        }

        public void ForEachComponentWithInterfaceParallel<TInterface>(TaskQueue queue, Action<TInterface> action) where TInterface : class, IInterface
        {
            // Setup
            var componentsWithInterface = ComponentRegistry.GetComponentsImplementingInterface<TInterface>();
            var dependencies = new int[ComponentRegistry.ComponentCount];
            foreach (var componentID in componentsWithInterface)
            {
                var dependenciesWithInterface = ComponentRegistry.GetDependedOnComponents(componentID) & componentsWithInterface;
                dependencies[componentID] = dependenciesWithInterface.Count();
            }

            // Start initial tasks
            foreach(var componentID in componentsWithInterface)
            {
                if(dependencies[componentID] == 0)
                {
                    AddComponentTask(queue, action, componentsWithInterface, componentID, dependencies);   
                }
            }

            // Wait until all tasks are complete
            queue.WaitUntilEmpty();
        }

		public LevelSaveData Save()
        {
			var saveData = new LevelSaveData();
			saveData.LevelPath = Data.Path;

            /*
            // Save systems
            var savedProperties = new LuaTable();
            foreach (var system in m_systems.GetSystemsWithInterface<ISave>())
            {
                // Save the system
				system.Save(savedProperties);
                if (savedProperties.Count > 0)
                {
					// Store the system properties 
                    var systemID = ComponentRegistry.GetSystemID((SystemBase)system);
					saveData.SystemProperties[systemID] = savedProperties;
                    savedProperties = new LuaTable();
                }    
            }

            // Save entities
            foreach(var entity in m_entities)
            {
				// TODO
                App.Assert(entity.Data != null, "Cannot persist entity without an EntityData");
                App.Assert(entity.Properties != null, "Cannot persist entity without Properties");
                App.Assert(entity.ComponentsMask == entity.Data.Components, "Cannot persist entity with added/removed components");

				var entitySaveData = new LevelSaveData.EntitySaveData();
				entitySaveData.ID = entity.ID;
				entitySaveData.Type = entity.Data.Path;
				entitySaveData.Properties = entity.Properties;
				entitySaveData.ComponentProperties = new Dictionary<int, LuaTable>();
				foreach(var component in entity.GetComponentsWithInterface<ISave>())
				{
					// Save the component
					var savedProperties = new LuaTable();
					component.Save(savedProperties);

					if (savedProperties.Count > 0)
					{
						// Remove values that are unchanged from their defaults
						var componentID = ComponentRegistry.GetComponentID((Component)component);
						var defaultProperties = LuaTableUtils.InjectProperties(entity.Data.ComponentProperties[componentID], entity.Properties);
						var valuesToDelete = new List<LuaValue>();
						foreach (var pair in savedProperties)
						{
							if (pair.Key.IsString() && LuaValue.DeepEquals(defaultProperties[pair.Key], pair.Value))
							{
								valuesToDelete.Add(pair.Key);
							}
						}
						foreach (var value in valuesToDelete)
						{
							savedProperties[value] = LuaValue.Nil;
						}

						// Store the component properties
						if (savedProperties.Count > 0)
	                    {
							entitySaveData.ComponentProperties[componentID] = savedProperties;
	                    }
					}
                }
				saveData.Entities.Add(entitySaveData);
            }
            */

            return saveData;
        }
    }
}
