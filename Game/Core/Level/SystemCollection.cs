using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Dan200.Core.Util;

namespace Dan200.Core.Level
{
    internal class SystemCollection
    {
        internal struct Systems : IEnumerable<SystemBase>
        {
            internal struct Enumerator : IEnumerator<SystemBase>
            {
                private readonly SystemCollection m_owner;
                private BitField.BitEnumerator m_enumerator;

                public SystemBase Current
                {
                    get
                    {
                        return m_owner.m_systems[m_enumerator.Current];
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(SystemCollection owner)
                {
                    var mask = owner.m_systemsMask;
                    m_owner = owner;
                    m_enumerator = mask.GetEnumerator();
                }

                public void Dispose()
                {
                    m_enumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return m_enumerator.MoveNext();
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly SystemCollection m_owner;

            public Systems(SystemCollection owner)
            {
                m_owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner);
            }

            IEnumerator<SystemBase> IEnumerable<SystemBase>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct SystemsWithInterface<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
        {
            internal struct Enumerator : IEnumerator<TInterface>
            {
                private readonly SystemCollection m_owner;
                private BitField.BitEnumerator m_enumerator;

                public TInterface Current
                {
                    get
                    {
                        return m_owner.m_systems[m_enumerator.Current] as TInterface;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(SystemCollection owner)
                {
                    var mask = owner.m_systemsMask & ComponentRegistry.GetSystemsImplementingInterface<TInterface>();
                    m_owner = owner;
                    m_enumerator = mask.GetEnumerator();
                }

                public void Dispose()
                {
                    m_enumerator.Dispose();
                }

                public bool MoveNext()
                {
                    return m_enumerator.MoveNext();
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly SystemCollection m_owner;

            public SystemsWithInterface(SystemCollection owner)
            {
                m_owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner);
            }

            IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private Level m_level;
        private ComponentCollection m_components;
        private BitField m_systemsMask;
        private SystemBase[] m_systems;

        public Level Level
        {
            get
            {
                return m_level;
            }
        }

        public BitField Mask
        {
            get
            {
                return m_systemsMask;
            }
        }

        public SystemCollection(Level level, ComponentCollection components)
        {
            App.Assert(components.Level == level);
            m_level = level;
            m_components = components;
            m_systemsMask = BitField.Empty;
            m_systems = new SystemBase[ComponentRegistry.SystemCount];
        }

        public void Add(int id, SystemBase system)
        {
            App.Assert(id >= 0 && id < ComponentRegistry.SystemCount);
            App.Assert(ComponentRegistry.GetSystemID(system) == id);

            // Check for duplicates
            if (m_systemsMask[id])
            {
                throw new Exception("Level already contains system " + ComponentRegistry.GetSystemName(id));
            }

            // Check system requirements
            var systemRequirements = ComponentRegistry.GetSystemsRequiredBySystem(id);
            var missingSystems = (~m_systemsMask & systemRequirements);
            if (!missingSystems.IsEmpty)
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append("Level is missing systems");
                foreach (var requiredID in missingSystems)
                {
                    errorMessage.Append(" " + ComponentRegistry.GetSystemName(requiredID));
                }
                errorMessage.Append(" required by system " + ComponentRegistry.GetSystemName(id));
                throw new Exception(errorMessage.ToString());
            }

            // Add the system
            App.Assert(m_systemsMask[id] == false);
            App.Assert(m_systems[id] == null);
            m_systemsMask[id] = true;
            m_systems[id] = system;
        }

        public void Remove(int id)
        {
            App.Assert(id >= 0 && id < ComponentRegistry.SystemCount);

            if (!m_systemsMask[id])
            {
                // We don't have the system, nothing to do
                return;
            }

            // Check that no systems depend on the system being removed
            foreach (var otherSystemID in m_systemsMask)
            {
                var requirements = ComponentRegistry.GetSystemsRequiredBySystem(otherSystemID);
                if (requirements[id])
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.Append("Attempt to remove system " + ComponentRegistry.GetSystemName(id) + " required by systems");
                    foreach (var id2 in m_systemsMask)
                    {
                        requirements = ComponentRegistry.GetRequiredSystems(id2);
                        if (requirements[id])
                        {
                            errorMessage.Append(" " + ComponentRegistry.GetSystemName(id2));
                        }
                    }
                    throw new Exception(errorMessage.ToString());
                }
            }

            // Check that no components depend on the system being removed
            for (int componentID = 0; componentID < ComponentRegistry.ComponentCount; ++componentID)
            {
                var requirements = ComponentRegistry.GetRequiredSystems(id);
                if (requirements[id] && m_components.GetComponentCount(id) > 0)
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.Append("Attempt to remove system " + ComponentRegistry.GetSystemName(id) + " required by components");
                    for (int id2 = id; id2 < ComponentRegistry.ComponentCount; ++id2)
                    {
                        requirements = ComponentRegistry.GetRequiredSystems(id2);
                        if (requirements[id] && m_components.GetComponentCount(id2) > 0)
                        {
                            errorMessage.Append(" " + ComponentRegistry.GetComponentName(id2));
                        }
                    }
                    throw new Exception(errorMessage.ToString());
                }
            }

            // Remove the system
            App.Assert(m_systems[id] != null);
            App.Assert(m_systemsMask[id] == true);
            var system = m_systems[id];
            system.Shutdown();
            m_systemsMask[id] = false;
            m_systems[id] = null;
        }

        public void Clear()
        {
            // Check that no existant components depend on any systems
            if (m_components.GetComponentCount() > 0)
            {
                for (int componentID = 0; componentID < ComponentRegistry.ComponentCount; ++componentID)
                {
                    if (m_components.GetComponentCount(componentID) > 0)
                    {
                        var requirements = ComponentRegistry.GetRequiredSystems(componentID);
                        if (!requirements.IsEmpty)
                        {
                            var errorMessage = new StringBuilder();
                            errorMessage.Append("Attempt to remove systems required by " + ComponentRegistry.GetComponentName(componentID) + " component:");
                            foreach (int systemID in requirements)
                            {
                                errorMessage.Append(" " + ComponentRegistry.GetSystemName(systemID));
                            }
                            throw new Exception(errorMessage.ToString());
                        }
                    }
                }
            }

            // Remove all the systems in reverse order
            for (int id = ComponentRegistry.SystemCount - 1; id >= 0; --id)
            {
                var system = m_systems[id];
                if (system != null)
                {
                    system.Shutdown();
                    m_systemsMask[id] = false;
                    m_systems[id] = null;
                }
            }
            App.Assert(m_systemsMask.IsEmpty);
        }

        public SystemBase Get(int id)
        {
            App.Assert(id >= 0 && id < ComponentRegistry.SystemCount);
            return m_systems[id];
        }
        
        public Systems GetSystems()
        {
            return new Systems(this);
        }

        public SystemsWithInterface<TInterface> GetSystemsWithInterface<TInterface>() where TInterface : class, IInterface
        {
            return new SystemsWithInterface<TInterface>(this);
        }
    }
}
