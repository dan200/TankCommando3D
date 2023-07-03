
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;
using Dan200.Core.Interfaces.Core;

namespace Dan200.Core.Components.Core
{
    internal struct HierarchyComponentData
    {
        [Optional(Default = 0)]
        public int Parent;
    }

    internal class HierarchyComponent : EditableComponent<HierarchyComponentData>, IComponentListener, IDebugDraw
	{
		public static Entity GetParent(Entity e)
		{
			var hierarchy = e.GetComponent<HierarchyComponent>();
			if (hierarchy != null)
			{
				return hierarchy.Parent;
			}
			return null;
		}
		
		internal struct ChildCollection : IEnumerable<Entity>
		{
			internal struct Enumerator : IEnumerator<Entity>
			{
                private HierarchyComponent m_root;
                private HierarchyComponent m_current;

				public Entity Current
				{
					get
					{
                        return m_current.Entity;
					}
				}

				object IEnumerator.Current
				{
					get
					{
						return Current;
					}
				}

				internal Enumerator(HierarchyComponent owner)
				{
                    App.Assert(owner != null);
                    m_root = owner;
                    m_current = null;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
                    if(m_current == null)
                    {
                        m_current = m_root.m_firstChild;
                    }
                    else
                    {
                        m_current = m_current.m_nextSibling;
                    }
                    return m_current != null;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;

			public ChildCollection(HierarchyComponent owner)
			{
				m_owner = owner;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner);
			}

			IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		internal struct ChildrenWithComponent<TComponent> : IEnumerable<TComponent> where TComponent : ComponentBase
		{
			internal struct Enumerator : IEnumerator<TComponent>
			{
				private ChildCollection.Enumerator m_enumerator;
				private TComponent m_current;

				public TComponent Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner)
				{
					m_enumerator = new ChildCollection.Enumerator(owner);
					m_current = null;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					while (m_enumerator.MoveNext())
					{
						m_current = m_enumerator.Current.GetComponent<TComponent>();
						if (m_current != null)
						{
							return true;
						}
					}
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;

			public ChildrenWithComponent(HierarchyComponent owner)
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

		internal struct ChildrenWithInterface<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
		{
			internal struct Enumerator : IEnumerator<TInterface>
			{
				private HierarchyComponent m_owner;
				private BitField.BitEnumerator m_componentEnumerator;
				private ChildCollection.Enumerator m_childEntityEnumerator;
				private TInterface m_current;

				public TInterface Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner)
				{
					var componentsToFind = ComponentRegistry.GetComponentsImplementingInterface<TInterface>();
					if (!componentsToFind.IsEmpty)
					{
						var allComponents = BitField.Empty;
						foreach (var entity in owner.Children)
						{
							allComponents |= entity.ComponentsMask;
						}
						componentsToFind &= allComponents;
					}
					m_owner = owner;
					m_componentEnumerator = componentsToFind.GetEnumerator();
					m_childEntityEnumerator = default(ChildCollection.Enumerator);
					m_current = null;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Begin
					if (m_current == null)
					{
						if (m_componentEnumerator.MoveNext())
						{
							m_childEntityEnumerator = new ChildCollection.Enumerator(m_owner);
						}
						else
						{
							return false;
						}
					}

					while(true)
					{
						// Find the next component with the interface
						var componentID = m_componentEnumerator.Current;
						while (m_childEntityEnumerator.MoveNext())
						{
							m_current = m_childEntityEnumerator.Current.GetComponent(componentID) as TInterface;
							if (m_current != null)
							{
								return true;
							}
						}

						// Move onto the next component type
						if (m_componentEnumerator.MoveNext())
						{
							m_childEntityEnumerator = new ChildCollection.Enumerator(m_owner);
						}
						else
						{
							break;
						}
					}

					// Reached the end
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;

			public ChildrenWithInterface(HierarchyComponent owner)
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

		internal struct DescendantCollection : IEnumerable<Entity>
		{
			internal struct Enumerator : IEnumerator<Entity>
			{
				private HierarchyComponent m_root;
				private HierarchyComponent m_current;

				public Entity Current
				{
					get
					{
						return m_current.Entity;
					}
				}

				object IEnumerator.Current
				{
					get
					{
						return Current;
					}
				}

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
                    App.Assert(owner != null);
					m_root = owner;
					m_current = includeSelf ? null : owner;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Begin with the root
					if (m_current == null)
					{
						m_current = m_root;
						return true;
					}

                    // Then descend into the children
                    if(m_current.m_firstChild != null)
                    {
                        m_current = m_current.m_firstChild;
                        return true;
                    }

                    // Then visit siblings, or parent's siblings
                    while(m_current != m_root)
                    {
                        if(m_current.m_nextSibling != null)
                        {
                            m_current = m_current.m_nextSibling;
                            return true;
                        }
                        m_current = m_current.m_parent;
                    }

					// Reached the end
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public DescendantCollection(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
			}

			IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		internal struct DescendantsWithComponent<TComponent> : IEnumerable<TComponent> where TComponent : ComponentBase
		{
			internal struct Enumerator : IEnumerator<TComponent>
			{
				private DescendantCollection.Enumerator m_enumerator;
				private TComponent m_current;

				public TComponent Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
					m_enumerator = new DescendantCollection.Enumerator(owner, includeSelf);
					m_current = null;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					while (m_enumerator.MoveNext())
					{
						m_current = m_enumerator.Current.GetComponent<TComponent>();
						if (m_current != null)
						{
							return true;
						}
					}
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public DescendantsWithComponent(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
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

		internal struct DescendantsWithInterface<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
		{
			internal struct Enumerator : IEnumerator<TInterface>
			{
				private HierarchyComponent m_owner;
				private BitField.BitEnumerator m_componentEnumerator;
				private DescendantCollection.Enumerator m_childEntityEnumerator;
				private TInterface m_current;
				private bool m_includeSelf;

				public TInterface Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
					var componentsToFind = ComponentRegistry.GetComponentsImplementingInterface<TInterface>();
					if (!componentsToFind.IsEmpty)
					{
						var allComponents = BitField.Empty;
						foreach (var entity in new DescendantCollection(owner, includeSelf))
						{
							allComponents |= entity.ComponentsMask;
						}
						componentsToFind &= allComponents;
					}
					m_owner = owner;
					m_componentEnumerator = componentsToFind.GetEnumerator();
					m_childEntityEnumerator = default(DescendantCollection.Enumerator);
					m_current = null;
					m_includeSelf = includeSelf;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Begin
					if (m_current == null)
					{
						if (m_componentEnumerator.MoveNext())
						{
							m_childEntityEnumerator = new DescendantCollection.Enumerator(m_owner, m_includeSelf);
						}
						else
						{
							return false;
						}
					}

					while (true)
					{
						// Find the next component with the interface
						var componentID = m_componentEnumerator.Current;
						while (m_childEntityEnumerator.MoveNext())
						{
							m_current = m_childEntityEnumerator.Current.GetComponent(componentID) as TInterface;
							if (m_current != null)
							{
								return true;
							}
						}

						// Move onto the next component type
						if (m_componentEnumerator.MoveNext())
						{
							m_childEntityEnumerator = new DescendantCollection.Enumerator(m_owner, m_includeSelf);
						}
						else
						{
							break;
						}
					}

					// Reached the end
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public DescendantsWithInterface(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
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

		internal struct AncestorCollection : IEnumerable<Entity>
		{
			internal struct Enumerator : IEnumerator<Entity>
			{
				private Entity m_current;
				private HierarchyComponent m_next;

				public Entity Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
					m_current = null;
					m_next = includeSelf ? owner : owner.m_parent;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Handle first iteration
					if (m_next != null)
					{
						m_current = m_next.Entity;
						m_next = m_next.m_parent;
						return true;
					}
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public AncestorCollection(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
			}

			IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
			{
				return GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		internal struct AncestorsWithComponent<TComponent> : IEnumerable<TComponent> where TComponent : ComponentBase
		{
			internal struct Enumerator : IEnumerator<TComponent>
			{
				private AncestorCollection.Enumerator m_enumerator;
				private TComponent m_current;

				public TComponent Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
					m_enumerator = new AncestorCollection.Enumerator(owner, includeSelf);
					m_current = null;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					while (m_enumerator.MoveNext())
					{
						m_current = m_enumerator.Current.GetComponent<TComponent>();
						if (m_current != null)
						{
							return true;
						}
					}
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public AncestorsWithComponent(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
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

		internal struct AncestorsWithInterface<TInterface> : IEnumerable<TInterface> where TInterface : class, IInterface
		{
			internal struct Enumerator : IEnumerator<TInterface>
			{
				private HierarchyComponent m_owner;
				private BitField.BitEnumerator m_componentEnumerator;
				private AncestorCollection.Enumerator m_ancestorEntityEnumerator;
				private TInterface m_current;
				private bool m_includeSelf;

				public TInterface Current
				{
					get
					{
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

				internal Enumerator(HierarchyComponent owner, bool includeSelf)
				{
					var componentsToFind = ComponentRegistry.GetComponentsImplementingInterface<TInterface>();
					if (!componentsToFind.IsEmpty)
					{
						var allComponents = BitField.Empty;
						foreach (var entity in new AncestorCollection(owner, includeSelf))
						{
							allComponents |= entity.ComponentsMask;
						}
						componentsToFind &= allComponents;
					}
					m_owner = owner;
					m_componentEnumerator = componentsToFind.GetEnumerator();
					m_ancestorEntityEnumerator = default(AncestorCollection.Enumerator);
					m_current = null;
					m_includeSelf = includeSelf;
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Begin
					if (m_current == null)
					{
						if (m_componentEnumerator.MoveNext())
						{
							m_ancestorEntityEnumerator = new AncestorCollection.Enumerator(m_owner, m_includeSelf);
						}
						else
						{
							return false;
						}
					}

					while (true)
					{
						// Find the next component with the interface
						var componentID = m_componentEnumerator.Current;
						while (m_ancestorEntityEnumerator.MoveNext())
						{
							m_current = m_ancestorEntityEnumerator.Current.GetComponent(componentID) as TInterface;
							if (m_current != null)
							{
								return true;
							}
						}

						// Move onto the next component type
						if (m_componentEnumerator.MoveNext())
						{
							m_ancestorEntityEnumerator = new AncestorCollection.Enumerator(m_owner, m_includeSelf);
						}
						else
						{
							break;
						}
					}

					// Reached the end
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;
			private bool m_includeSelf;

			public AncestorsWithInterface(HierarchyComponent owner, bool includeSelf)
			{
				m_owner = owner;
				m_includeSelf = includeSelf;
			}

			public Enumerator GetEnumerator()
			{
				return new Enumerator(m_owner, m_includeSelf);
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

		private HierarchyComponent m_parent;
        private HierarchyComponent m_firstChild;
        private HierarchyComponent m_nextSibling;
        private HierarchyComponent m_previousSibling;

        public Entity Parent
		{
			get
			{
				return (m_parent != null) ? m_parent.Entity : null;
			}
			set
			{
				// Get the parent hierarchy component
				HierarchyComponent parent;
				if (value != null)
				{
					parent = value.GetComponent<HierarchyComponent>();
					App.Assert(parent != null, "Cannot parent to an entity without a hierarchy component");
				}
				else
				{
					parent = null;
				}

                var oldParent = m_parent;
                if (oldParent != parent)
				{
                    // Change the parent
                    if (oldParent != null)
                    {
                        DetachFromParent();
                    }
                    if (parent != null)
                    {
                        AttachToParent(parent);
                    }

                    // Notify interested parties:
					// Self
					foreach (var listener in Entity.GetComponentsWithInterface<IHierarchyListener>())
					{
						listener.OnParentChanged(
							(oldParent != null) ? oldParent.Entity : null,
							(parent != null) ? parent.Entity : null
						);
					}

					// Descendants
					foreach (var listener in GetDescendantsWithInterface<IAncestryListener>(true))
					{
						listener.OnAncestryChanged();
					}
				}
			}
		}

        public bool HasChildren
        {
            get
            {
                return m_firstChild != null;
            }
        }

        public ChildCollection Children
		{
			get
			{
				return new ChildCollection(this);
			}
		}

		public DescendantCollection Descendants
		{
			get
			{
				return new DescendantCollection(this, false);
			}
		}

		public AncestorCollection Ancestors
		{
			get
			{
				return new AncestorCollection(this, false);
			}
		}

        public BitField ComponentsOnAncestorsMask
        {
            get
            {
                var result = new BitField();
                var parent = m_parent;
                while(parent != null)
                {
                    result |= parent.Entity.ComponentsMask;
                    parent = parent.m_parent;
                }
                return result;
            }
        }

        private void AttachToParent(HierarchyComponent parent)
        {
            App.Assert(parent != null);
            App.Assert(m_parent == null);
            App.Assert(m_nextSibling == null);
            App.Assert(m_previousSibling == null);

            if(parent.m_firstChild != null)
            {
                m_nextSibling = parent.m_firstChild;
                m_nextSibling.m_previousSibling = this;
            }
            m_parent = parent;
            m_parent.m_firstChild = this;
        }

        private void DetachFromParent()
        {
            App.Assert(m_parent != null);

            if(m_nextSibling != null)
            {
                m_nextSibling.m_previousSibling = m_previousSibling;
            }
            if(m_previousSibling != null)
            {
                m_previousSibling.m_nextSibling = m_nextSibling;
            }
            if(m_parent.m_firstChild == this)
            {
                m_parent.m_firstChild = m_nextSibling;
            }
            m_parent = null;
            m_nextSibling = null;
            m_previousSibling = null;
        }

        private void DetachChildren()
        {
            App.Assert(m_firstChild != null);

            var child = m_firstChild;
            do
            {
                var nextChild = child.m_nextSibling;
                child.m_parent = null;
                child.m_nextSibling = null;
                child.m_previousSibling = null;
                child = nextChild;
            }
            while (child != null);
            m_firstChild = null;
        }

        protected override void OnInit(in HierarchyComponentData properties)
		{
            if (properties.Parent != 0)
			{
				// Get the parent
                var parentID = properties.Parent;
				var parent = Level.Entities.Lookup(parentID);
				App.Assert(parent != null && parent != Entity);

				var parentHierarchy = parent.GetComponent<HierarchyComponent>();
				App.Assert(parentHierarchy != null);

                // Attach to the parent
                AttachToParent(parentHierarchy);

				// Notify all interested parties:
				// Self
				foreach (var listener in Entity.GetComponentsWithInterface<IHierarchyListener>())
				{
					listener.OnParentChanged(null, parent);
				}

				// Descendants
				foreach (var listener in GetDescendantsWithInterface<IAncestryListener>(true))
				{
					listener.OnAncestryChanged();
				}
			}
		}

        protected override void Reset(in HierarchyComponentData properties)
        {
            var parentID = properties.Parent;
            if (parentID != 0)
            {
                var parent = Level.Entities.Lookup(parentID);
                App.Assert(parent != null && parent != Entity);
                Parent = parent;
            }
            else
            {
                Parent = null;
            }
        }

        protected override void OnShutdown()
		{
			// Detach from the parent
			var oldParent = m_parent;
			if (oldParent != null)
			{
                DetachFromParent();
			}

			// Get information about descendants
			List<IHierarchyListener> oldChildrenHierarchyListeners = null;
			List<IAncestryListener> oldDescendantAncestorListeners = null;

			// Get descendant listeners (includes self)
			var enumerator = GetDescendantsWithInterface<IAncestryListener>(true).GetEnumerator();
			if (enumerator.MoveNext())
			{
				oldDescendantAncestorListeners = new List<IAncestryListener>();
				do
				{
					oldDescendantAncestorListeners.Add(enumerator.Current);
				}
				while (enumerator.MoveNext());
			}

			if (HasChildren)
			{
				// Get child listeners
				var enumerator2 = GetChildrenWithInterface<IHierarchyListener>().GetEnumerator();
				if (enumerator2.MoveNext())
				{
					oldChildrenHierarchyListeners = new List<IHierarchyListener>();
					do
					{
						oldChildrenHierarchyListeners.Add(enumerator2.Current);
					}
					while (enumerator2.MoveNext());
				}

                // Detach the children
                DetachChildren();
			}

			// Notify all interested parties
			// Self
			if (oldParent != null)
			{
				foreach (var listener in Entity.GetComponentsWithInterface<IHierarchyListener>())
				{
					listener.OnParentChanged(oldParent.Entity, null);
				}
			}

			// Old children
			if (oldChildrenHierarchyListeners != null)
			{
				foreach (var listener in oldChildrenHierarchyListeners)
				{
					listener.OnParentChanged(Entity, null);
				}
			}

            // Old descendants
			if (oldDescendantAncestorListeners != null)
			{
				foreach (var listener in oldDescendantAncestorListeners)
				{
					listener.OnAncestryChanged();
				}
			}

			// Check that nothing else was re-attached to us
			App.Assert(m_parent == null);
			App.Assert(m_firstChild == null);
            App.Assert(m_nextSibling == null);
            App.Assert(m_previousSibling == null);
		}

		public TComponent GetAncestorWithComponent<TComponent>(bool includeSelf = false) where TComponent : ComponentBase
		{
			var hierarchy = includeSelf ? this : m_parent;
			while (hierarchy != null)
			{
				var component = hierarchy.Entity.GetComponent<TComponent>();
				if (component != null)
				{
					return component;
				}
				hierarchy = hierarchy.m_parent;
			}
			return null;
		}

		public ChildrenWithComponent<TComponent> GetChildrenWithComponent<TComponent>() where TComponent : ComponentBase
		{
			return new ChildrenWithComponent<TComponent>(this);
		}

		public ChildrenWithInterface<TInterface> GetChildrenWithInterface<TInterface>() where TInterface : class, IInterface
		{
			return new ChildrenWithInterface<TInterface>(this);
		}

		public DescendantsWithComponent<TComponent> GetDescendantsWithComponent<TComponent>(bool includeSelf = false) where TComponent : ComponentBase
		{
			return new DescendantsWithComponent<TComponent>(this, includeSelf);
		}

		public DescendantsWithInterface<TInterface> GetDescendantsWithInterface<TInterface>(bool includeSelf = false) where TInterface : class, IInterface
		{
			return new DescendantsWithInterface<TInterface>(this, includeSelf);
		}

		public AncestorsWithComponent<TComponent> GetAncestorsWithComponent<TComponent>(bool includeSelf = false) where TComponent : ComponentBase
		{
			return new AncestorsWithComponent<TComponent>(this, includeSelf);
		}

		public AncestorsWithInterface<TInterface> GetAncestorsWithInterface<TInterface>(bool includeSelf = false) where TInterface : class, IInterface
		{
			return new AncestorsWithInterface<TInterface>(this, includeSelf);
		}

		public void OnComponentAdded(ComponentBase component)
		{
			var entity = Entity;
			foreach (var listener in GetDescendantsWithInterface<IAncestryListener>(false))
			{
				listener.OnComponentAdded(entity, component);
			}
		}

		public void OnComponentRemoved(ComponentBase component)
		{
			var entity = Entity;
			foreach (var listener in GetDescendantsWithInterface<IAncestryListener>(false))
			{
				listener.OnComponentRemoved(entity, component);
			}
		}

        public void DebugDraw()
        {
            if (m_parent != null)
            {
                var transform = Entity.GetComponent<TransformComponent>();
                var parentTransform = m_parent.Entity.GetComponent<TransformComponent>();
                if(transform != null && parentTransform != null)
                {
                    App.DebugDraw.DrawLine(transform.Position, parentTransform.Position, Colour.Magenta);
                }
            }
        }
    }
}
