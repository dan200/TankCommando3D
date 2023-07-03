using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Util;
using Dan200.Core.Main;

namespace Dan200.Core.Level
{
    internal class ComponentCollection
    {
        internal struct Components : IEnumerable<ComponentBase>
        {
            internal struct Enumerator : IEnumerator<ComponentBase>
            {
                private readonly ComponentCollection m_owner;
                private int m_id;
                private Dictionary<int, ComponentBase>.Enumerator m_componentEnumerator;
                private ComponentBase m_current;
                private bool m_singleID;

                public ComponentBase Current
                {
                    get
                    {
                        App.Assert(m_current != null && !m_current.Dead);
                        return m_current;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(ComponentCollection owner, int singleID)
                {
                    m_owner = owner;
                    if (singleID >= 0)
                    {
                        m_id = singleID;
                        m_componentEnumerator = owner.m_components[singleID].GetEnumerator();
                        m_singleID = true;
                    }
                    else
                    {
                        m_id = -1;
                        m_componentEnumerator = default(Dictionary<int, ComponentBase>.Enumerator);
                        m_singleID = false;
                    }
                    m_current = null;
                }

                public void Dispose()
                {
                    if (m_id >= 0)
                    {
                        m_componentEnumerator.Dispose();
                    }
                }

                public bool MoveNext()
                {
                    while(MoveNextImpl())
                    {
                        if(!m_current.Dead)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                private bool MoveNextImpl()
                {
                    if (m_id >= 0 && m_componentEnumerator.MoveNext())
                    {
                        m_current = m_componentEnumerator.Current.Value;
                        return true;
                    }
                    else
                    {
                        if(!m_singleID)
                        {
                            while (++m_id < m_owner.m_components.Length)
                            {
                                m_componentEnumerator = m_owner.m_components[m_id].GetEnumerator();
                                if (m_componentEnumerator.MoveNext())
                                {
                                    m_current = m_componentEnumerator.Current.Value;
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly ComponentCollection m_owner;
            private readonly int m_singleID;

            public Components(ComponentCollection owner, int singleID)
            {
                m_owner = owner;
                m_singleID = singleID;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner, m_singleID);
            }

            IEnumerator<ComponentBase> IEnumerable<ComponentBase>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct Components<TComponent> : IEnumerable<TComponent> where TComponent : ComponentBase
        {
            internal struct Enumerator : IEnumerator<TComponent>
            {
                private Dictionary<int, ComponentBase>.Enumerator m_componentEnumerator;
                private ComponentBase m_current;

                public TComponent Current
                {
                    get
                    {
                        App.Assert(m_current is TComponent && !m_current.Dead);
                        return m_current as TComponent;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(ComponentCollection owner)
                {
                    var id = ComponentRegistry.GetComponentID<TComponent>();
                    m_componentEnumerator = owner.m_components[id].GetEnumerator();
                    m_current = null;
                }

                public void Dispose()
                {
                    m_componentEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    while( m_componentEnumerator.MoveNext() )
                    {
                        var component = m_componentEnumerator.Current.Value;
                        if(!component.Dead)
                        {
                            App.Assert(component is TComponent);
                            m_current = component;
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly ComponentCollection m_owner;

            public Components(ComponentCollection owner)
            {
                m_owner = owner;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner);
            }

            IEnumerator<TComponent> IEnumerable<TComponent>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct ComponentsOnEntity : IEnumerable<ComponentBase>
        {
            internal struct Enumerator : IEnumerator<ComponentBase>
            {
                private readonly ComponentCollection m_owner;
                private readonly int m_entityID;
                private BitField.BitEnumerator m_idEnumerator;
                private ComponentBase m_current;

                public ComponentBase Current
                {
                    get
                    {
                        App.Assert(m_current != null && !m_current.Dead);
                        return m_current;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(ComponentCollection owner, Entity entity)
                {
                    m_owner = owner;
                    m_entityID = entity.ID;
                    m_idEnumerator = entity.ComponentsMask.GetEnumerator();
                    m_current = null;
                }

                public void Dispose()
                {
                    m_idEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    while(m_idEnumerator.MoveNext())
                    {
                        if(m_owner.m_components[m_idEnumerator.Current].TryGetValue(m_entityID, out m_current) && !m_current.Dead)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly ComponentCollection m_owner;
            private readonly Entity m_entity;

            public ComponentsOnEntity(ComponentCollection owner, Entity entity)
            {
                m_owner = owner;
                m_entity = entity;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner, m_entity);
            }

            IEnumerator<ComponentBase> IEnumerable<ComponentBase>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct ComponentsWithInterface<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
        {
            internal struct Enumerator : IEnumerator<TInterface>
            {
                private readonly ComponentCollection m_owner;
                private BitField.BitEnumerator m_idEnumerator;
                private Dictionary<int, ComponentBase>.Enumerator m_componentEnumerator;
                private ComponentBase m_current;
                private bool m_started;

                public TInterface Current
                {
                    get
                    {
                        App.Assert(m_current is TInterface && !m_current.Dead);
                        return m_current as TInterface;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(ComponentCollection owner)
                {
                    m_owner = owner;
                    m_idEnumerator = ComponentRegistry.GetComponentsImplementingInterface<TInterface>().GetEnumerator();
                    m_componentEnumerator = default(Dictionary<int, ComponentBase>.Enumerator);
                    m_started = false;
                    m_current = null;
                }

                public bool MoveNext()
                {
                    while(MoveNextImpl())
                    {
                        if(!m_current.Dead)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                private bool MoveNextImpl()
                {
                    if (m_started && m_componentEnumerator.MoveNext())
                    {
                        m_current = m_componentEnumerator.Current.Value;
                        return true;
                    }
                    else
                    {
                        m_started = true;
                        while (m_idEnumerator.MoveNext())
                        {
                            m_componentEnumerator = m_owner.m_components[m_idEnumerator.Current].GetEnumerator();
                            if (m_componentEnumerator.MoveNext())
                            {
                                m_current = m_componentEnumerator.Current.Value;
                                return true;
                            }
                        }
                        return false;
                    }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                    m_idEnumerator.Dispose();
                    if (m_started)
                    {
                        m_componentEnumerator.Dispose();
                    }
                }
            }

            private readonly ComponentCollection m_owner;

            public ComponentsWithInterface(ComponentCollection owner)
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

        internal struct ComponentsWithInterfaceOnEntity<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
        {
            internal struct Enumerator : IEnumerator<TInterface>
            {
                private readonly ComponentCollection m_owner;
                private readonly int m_entityID;
                private BitField.BitEnumerator m_idEnumerator;
                private ComponentBase m_current;

                public TInterface Current
                {
                    get
                    {
                        App.Assert(m_current is TInterface && !m_current.Dead);
                        return m_current as TInterface;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                internal Enumerator(ComponentCollection owner, Entity entity)
                {
                    var mask = ComponentRegistry.GetComponentsImplementingInterface<TInterface>() & entity.ComponentsMask;
                    m_owner = owner;
                    m_entityID = entity.ID;
                    m_idEnumerator = mask.GetEnumerator();
                    m_current = null;
                }

                public void Dispose()
                {
                    m_idEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    ComponentBase component;
                    while (m_idEnumerator.MoveNext())
                    {
                        if (m_owner.m_components[m_idEnumerator.Current].TryGetValue(m_entityID, out component) && !component.Dead)
                        {
                            App.Assert(component is TInterface);
                            m_current = component;
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly ComponentCollection m_owner;
            private readonly Entity m_entity;

            public ComponentsWithInterfaceOnEntity(ComponentCollection owner, Entity entity)
            {
                m_owner = owner;
                m_entity = entity;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_owner, m_entity);
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

        private readonly Level m_level;
        private readonly Dictionary<int, ComponentBase>[] m_components;
        private readonly Dictionary<int, ComponentBase>[] m_newComponents;
        private int m_numNewComponents;
        private int m_numDeadComponents;

        public Level Level
        {
            get
            {
                return m_level;
            }
        }

        public ComponentCollection(Level level)
        {
            m_level = level;
            m_components = new Dictionary<int, ComponentBase>[ComponentRegistry.ComponentCount];
            m_newComponents = new Dictionary<int, ComponentBase>[ComponentRegistry.ComponentCount];
            for (int i = 0; i < m_components.Length; ++i)
            {
                m_components[i] = new Dictionary<int, ComponentBase>();
                m_newComponents[i] = new Dictionary<int, ComponentBase>();
            }
            m_numDeadComponents = 0;
            m_numNewComponents = 0;
        }

        public void Add(Entity entity, ComponentBase component)
        {
            var id = ComponentRegistry.GetComponentID(component);
            App.Assert(!entity.Dead);
            App.Assert(!component.Dead);
            App.Assert(entity.ComponentsMask[id]);
            App.Assert(!m_components[id].ContainsKey(entity.ID) && !m_newComponents[id].ContainsKey(entity.ID));
            m_newComponents[id].Add(entity.ID, component);
            m_numNewComponents++;
        }

        public void Remove(Entity entity, int id)
        {
            App.Assert(id >= 0 && id < ComponentRegistry.ComponentCount);
            App.Assert(!entity.ComponentsMask[id]);
            App.Assert(m_components[id].ContainsKey(entity.ID) || m_newComponents[id].ContainsKey(entity.ID));
            if (m_newComponents[id].Remove(entity.ID))
            {
                App.Assert(!m_components[id].ContainsKey(entity.ID));
                m_numNewComponents--;
            }
            else
            {
                App.Assert(m_components[id].ContainsKey(entity.ID));
                App.Assert(m_components[id][entity.ID].Dead);
                m_numDeadComponents++;
            }
        }

        public void RemoveDeadComponents()
        {
            // Remove all components which are dead (shutdown). Done at once to avoid enumerator invalidation
            if (m_numDeadComponents > 0)
            {
                var deadEntityList = new List<int>();
                for (int id = 0; id < ComponentRegistry.ComponentCount; ++id)
                {
                    var components = m_components[id];
                    foreach (var pair in components)
                    {
                        var entityID = pair.Key;
                        var component = pair.Value;
                        if (component.Dead)
                        {
                            deadEntityList.Add(entityID);
                        }
                    }
                    if (deadEntityList.Count > 0)
                    {
                        foreach (var entityID in deadEntityList)
                        {
                            components.Remove(entityID);
                        }
                        deadEntityList.Clear();
                    }
                }
                m_numDeadComponents = 0;
            }
        }

        public void PromoteNewComponents()
        {
            // Move all components from the "new" list to the "live" list.
            // This ensures things added mid-frame don't get half-advanced, rendered without advance, etc
            if (m_numNewComponents > 0)
            {
                for (int id = 0; id < ComponentRegistry.ComponentCount; ++id)
                {
                    var newComponents = m_newComponents[id];
                    if (newComponents.Count > 0)
                    {
                        var components = m_components[id];
                        foreach (var pair in newComponents)
                        {
                            var entityID = pair.Key;
                            var component = pair.Value;
                            App.Assert(!component.Dead);
                            App.Assert(!components.ContainsKey(entityID));
                            components.Add(entityID, component);
                        }
                        newComponents.Clear();
                    }
                }
                m_numNewComponents = 0;
            }
        }

        public ComponentBase Get(Entity entity, int id)
        {
            App.Assert(entity.Level == Level);
            App.Assert(entity.ComponentsMask[id]);
            return Get(entity.ID, id);
        }

        public ComponentBase Get(int entityID, int componentID)
        {
            App.Assert(componentID >= 0 && componentID < ComponentRegistry.ComponentCount);
            App.Assert(m_components[componentID].ContainsKey(entityID) || m_newComponents[componentID].ContainsKey(entityID));

            ComponentBase result;
            if(!m_components[componentID].TryGetValue(entityID, out result))
            {
                App.Assert(m_newComponents[componentID].ContainsKey(entityID));
                result = m_newComponents[componentID][entityID];
            }
            return result;
        }

        public Components GetComponents()
        {
            return new Components(this, -1);
        }

        public Components GetComponents(int componentID)
        {
            App.Assert(componentID >= 0 && componentID < m_components.Length);
            return new Components(this, componentID);
        }

        public IEnumerable<ComponentBase> GetNonLiveComponents(int componentID)
        {
            App.Assert(componentID >= 0 && componentID < m_components.Length);
            return m_components[componentID].Values;
        }

        public Components<TComponent> GetComponents<TComponent>() where TComponent : ComponentBase
        {
            return new Components<TComponent>(this);
        }

        public ComponentsWithInterface<TInterface> GetComponentsWithInterface<TInterface>() where TInterface : class, IInterface
        {
            return new ComponentsWithInterface<TInterface>(this);
        }

        public ComponentsOnEntity GetComponents(Entity entity)
        {
            return new ComponentsOnEntity(this, entity);
        }

        public ComponentsWithInterfaceOnEntity<TInterface> GetComponentsWithInterface<TInterface>(Entity entity) where TInterface : class, IInterface
        {
            return new ComponentsWithInterfaceOnEntity<TInterface>(this, entity);
        }

        public int GetComponentCount()
        {
            var count = 0;
            for(int id=0; id<m_components.Length; ++id)
            {
                count += m_components[id].Count;
            }
            return count + m_numNewComponents - m_numDeadComponents;
        }

        public int GetComponentCount(int componentID)
        {
            App.Assert(componentID >= 0 && componentID < m_components.Length);
            return m_components[componentID].Count + m_newComponents[componentID].Count;
        }
    }
}
