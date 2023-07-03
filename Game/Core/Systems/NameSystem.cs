using System;
using System.Collections.Generic;
using Dan200.Core.Components;
using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;

namespace Dan200.Core.Systems
{
    internal struct NameSystemData
    {
    }

    internal class NameSystem : System<NameSystemData>
	{
		private Dictionary<Entity, string> m_names;
		private Dictionary<string, Entity> m_rootChildren;
		private Dictionary<Entity, Dictionary<string, Entity>> m_children;

        protected override void OnInit(in NameSystemData properties)
		{
			m_names = new Dictionary<Entity, string>();
			m_rootChildren = new Dictionary<string, Entity>();
			m_children = new Dictionary<Entity, Dictionary<string, Entity>>();
		}

		protected override void OnShutdown()
		{
			m_names = null;
			m_rootChildren = null;
			m_children = null;
		}

		public void AddEntity(Entity entity, string name)
		{
			App.Assert(!entity.Dead);
            App.Assert(!m_names.ContainsKey(entity));

            // If name is null, nothing to do
            if(name == null)
            {
                return;
            }

			// Store the entity name
			m_names.Add(entity, name);

			// Add the entity to it's parent's children
			var parent = HierarchyComponent.GetParent(entity);
			Dictionary<string, Entity> children;
			if (parent == null)
			{
				children = m_rootChildren;
			}
			else
			{
				if (!m_children.TryGetValue(parent, out children))
				{
					children = new Dictionary<string, Entity>();
					m_children.Add(parent, children);
				}
			}
			App.Assert(!children.ContainsValue(entity));
			if (children.ContainsKey(name))
			{
				var existing = children[name];
				if (parent == null)
				{
					App.LogWarning("Multiple root entities named {0} ({1}, {2}). Only {1} will be findable", name, existing.ID, entity.ID);
				}
				else
				{
					App.LogWarning("Multiple entities named {0} ({1}, {2}) on the same parent. Only {1} will the findable", name, existing.ID, entity.ID);
				}
			}
			else
			{
				children.Add(name, entity);
			}
		}

		public void RemoveEntity(Entity entity)
		{
			App.Assert(!entity.Dead);
			
            // Get the name
            string name;
            if (!m_names.TryGetValue(entity, out name))
            {
                // If not named, nothing to do
                return;
            }

            // Remove the name
			m_names.Remove(entity);

			// Remove the entity from it's parent's children
			var parent = HierarchyComponent.GetParent(entity);
			Dictionary<string, Entity> children;
			if (parent == null)
			{
				children = m_rootChildren;
			}
			else
			{
				App.Assert(m_children.ContainsKey(parent));
				children = m_children[parent];
			}
			Entity existing;
			if (children.TryGetValue(name, out existing) && existing == entity)
			{
				children.Remove(name);
				if (children != m_rootChildren && children.Count <= 0)
				{
					m_children.Remove(parent);
				}
			}
		}

		public void MoveEntity(Entity entity, Entity oldParent, Entity newParent)
		{
			App.Assert(!entity.Dead);
			App.Assert(oldParent != newParent);

            // Get the name
            string name;
            if(!m_names.TryGetValue(entity, out name))
            {
                // If not named, nothing to do
                return;
            }

			// Remove the entity from it's old parent's children
			Dictionary<string, Entity> children;
			if (oldParent == null)
			{
				children = m_rootChildren;
			}
			else
			{
				App.Assert(m_children.ContainsKey(oldParent));
				children = m_children[oldParent];
			}
			Entity existing;
			if (children.TryGetValue(name, out existing) && existing == entity)
			{
				children.Remove(name);
				if (children != m_rootChildren && children.Count <= 0)
				{
					m_children.Remove(oldParent);
				}
			}

			// Add the entity to it's new parent list
			if (newParent == null)
			{
				children = m_rootChildren;
			}
			else
			{
				if (!m_children.TryGetValue(newParent, out children))
				{
					children = new Dictionary<string, Entity>();
					m_children.Add(newParent, children);
				}
			}
			App.Assert(!children.ContainsValue(entity));
			if (children.ContainsKey(name))
			{
				existing = children[name];
				if (newParent == null)
				{
					App.LogWarning("Multiple root entities named {0} ({1}, {2}). Only {1} will be findable", name, existing.ID, entity.ID);
				}
				else
				{
					App.LogWarning("Multiple entities named {0} ({1}, {2}) on the same parent. Only {1} will the findable", name, existing.ID, entity.ID);
				}
			}
			else
			{
				children.Add(name, entity);
			}
		}

		public string GetName(Entity entity)
		{
			string name;
			if (m_names.TryGetValue(entity, out name))
			{
				return name;
			}
			return null;
		}

		public string GetPath(Entity entity)
		{
			var name = GetName(entity);
			if (name == null)
			{
				return null;
			}

			var parent = HierarchyComponent.GetParent(entity);
			if (parent != null)
			{
				var parentPath = GetPath(parent);
				if (parentPath != null)
				{
					return parentPath + '/' + name;
				}
				else
				{
					return null;
				}
			}
			else
			{
				return name;
			}
		}

		public Entity Lookup(string path, Entity root=null)
		{
			int pos = 0;

			if (path.Length > 0 && path[0] == '/')
			{
				// Start at the root
				pos = 1;
				root = null;
			}

			while (pos < path.Length)
			{
				// Extract the next part of the path
				string part;
				int slashIndex = path.IndexOf('/', pos);
				if (slashIndex >= 0)
				{
					part = path.Substring(pos, (slashIndex - pos));
					pos = slashIndex + 1;
				}
				else
				{
					part = path.Substring(pos);
					pos = path.Length;
				}

				// Interpret it
				if (part.Length == 0 || part == ".")
				{
					// Stay in place
					continue;
				}
				else if (part == "..")
				{
					if (root != null)
					{
						// Go up a level
						root = HierarchyComponent.GetParent(root);
					}
					else
					{
						// Attempt to ascend beyond the root
						return null;
					}
				}
				else
				{
					// Look in children
					Dictionary<string, Entity> children;
					if (root == null)
					{
						children = m_rootChildren;
					}
					else if (!m_children.TryGetValue(root, out children))
					{
						// Entity has no children
						return null;
					}

					Entity child;
					if (children.TryGetValue(part, out child))
					{
						// Child found
						root = child;
					}
					else
					{
						// No matching children
						return null;
					}
				}
			}

			return root;
		}
	}
}
