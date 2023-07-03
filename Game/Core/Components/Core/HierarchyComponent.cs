
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
		
		internal struct ChildCollection : IReadOnlyCollection<Entity>
		{
			internal struct Enumerator : IEnumerator<Entity>
			{
				private List<HierarchyComponent>.Enumerator m_enumerator;

				public Entity Current
				{
					get
					{
						return m_enumerator.Current.Entity;
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
					m_enumerator = owner.m_children.GetEnumerator();
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					return m_enumerator.MoveNext();
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
			}

			private HierarchyComponent m_owner;

			public int Count
			{
				get
				{
					return m_owner.m_children.Count;
				}
			}

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
				private List<HierarchyComponent>.Enumerator m_enumerator;

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
					m_root = owner;
					m_current = includeSelf ? null : owner;
					m_enumerator = owner.m_children.GetEnumerator();
				}

				public void Dispose()
				{
				}

				public bool MoveNext()
				{
					// Handle first iterations
					if (m_current == null)
					{
						m_current = m_root;
						return true;
					}
					else if (m_current == m_root)
					{
						if (m_enumerator.MoveNext())
						{
							m_current = m_enumerator.Current;
							return true;
						}
						else
						{
							return false;
						}
					}

					// If there are children, descend into them
					if (m_current.m_children.Count > 0)
					{
						m_enumerator = m_current.m_children.GetEnumerator();
						m_enumerator.MoveNext();
						m_current = m_enumerator.Current;
						return true;
					}

					// Else, move to the next sibling
					if (m_enumerator.MoveNext())
					{
						m_current = m_enumerator.Current;
						return true;
					}

					// Else, move up the hierarchy
					var parent = m_current.m_parent;
					while (parent != m_root)
					{
						App.Assert(parent != null);
						App.Assert(parent.m_parent != null);
						App.Assert(parent.m_parent.m_children.Contains(parent));
						var Enumerator = parent.m_parent.m_children.GetEnumerator();
						while (Enumerator.MoveNext())
						{
							if (Enumerator.Current == parent)
							{
								m_enumerator = Enumerator;
								if (m_enumerator.MoveNext())
								{
									m_current = m_enumerator.Current;
									return true;
								}
								else
								{
									break;
								}
							}
						}
						parent = parent.m_parent;
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
		private List<HierarchyComponent> m_children;

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

				if (m_parent != parent)
				{
					// Get current ancestors before we break the link with the parent
					List<IDescendantListener> oldAncestorDescendantListeners = null;
					if(m_parent != null)
					{
						var Enumerator = GetAncestorsWithInterface<IDescendantListener>().GetEnumerator();
						if (Enumerator.MoveNext())
						{
							oldAncestorDescendantListeners = new List<IDescendantListener>();
							do
							{
								oldAncestorDescendantListeners.Add(Enumerator.Current);
							}
							while (Enumerator.MoveNext());
						}
					}

					// Change the parent
					if (m_parent != null)
					{
						m_parent.m_children.Remove(this);
					}
					var oldParent = m_parent;
					m_parent = parent;
					if (m_parent != null)
					{
						m_parent.m_children.Add(this);
					}

					// Old ancestors
					if (oldParent != null)
					{
						foreach (var oldParentListener in oldParent.Entity.GetComponentsWithInterface<IHierarchyListener>())
						{
							oldParentListener.OnChildRemoved(Entity);
						}
						if (oldAncestorDescendantListeners != null)
						{
							foreach (var oldParentListener in oldAncestorDescendantListeners)
							{
								oldParentListener.OnDescendantRemoved(Entity);
								foreach(var descendant in Descendants)
								{
									oldParentListener.OnDescendantRemoved(descendant);
								}
							}
						}
					}

					// New ancestors
					if (parent != null)
					{
						foreach (var newParentListener in parent.Entity.GetComponentsWithInterface<IHierarchyListener>())
						{
							newParentListener.OnChildAdded(Entity);
						}
						foreach (var newParentListener in GetAncestorsWithInterface<IDescendantListener>())
						{
							newParentListener.OnDescendantAdded(Entity);
							foreach(var descendant in Descendants)
							{
								newParentListener.OnDescendantAdded(descendant);
							}
						}
					}

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

		private void Init()
		{
			if (m_children == null)
			{
				m_parent = null;
				m_children = new List<HierarchyComponent>();
			}
		}

        protected override void OnInit(in HierarchyComponentData properties)
		{
			Init();
            if (properties.Parent != 0)
			{
				// Get the parent
                var parentID = properties.Parent;
				var parent = Level.Entities.Lookup(parentID);
				App.Assert(parent != null && parent != Entity);

				var parentHierarchy = parent.GetComponent<HierarchyComponent>();
				App.Assert(parentHierarchy != null);
				parentHierarchy.Init();

				// Attach to the parent
				m_parent = parentHierarchy;
				parentHierarchy.m_children.Add(this);

				// Notify all interested parties:
				// Ancestors
				foreach (var parentListener in parent.GetComponentsWithInterface<IHierarchyListener>())
				{
					parentListener.OnChildAdded(Entity);
				}
				foreach (var parentListener in GetAncestorsWithInterface<IDescendantListener>())
				{
					parentListener.OnDescendantAdded(Entity);
				}

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

        protected override void ReInit(in HierarchyComponentData properties)
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
			// Get informatiom about ancestors
			Entity oldParent = null;
			List<IDescendantListener> oldAncestorDescendantListeners = null;
			if (m_parent != null)
			{
				// Get the parent entity
				oldParent = m_parent.Entity;

				// Get ancestor listeners
				var Enumerator = GetAncestorsWithInterface<IDescendantListener>().GetEnumerator();
				if (Enumerator.MoveNext())
				{
					oldAncestorDescendantListeners = new List<IDescendantListener>();
					do
					{
						oldAncestorDescendantListeners.Add(Enumerator.Current);
					}
					while (Enumerator.MoveNext());
				}
				
				// Disconnect from the parent
				m_parent.m_children.Remove(this);
				m_parent = null;
			}

			// Get information about descendants
			List<Entity> oldChildren = null;
			List<Entity> oldDescendants = null;
			List<IHierarchyListener> oldChildrenHierarchyListeners = null;
			List<IAncestryListener> oldDescendantAncestorListeners = null;

			// Get descendant listeners (includes self)
			var Enumerator2 = GetDescendantsWithInterface<IAncestryListener>(true).GetEnumerator();
			if (Enumerator2.MoveNext())
			{
				oldDescendantAncestorListeners = new List<IAncestryListener>();
				do
				{
					oldDescendantAncestorListeners.Add(Enumerator2.Current);
				}
				while (Enumerator2.MoveNext());
			}

			if (m_children.Count > 0)
			{
				// Collect child listeners before we clear the children
				if ((Entity.ComponentsMask & ComponentRegistry.GetComponentsImplementingInterface<IHierarchyListener>()) != BitField.Empty)
				{
					oldChildren = new List<Entity>(Children);
				}
				if (oldAncestorDescendantListeners != null)
				{
					oldDescendants = new List<Entity>(Descendants);
				}
				var Enumerator = GetChildrenWithInterface<IHierarchyListener>().GetEnumerator();
				if (Enumerator.MoveNext())
				{
					oldChildrenHierarchyListeners = new List<IHierarchyListener>();
					do
					{
						oldChildrenHierarchyListeners.Add(Enumerator.Current);
					}
					while (Enumerator.MoveNext());
				}

				// Clear the children
				foreach (var child in m_children)
				{
					child.m_parent = null;
				}
				m_children.Clear();
			}

			// Notify all interested parties
			// Old ancestors
			if (oldParent != null)
			{
				foreach (var oldParentListener in oldParent.GetComponentsWithInterface<IHierarchyListener>())
				{
					oldParentListener.OnChildRemoved(Entity);
				}
				if (oldAncestorDescendantListeners != null)
				{
					foreach (var oldParentListener in oldAncestorDescendantListeners)
					{
						oldParentListener.OnDescendantRemoved(Entity);
						if (oldDescendants != null)
						{
							foreach(var oldDescendant in oldDescendants)
							{
								oldParentListener.OnDescendantRemoved(oldDescendant);
							}
						}
					}
				}
			}

			// Self
			if (oldParent != null)
			{
				foreach (var listener in Entity.GetComponentsWithInterface<IHierarchyListener>())
				{
					listener.OnParentChanged(oldParent, null);
				}
			}
			if (oldChildren != null)
			{
				foreach (var listener in Entity.GetComponentsWithInterface<IHierarchyListener>())
				{
					foreach (var child in oldChildren)
					{
						listener.OnChildRemoved(child);
					}
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
			if (oldDescendantAncestorListeners != null)
			{
				foreach (var listener in oldDescendantAncestorListeners)
				{
					listener.OnAncestryChanged();
				}
			}

			// Check nothing else was re-attached to us
			App.Assert(m_parent == null);
			App.Assert(m_children.Count == 0);
			m_children = null;
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
			foreach (var listener in GetAncestorsWithInterface<IDescendantListener>(false))
			{
				listener.OnComponentAdded(entity, component);
			}
			foreach (var listener in GetDescendantsWithInterface<IAncestryListener>(false))
			{
				listener.OnComponentAdded(entity, component);
			}
		}

		public void OnComponentRemoved(ComponentBase component)
		{
			var entity = Entity;
			foreach (var listener in GetAncestorsWithInterface<IDescendantListener>(false))
			{
				listener.OnComponentRemoved(entity, component);
			}
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
