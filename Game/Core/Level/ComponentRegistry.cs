using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dan200.Core.Util;
using System.Linq;
using System.Text;
using Dan200.Core.Interfaces;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Level
{
    internal static class ComponentRegistry
    {
        private class RegisteredSystem
        {
            public int ID;
            public string Name;
            public Type Type;
            public BitField RequiredSystems;
            public BitField DependendedOnSystems;
            public BitField DependentSystems;
            public BitField ImplementedInterfaces;
            public bool Finalised;

            public override string ToString()
            {
                return Name;
            }
        }

		private class RegisteredComponent
		{
			public int ID;
			public string Name;
			public Type Type;
            public StructLayout DataLayout;
            public BitField RequiredSystems;
            public BitField RequiredComponents;
            public BitField DependendedOnComponents;
            public BitField DependentComponents;
            public BitField ImplementedInterfaces;
            public bool Finalised;

            public override string ToString()
            {
                return Name;
            }
        }

        private class RegisteredInterface
        {
            public int ID;
            public Type Type;
            public BitField ImplementingSystems;
            public BitField ImplementingComponents;

            public override string ToString()
            {
                return Type.Name;
            }
        }

        private static Dictionary<string, int> s_systemNameToId = new Dictionary<string, int>();
        private static Dictionary<Type, int> s_systemTypeToId = new Dictionary<Type, int>();
        private static List<RegisteredSystem> s_registeredSystems = new List<RegisteredSystem>();

        private static Dictionary<string, int> s_componentNameToId = new Dictionary<string, int>();
		private static Dictionary<Type, int> s_componentTypeToId = new Dictionary<Type, int>();
		private static List<RegisteredComponent> s_registeredComponents = new List<RegisteredComponent>();

        private static Dictionary<Type, int> s_interfaceTypeToId = new Dictionary<Type, int>();
        private static List<RegisteredInterface> s_registeredInterfaces = new List<RegisteredInterface>();

        private static bool s_finalised = false;

        public static int SystemCount
        {
            get
            {
                App.Assert(s_finalised);
                return s_registeredSystems.Count;
            }
        }

        public static int ComponentCount
		{
			get
			{
				App.Assert(s_finalised);
				return s_registeredComponents.Count;
			}
		}

        public static void Reset()
        {
            s_systemNameToId.Clear();
            s_systemTypeToId.Clear();
            s_registeredSystems.Clear();

            s_componentNameToId.Clear();
            s_componentTypeToId.Clear();
			s_registeredComponents.Clear();

            s_interfaceTypeToId.Clear();
            s_registeredComponents.Clear();

			s_finalised = false;
        }

        public static void RegisterSystem<TSystem>(string name) where TSystem : SystemBase
        {
            App.Assert(!s_finalised);

            var type = typeof(TSystem);
            if (s_systemTypeToId.ContainsKey(type))
            {
                throw new IOException("System type " + type.Name + " is already registered");
            }
            if (s_systemNameToId.ContainsKey(name))
            {
                throw new IOException("System name " + name + " is already in use");
            }

            var id = s_registeredSystems.Count;
            s_systemTypeToId.Add(type, id);
            s_systemNameToId.Add(name, id);

            var registeredSystem = new RegisteredSystem();
            registeredSystem.ID = id;
            registeredSystem.Name = name;
            registeredSystem.Type = type;
            s_registeredSystems.Add(registeredSystem);
        }

        public static void RegisterComponent<TComponent, TComponentData>(string name)
            where TComponent : Component<TComponentData>, new()
            where TComponentData : struct
        {
			App.Assert(!s_finalised);

            var type = typeof(TComponent);
            if (s_componentTypeToId.ContainsKey(type))
            {
                throw new IOException("Component type " + type.Name + " is already registered");
            }
            if (s_componentNameToId.ContainsKey(name))
            {
                throw new IOException("Component name " + name + " is already in use");
            }

			var id = s_registeredComponents.Count;
			s_componentTypeToId.Add(type, id);
            s_componentNameToId.Add(name, id);

			var registeredComponent = new RegisteredComponent();
			registeredComponent.ID = id;
			registeredComponent.Name = name;
			registeredComponent.Type = type;
            registeredComponent.DataLayout = StructLayout.Get(typeof(TComponentData));
			s_registeredComponents.Add(registeredComponent);
        }

        private static HashSet<RegisteredSystem> s_systemFinaliseInProgress = new HashSet<RegisteredSystem>();
        private static HashSet<RegisteredComponent> s_componentFinaliseInProgress = new HashSet<RegisteredComponent>();

        private static void FinaliseSystem(RegisteredSystem system, List<RegisteredSystem> o_updateOrder)
        {
            App.Assert(!system.Finalised);
            s_systemFinaliseInProgress.Add(system);
            try
            {
                // Merge requirements and dependencies
                var dependencies = system.DependendedOnSystems;
                foreach (var dependencyID in dependencies)
                {
                    var dependencySystem = s_registeredSystems[dependencyID];
                    if (!dependencySystem.Finalised)
                    {
                        if (s_systemFinaliseInProgress.Contains(dependencySystem))
                        {
                            throw new Exception(string.Format(
                                "Circular dependency detected between systenms {0} and {1}",
                                system.Name,
                                dependencySystem.Name
                            ));
                        }
                        FinaliseSystem(dependencySystem, o_updateOrder);
                    }
                    system.DependendedOnSystems |= dependencySystem.DependendedOnSystems;
                    if (system.RequiredSystems[dependencyID])
                    {
                        system.RequiredSystems |= dependencySystem.RequiredSystems;
                    }
                }

                // Finish
                system.Finalised = true;
                o_updateOrder.Add(system);
            }
            finally
            {
                s_systemFinaliseInProgress.Remove(system);
            }
        }

        private static void FinaliseComponent(RegisteredComponent component, List<RegisteredComponent> o_updateOrder)
		{
            App.Assert(!component.Finalised);
            s_componentFinaliseInProgress.Add(component);
			try
			{
                // Merge requirements and dependencies
                var dependencies = component.DependendedOnComponents;
                foreach(var dependencyID in dependencies)
                {
                    var dependencyComponent = s_registeredComponents[dependencyID];
                    if(!dependencyComponent.Finalised)
                    {
                        if (s_componentFinaliseInProgress.Contains(dependencyComponent))
                        {
                            throw new Exception(string.Format(
                                "Circular dependency detected between components {0} and {1}",
                                component.Name,
                                dependencyComponent.Name
                            ));
                        }
                        FinaliseComponent(dependencyComponent, o_updateOrder);
                    }
                    component.DependendedOnComponents |= dependencyComponent.DependendedOnComponents;
                    if(component.RequiredComponents[dependencyID])
                    {
                        component.RequiredComponents |= dependencyComponent.RequiredComponents;
                    }
                }

                // Check editable components only depend on other editable components
                var editableComponents = GetComponentsImplementingInterface<IEditable>();
                if(editableComponents[component.ID])
                {
                    var nonEditableDependencies = component.RequiredComponents & ~editableComponents;
                    if (!nonEditableDependencies.IsEmpty)
                    {
                        throw new Exception(string.Format(
                            "Component {0} is editable but requires component {1} which is not",
                            component.Name,
                            LookupComponent(nonEditableDependencies.First()).Name
                        ));
                    }
                }

                // Finish
				component.Finalised = true;
				o_updateOrder.Add(component);
			}
			finally
			{
                s_componentFinaliseInProgress.Remove(component);
			}
		}
        
        private static int RegisterInterface(Type interfaceType)
        {
            int id;
            if (!s_interfaceTypeToId.TryGetValue(interfaceType, out id))
            {
                id = s_registeredInterfaces.Count;
                s_interfaceTypeToId[interfaceType] = id;

                var registeredInterface = new RegisteredInterface();
                registeredInterface.ID = id;
                registeredInterface.Type = interfaceType;
                registeredInterface.ImplementingComponents = new BitField();
                s_registeredInterfaces.Add(registeredInterface);
            }
            return id;
        }

        public static void Finalise()
		{
			App.Assert(!s_finalised);
			s_finalised = true;

            // Collect information about systens abd components from their attributes
            var iInterface = typeof(IInterface);
            var iComponentInterface = typeof(IComponentInterface);
            foreach(var system in s_registeredSystems)
            {
                // Get requirements
                foreach (var attribute in system.Type.GetCustomAttributes<RequireSystemAttribute>())
                {
                    var id = GetSystemID(attribute.RequiredType);
                    system.RequiredSystems[id] = true;
                }

                // Get after dependencies
                system.DependendedOnSystems |= system.RequiredSystems;
                foreach (var attribute in system.Type.GetCustomAttributes<AfterSystemAttribute>())
                {
                    var id = GetSystemID(attribute.DependentType);
                    system.DependendedOnSystems[id] = true;
                }

                // Get before dependencies
                foreach (var attribute in system.Type.GetCustomAttributes<BeforeSystemAttribute>())
                {
                    var id = GetSystemID(attribute.DependentType);
                    var otherSystem = s_registeredSystems[id];
                    otherSystem.DependendedOnSystems[system.ID] = true;
                }

                // Don't allow systems to rely on components (systems always advance before components)
                App.Assert(system.Type.GetCustomAttributes<RequireComponentAttribute>().Count() == 0);
                App.Assert(system.Type.GetCustomAttributes<AfterComponentAttribute>().Count() == 0);

				// Get interfaces
				system.ImplementedInterfaces = new BitField();
                foreach (var interfaceType in system.Type.GetInterfaces())
                {
                    if (iInterface.IsAssignableFrom(interfaceType) &&
                        interfaceType != iInterface &&
                        interfaceType != iComponentInterface)
                    {
                        // Register the interface
                        int id = RegisterInterface(interfaceType);

                        // Record that this system implements the interface
                        system.ImplementedInterfaces[id] = true;

                        // Record that this interface is implemented by the system
                        var _interface = s_registeredInterfaces[id];
                        _interface.ImplementingSystems[system.ID] = true;
                    }
                }
            }
            foreach (var component in s_registeredComponents)
            {
                // Get requirements
                foreach (var attribute in component.Type.GetCustomAttributes<RequireComponentAttribute>())
                {
                    var id = GetComponentID(attribute.RequiredType);
                    component.RequiredComponents[id] = true;
                }

                // Get after dependencies
                component.DependendedOnComponents |= component.RequiredComponents;
                foreach (var attribute in component.Type.GetCustomAttributes<AfterComponentAttribute>())
                {
                    var id = GetComponentID(attribute.DependentType);
                    var otherComponent = s_registeredComponents[id];
                    component.DependendedOnComponents[id] = true;
                }

                // Get before dependencies
                foreach(var attribute in component.Type.GetCustomAttributes<BeforeComponentAttribute>())
                {
                    var id = GetComponentID(attribute.DependentType);
                    var otherComponent = s_registeredComponents[id];
                    otherComponent.DependendedOnComponents[component.ID] = true;
                }

                // Get system requirements
                foreach (var attribute in component.Type.GetCustomAttributes<RequireSystemAttribute>())
                {
                    var id = GetSystemID(attribute.RequiredType);
                    component.RequiredSystems[id] = true;
                }

                // Don't allow systems to rely on components (systems always advance before components)
                App.Assert(component.Type.GetCustomAttributes<BeforeSystemAttribute>().Count() == 0);

                // Get interfaces
				component.ImplementedInterfaces = new BitField();
                foreach (var interfaceType in component.Type.GetInterfaces())
                {
                    if (iInterface.IsAssignableFrom(interfaceType) &&
                        interfaceType != iInterface &&
                        interfaceType != iComponentInterface)
                    {
                        // Register the interface
                        int id = RegisterInterface(interfaceType);

                        // Record that this component implements the interface
                        component.ImplementedInterfaces[id] = true;

                        // Record that this interface is implemented by the component
                        var _interface = s_registeredInterfaces[id];
                        _interface.ImplementingComponents[component.ID] = true;
                    }
                }
            }

            // Finalise each system (merge dependencies and produce an update order)
            var systemUpdateOrder = new List<RegisteredSystem>(s_registeredSystems.Count);
            foreach(var system in s_registeredSystems)
            {
                if(!system.Finalised)
                {
                    FinaliseSystem(system, systemUpdateOrder);
                }
                foreach(var dependencyID in system.DependendedOnSystems)
                {
                    s_registeredSystems[dependencyID].DependentSystems[system.ID] = true;
                }
            }

            // Finalise each component (merge dependencies and produce an update order)
            var componentUpdateOrder = new List<RegisteredComponent>(s_registeredComponents.Count);
			foreach (var component in s_registeredComponents)
			{
				if (!component.Finalised)
				{
					FinaliseComponent(component, componentUpdateOrder);
				}
                foreach (var dependencyID in component.DependendedOnComponents)
                {
                    s_registeredComponents[dependencyID].DependentComponents[component.ID] = true;
                }
            }

            // Re-order the systems
            App.Assert(systemUpdateOrder.Count == s_registeredSystems.Count);
            var oldToNewSystemID = new Dictionary<int, int>();
            for (int i = 0; i < systemUpdateOrder.Count; ++i)
            {
                var system = systemUpdateOrder[i];
                var newID = i;
                oldToNewSystemID[system.ID] = newID;
                system.ID = newID;
                s_systemTypeToId[system.Type] = newID;
                s_systemNameToId[system.Name] = newID;
            }
            s_registeredSystems = systemUpdateOrder;

            // Re-order the components
            App.Assert(componentUpdateOrder.Count == s_registeredComponents.Count);
            var oldToNewComponentID = new Dictionary<int, int>();
            for (int i=0; i<componentUpdateOrder.Count; ++i)
			{
				var component = componentUpdateOrder[i];
				var newID = i;
                oldToNewComponentID[component.ID] = newID;
                component.ID = newID;
				s_componentTypeToId[component.Type] = newID;
				s_componentNameToId[component.Name] = newID;
			}
			s_registeredComponents = componentUpdateOrder;

            // Rebuild the requirement masks (as the IDs will have changed)
            Func<BitField, Dictionary<int, int>, BitField> Remap = delegate (BitField mask, Dictionary<int, int> oldToNewID)
            {
                var result = new BitField();
                foreach (var oldID in mask)
                {
                    var newID = oldToNewID[oldID];
                    result[newID] = true;
                }
                return result;
            };
            foreach (var system in s_registeredSystems)
            {
                system.RequiredSystems = Remap(system.RequiredSystems, oldToNewSystemID);
                system.DependendedOnSystems = Remap(system.DependendedOnSystems, oldToNewSystemID);
                system.DependentSystems = Remap(system.DependentSystems, oldToNewSystemID);
            }
            foreach (var component in s_registeredComponents)
			{
                component.RequiredSystems = Remap(component.RequiredSystems, oldToNewSystemID);
                component.RequiredComponents = Remap(component.RequiredComponents, oldToNewComponentID);
                component.DependendedOnComponents = Remap(component.DependendedOnComponents, oldToNewComponentID);
                component.DependentComponents = Remap(component.DependentComponents, oldToNewComponentID);
			}
            foreach (var componentInterface in s_registeredInterfaces)
            {
                componentInterface.ImplementingSystems = Remap(componentInterface.ImplementingSystems, oldToNewSystemID);
                componentInterface.ImplementingComponents = Remap(componentInterface.ImplementingComponents, oldToNewComponentID);
            }

			// Print some debug stuff
			var builder = new StringBuilder();
			builder.Append("System update order:");
            foreach (var system in s_registeredSystems)
            {
				builder.Append(" " + system.Name + ",");
            }
			builder.Remove(builder.Length - 1, 1);
			App.LogDebug(builder.ToString());

			builder.Clear();
			builder.Append("Component update order:");
			foreach (var component in s_registeredComponents)
            {
				builder.Append(" " + component.Name + ",");
            }
			builder.Remove(builder.Length - 1, 1);
			App.LogDebug(builder.ToString());
        }

        public static int GetComponentID(string typeName)
        {
			App.Assert(s_finalised);
            int typeID;
            if (s_componentNameToId.TryGetValue(typeName, out typeID))
            {
                return typeID;
            }
            else
            {
                return -1;
            }
        }

        public static int GetComponentID(ComponentBase component)
        {
			return GetComponentID(component.GetType());
        }

		public static int GetComponentID<TComponent>() where TComponent : ComponentBase
		{
			return GetComponentID(typeof(TComponent));
		}

		private static int GetComponentID(Type type)
		{
			App.Assert(s_finalised);
			int id;
            if (s_componentTypeToId.TryGetValue(type, out id))
            {
                return id;
            }
            else
            {
				return -1;
            }
		}

        public static string GetComponentName(int id)
        {
			return LookupComponent(id).Name;
        }

        public static Type GetComponentType(int id)
        {
            return LookupComponent(id).Type;
        }

        public static StructLayout GetComponentDataLayout(int id)
        {
            return LookupComponent(id).DataLayout;
        }
       
        public static ComponentBase InstantiateComponent(int id)
		{
			var type = LookupComponent(id).Type;
            try
            {
                return (ComponentBase)Activator.CreateInstance(type);
            }
            catch (TargetInvocationException e)
            {
                throw App.Rethrow(e.InnerException);
            }
		}

        public static BitField GetRequiredSystems(int componentID)
        {
            return LookupComponent(componentID).RequiredSystems;
        }

        public static BitField GetRequiredComponents(int componentID)
		{
			return LookupComponent(componentID).RequiredComponents;
		}

        public static BitField GetDependedOnComponents(int componentID)
        {
            return LookupComponent(componentID).DependendedOnComponents;
        }

        public static BitField GetDependentComponents(int componentID)
        {
            return LookupComponent(componentID).DependentComponents;
        }

        public static BitField GetComponentsImplementingInterface<TInterface>() where TInterface : IInterface
        {
            var type = typeof(TInterface);
            int id;
            if(s_interfaceTypeToId.TryGetValue(type, out id))
            {
                return s_registeredInterfaces[id].ImplementingComponents;
            }
            return BitField.Empty;
        }

		private static RegisteredComponent LookupComponent(int id)
		{
			App.Assert(s_finalised);
			if (id >= 0 && id < s_registeredComponents.Count)
			{
				return s_registeredComponents[id];
			}
			else
			{
                throw new IOException("Unrecognised component ID: " + id);
			}
		}

        public static int GetSystemID(string typeName)
        {
            App.Assert(s_finalised);
            int typeID;
            if (s_systemNameToId.TryGetValue(typeName, out typeID))
            {
                return typeID;
            }
            else
            {
				return -1;
            }
        }

        public static int GetSystemID(SystemBase system)
        {
            return GetSystemID(system.GetType());
        }

        public static int GetSystemID<TSystem>() where TSystem : SystemBase
        {
            return GetSystemID(typeof(TSystem));
        }

        private static int GetSystemID(Type type)
        {
            App.Assert(s_finalised);
            int id;
            if (s_systemTypeToId.TryGetValue(type, out id))
            {
                return id;
            }
            else
            {
                throw new IOException("System type " + type.Name + " is not registered");
            }
        }

        public static string GetSystemName(int id)
        {
            return LookupSystem(id).Name;
        }

        public static BitField GetSystemsRequiredBySystem(int systemID)
        {
            return LookupSystem(systemID).RequiredSystems;
        }

        public static BitField GetSystemsImplementingInterface<TInterface>() where TInterface : IInterface
        {
            var type = typeof(TInterface);
            int id;
            if (s_interfaceTypeToId.TryGetValue(type, out id))
            {
                return s_registeredInterfaces[id].ImplementingSystems;
            }
            return BitField.Empty;
        }

        private static RegisteredSystem LookupSystem(int id)
        {
            App.Assert(s_finalised);
            if (id >= 0 && id < s_registeredSystems.Count)
            {
                return s_registeredSystems[id];
            }
            else
            {
                throw new IOException("Unrecognised system ID: " + id);
            }
        }
    }
}
