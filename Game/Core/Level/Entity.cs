using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Multiplayer;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Util;
using System.Text;
using Dan200.Core.Components;
using Dan200.Core.Components.Core;

namespace Dan200.Core.Level
{
    internal sealed class Entity : IEquatable<Entity>
    {
        [Flags]
        private enum Flags
        {
            Initialised = 1,
            Visible = 2,
            Dead = 4,
        }

        private int m_id;
		private BitField m_componentsMask;
        private ComponentCollection m_components;
        private Flags m_flags;

        public int ID
        {
            get
            {
                return m_id;
            }
        }

        public Level Level
        {
            get
            {
                return m_components.Level;
            }
        }

        public BitField ComponentsMask
        {
            get
            {
                return m_componentsMask;
            }
        }

        public bool Visible
        {
            get
            {
                return (m_flags & Flags.Visible) != 0;
            }
            set
            {
                if (value)
                {
                    m_flags |= Flags.Visible;
                }
                else
                {
                    m_flags &= ~Flags.Visible;
                }
            }
        }

        public bool Dead
        {
            get
            {
                return (m_flags & Flags.Dead) != 0;
            }
        }

        public Entity()
        {
            m_id = 0;
            m_flags = Flags.Visible;
			m_componentsMask = new BitField();
            m_components = null;
        }

        internal void Init(int id, ComponentCollection components)
        {
            App.Assert((m_flags & Flags.Initialised) == 0);
            m_id = id;
            m_components = components;
            m_flags |= Flags.Initialised;
        }

        internal void Shutdown()
        {
            App.Assert((m_flags & Flags.Initialised) != 0);
            App.Assert((m_flags & Flags.Dead) == 0);

            foreach(int id in m_componentsMask.Reversed) // Shutdown components in reverse
            {
                // Remove the component
                var component = m_components.Get(this, id);
                component.Shutdown();
                m_componentsMask[id] = false;
                m_components.Remove(this, id);

                // Notify listeners
                foreach (var listener in GetComponentsWithInterface<IComponentListener>())
                {
                    App.Assert(listener != component);
                    listener.OnComponentRemoved(component);
                }

            }

            App.Assert(m_componentsMask.IsEmpty); // Ensure no components were re-added
            m_components = null;
            m_flags |= Flags.Dead;
        }

		public TComponent AddComponent<TComponent>(LuaTable properties, bool initialise=true) where TComponent : ComponentBase
        {
            var componentID = ComponentRegistry.GetComponentID<TComponent>();
			var component = AddComponentImpl(componentID) as TComponent;
            if (initialise)
            {
                InitComponentImpl(component, properties);
            }
            return component;
        }

        public ComponentBase AddComponent(int componentID, LuaTable properties, bool initialise = true)
        {
            var component = AddComponentImpl(componentID);
            if(initialise)
            {
                InitComponentImpl(component, properties);
            }
            return component;
        }

        private ComponentBase AddComponentImpl(int componentID)
        {
            App.Assert(componentID >= 0 && componentID < ComponentRegistry.ComponentCount);
            App.Assert((m_flags & Flags.Initialised) != 0);
            App.Assert((m_flags & Flags.Dead) == 0);

            // Check for duplicates
            if (m_componentsMask[componentID])
            {
                throw new Exception("Entity " + ID + " already contains component " + ComponentRegistry.GetComponentName(componentID));
            }

            // Check system requirements
            var systemRequirements = ComponentRegistry.GetRequiredSystems(componentID);
            var missingSystems = (~Level.SystemsMask & systemRequirements);
            if (!missingSystems.IsEmpty)
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append("Level is missing systems");
                foreach (var requiredID in missingSystems)
                {
                    errorMessage.Append(" " + ComponentRegistry.GetSystemName(requiredID));
                }
                errorMessage.Append(" required by component " + ComponentRegistry.GetComponentName(componentID) + " on entity " + ID);
                throw new Exception(errorMessage.ToString());
            }

            // Check component requirements
            var componentRequirements = ComponentRegistry.GetRequiredComponents(componentID);
            var existingComponents = m_componentsMask;
            var missingComponents = (~existingComponents & componentRequirements);
            if (!missingComponents.IsEmpty)
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append("Entity " + ID + " is missing components");
                foreach (var requiredID in missingComponents)
                {
                    errorMessage.Append(" " + ComponentRegistry.GetComponentName(requiredID));
                }
                errorMessage.Append(" required by component " + ComponentRegistry.GetComponentName(componentID));
                throw new Exception(errorMessage.ToString());
            }

            // Check ancestor component requirements
            var ancestorComponentRequirements = ComponentRegistry.GetRequiredComponentsOnAncestors(componentID);
            if(!ancestorComponentRequirements.IsEmpty)
            {
                var hierarchyComponent = GetComponent<HierarchyComponent>();
                var existingAncestorComponents = hierarchyComponent.ComponentsOnAncestorsMask;
                var missingAncestorComponents = (~existingAncestorComponents & ancestorComponentRequirements);
                if(!missingAncestorComponents.IsEmpty)
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.Append("Entity " + ID + " is missing ancestor components");
                    foreach (var requiredID in missingAncestorComponents)
                    {
                        errorMessage.Append(" " + ComponentRegistry.GetComponentName(requiredID));
                    }
                    errorMessage.Append(" required by component " + ComponentRegistry.GetComponentName(componentID));
                    throw new Exception(errorMessage.ToString());
                }
            }

            var ancestorOrSelfComponentRequirements = ComponentRegistry.GetRequiredComponentsOnAncestorsOrSelf(componentID);
            if (!ancestorOrSelfComponentRequirements.IsEmpty)
            {
                var hierarchyComponent = GetComponent<HierarchyComponent>();
                var existingAncestorOrSelfComponents = hierarchyComponent.ComponentsOnAncestorsMask | existingComponents;
                var missingAncestorOrSelfComponents = (~existingAncestorOrSelfComponents & ancestorComponentRequirements);
                if (!missingAncestorOrSelfComponents.IsEmpty)
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.Append("Entity " + ID + " is missing ancestor components");
                    foreach (var requiredID in missingAncestorOrSelfComponents)
                    {
                        errorMessage.Append(" " + ComponentRegistry.GetComponentName(requiredID));
                    }
                    errorMessage.Append(" required by component " + ComponentRegistry.GetComponentName(componentID));
                    throw new Exception(errorMessage.ToString());
                }
            }

            // Add the component
            var component = ComponentRegistry.InstantiateComponent(componentID);
            m_componentsMask[componentID] = true;
            m_components.Add(this, component);

            return component;
        }

        private void InitComponentImpl(ComponentBase component, LuaTable properties)
        {
			// Initialise the component
			component.Init(this, properties);

			// Let listeners know
			foreach (var listener in GetComponentsWithInterface<IComponentListener>())
			{
				if (listener != component)
				{
					listener.OnComponentAdded(component);
				}
			}
        }

        public void RemoveComponent<TComponent>() where TComponent : ComponentBase
        {
            var id = ComponentRegistry.GetComponentID<TComponent>();
            RemoveComponent(id);
        }

        public void RemoveComponent(int componentID)
        {
            App.Assert(componentID >= 0 && componentID < ComponentRegistry.ComponentCount);
            App.Assert((m_flags & Flags.Initialised) != 0);
            App.Assert((m_flags & Flags.Dead) == 0);

            if (!m_componentsMask[componentID])
            {
                // We don't have the component, nothing to do
                return;
            }

            // Check that no components depend on the component being removed
            foreach(var id in m_componentsMask)
            {
                var requirements = ComponentRegistry.GetRequiredComponents(id);
                if (requirements[componentID])
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.Append("Attempt to remove component " + ComponentRegistry.GetComponentName(componentID) + " required by components");
                    foreach(var id2 in m_componentsMask)
                    {
                        requirements = ComponentRegistry.GetRequiredComponents(id2);
                        if (requirements[componentID])
                        {
                            errorMessage.Append(" " + ComponentRegistry.GetComponentName(id2));
                        }
                    }
                    throw new Exception(errorMessage.ToString());
                }
            }

            // TODO: Check ancestor dependencies too!

			// Get the component
			var component = m_components.Get(this, componentID);
			App.Assert(component != null);

            // Remove the component
            component.Shutdown();
            m_componentsMask[componentID] = false;
            m_components.Remove(this, componentID);

            // Let listeners know
            foreach (var listener in GetComponentsWithInterface<IComponentListener>())
            {
                App.Assert(listener != component);
                listener.OnComponentRemoved(component);
            }
        }

        public TComponent GetComponentOnAncestor<TComponent>(bool includeSelf = false) where TComponent : ComponentBase
        {
            var hierarchy = GetComponent<HierarchyComponent>();
            if (hierarchy != null)
            {
                return hierarchy.GetAncestorWithComponent<TComponent>();
            }
            return null;
        }

        public TComponent GetComponent<TComponent>() where TComponent : ComponentBase
        {
            var id = ComponentRegistry.GetComponentID<TComponent>();
            App.Assert(id >= 0, string.Format("Component " + typeof(TComponent).Name + " is not registered"));
            return GetComponent(id) as TComponent;
        }

        public ComponentBase GetComponent(int id)
        {
            App.Assert((m_flags & Flags.Dead) == 0);
            if (m_componentsMask[id])
			{
                return m_components.Get(this, id);
			}
			return null;
        }

		public ComponentCollection.ComponentsOnEntity GetComponents()
        {
            App.Assert((m_flags & Flags.Dead) == 0);
            return m_components.GetComponents(this);
        }

		public ComponentCollection.ComponentsWithInterfaceOnEntity<TInterface> GetComponentsWithInterface<TInterface>() where TInterface : class, IInterface
		{
            App.Assert((m_flags & Flags.Dead) == 0);
            return m_components.GetComponentsWithInterface<TInterface>(this);
        }

        public override string ToString()
        {
			var name = GetComponent<NameComponent>();
            if (name != null && name.Name != null)
			{
				return string.Format("[{0} ({1})]", ID, name.Name);
			}
			else
			{
				return string.Format("[{0}]", ID);
			}
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public bool Equals(Entity other)
        {
            return other == this;
        }
    }
}
