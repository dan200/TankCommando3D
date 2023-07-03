using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Util;

namespace Dan200.Core.GUI
{
	internal struct ElementSet : IEnumerable<Element>
    {
        private Element m_owner;

        public int Count
        {
            get
            {
                return m_owner.m_children.Count;
            }
        }

        public ElementSet(Element owner)
        {
            m_owner = owner;
        }

        public void Add(Element element)
        {
            App.Assert(element.Parent == null, "Element already has a parent");

			// Add the element at the correct Z position
			var children = m_owner.m_children;
			if (children.Count == 0 || element.ZOrder >= children.Last().ZOrder)
			{
				children.Add(element);
			}
			else
			{
				for (int i = 0; i < children.Count; ++i)
				{
					var child = children[i];
					if (element.ZOrder < child.ZOrder)
					{
						children.Insert(i, element);
						break;
					}
				}
			}

			// Initialise the element
            if (m_owner.Screen != null)
            {
                element.Init(m_owner);
            }
        }

        public void Remove(Element element)
        {
            App.Assert(element.Parent == m_owner, "Element is not parented to this element");

            // Remove the element
            m_owner.m_children.Remove(element);
            if (m_owner.Screen.ModalDialog == element)
            {
                m_owner.Screen.ModalDialog = null;
            }
        }

        public void Clear()
        {
            m_owner.m_children.Clear();
        }

		public List<Element>.Enumerator GetEnumerator()
        {
            return m_owner.m_children.GetEnumerator();
        }

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

	internal abstract class Element : IDisposable
    {
        private Screen m_screen;
        private Element m_parent;
        internal List<Element> m_children;
        private Anchor m_anchor;
        private Vector2 m_position;
        private Vector2 m_size;
        private bool m_visible;
		private int m_zOrder;
		private GUIBuilder m_builder;
        private bool m_rebuildRequested;

        public Screen Screen
        {
            get
            {
                return m_screen;
            }
        }

        public Element Parent
        {
            get
            {
                return m_parent;
            }
        }

        public ElementSet Elements
        {
            get
            {
                return new ElementSet(this);
            }
        }

        public Anchor Anchor
        {
            get
            {
                return m_anchor;
            }
            set
            {
                if (m_anchor != value)
                {
                    m_anchor = value;
                    RequestRebuild(true);
                }
            }
        }

        public Vector2 LocalPosition
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                RequestRebuild(true);
            }
        }

        public Vector2 Position
        {
            get
            {
                if (m_parent != null)
                {
                    return m_parent.GetAnchorPosition(m_anchor) + m_position;
                }
                return m_position;
            }
        }

        public Vector2 Size
        {
            get
            {
                return new Vector2(Width, Height);
            }
            set
            {
                Resize(value);
            }
        }

        public float Width
        {
            get
            {
                return Mathf.Max(m_size.X, 0.0f);
            }
            set
            {
                Resize(new Vector2(value, m_size.Y));
            }
        }

        public float Height
        {
            get
            {
                return Mathf.Max(m_size.Y, 0.0f);
            }
            set
            {
                Resize(new Vector2(m_size.X, value));
            }
        }

        public Quad LocalArea
        {
            get
            {
                return new Quad(LocalPosition, Width, Height);
            }
        }

        public Quad Area
        {
            get
            {
                return new Quad(Position, Width, Height);
            }
        }

        public Vector2 Center
        {
            get
            {
                return Position + 0.5f * Size;
            }
        }

        public float AspectRatio
        {
            get
            {
                return Width / Height;
            }
        }

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
            }
        }

		public int ZOrder
		{
			get
			{
				return m_zOrder;
			}
			set
			{
				if (m_zOrder != value)
				{
					m_zOrder = value;
					if (m_parent != null)
					{
						m_parent.ZSortChildren();
					}
				}
			}
		}

		protected Element()
        {
            m_screen = null;
            m_parent = null;
            m_children = new List<Element>();
            m_anchor = Anchor.TopLeft;
            m_position = Vector2.Zero;
            m_size = Vector2.Zero;
            m_visible = true;
			m_zOrder = 0;
			m_builder = new GUIBuilder();
            m_rebuildRequested = true;
        }

        public virtual void Dispose()
        {
            // Dispose any children still attached
            foreach (var child in m_children)
            {
                child.Dispose();
            }
			m_children = null;
			m_builder.Dispose();
        }

		protected abstract void OnInit();
		protected abstract void OnUpdate(float dt);
		protected abstract void OnRebuild(GUIBuilder builder);

        internal void Init(Screen screen)
        {
            m_screen = screen;
            m_parent = null;
            OnInit();
            RequestRebuild(true);
            foreach (var child in m_children)
            {
                child.Init(this);
            }
        }

        internal void Init(Element parent)
        {
            m_screen = parent.Screen;
            m_parent = parent;
            OnInit();
            RequestRebuild(true);
            foreach (var child in m_children)
            {
                child.Init(this);
            }
        }

        internal void Update(float dt)
        {
            OnUpdate(dt);
            for (int i = 0; i < m_children.Count; ++i)
            {
                var child = m_children[i];
                child.Update(dt);
            }
        }

		internal void Draw(IRenderer renderer, ScreenEffectHelper effect)
        {
            if (m_visible)
            {
                RebuildIfRequested(renderer);
				m_builder.Draw(renderer, effect);
                foreach (var child in m_children)
                {
					child.Draw(renderer, effect);
                }
            }
        }
        
		protected void RequestRebuild(bool recursive = false)
        {
            m_rebuildRequested = true;
            if (recursive)
            {
                foreach (var child in m_children)
                {
                    child.RequestRebuild(true);
                }
            }
        }

        private static int CompareByZ(Element x, Element y)
        {
            return x.ZOrder.CompareTo(y.ZOrder);
        }

        private void ZSortChildren()
		{
			m_children.Sort(CompareByZ);
		}

        public Vector2 GetAnchorPosition(Anchor anchor)
        {
            var pos = Position;
            var size = Size;
            var anchorPos = Vector2.Zero;

            if (anchor.HasFlag(Anchor.Left))
            {
                anchorPos.X = pos.X;
            }
            else if (anchor.HasFlag(Anchor.Right))
            {
                anchorPos.X = pos.X + size.X;
            }
            else
            {
                anchorPos.X = pos.X + 0.5f * size.X;
            }

            if (anchor.HasFlag(Anchor.Top))
            {
                anchorPos.Y = pos.Y;
            }
            else if (anchor.HasFlag(Anchor.Bottom))
            {
                anchorPos.Y = pos.Y + size.Y;
            }
            else
            {
                anchorPos.Y = pos.Y + 0.5f * size.Y;
            }

            return anchorPos;
        }

        private void RebuildIfRequested(IRenderer renderer)
        {
            if (m_rebuildRequested)
            {
				m_builder.Clear();
				m_builder.ClipRegion = (Parent != null) ? Parent.Area : Screen.Area;
				OnRebuild(m_builder);
				m_builder.Upload(renderer);
                m_rebuildRequested = false;
            }
        }

        private void Resize(Vector2 size)
        {
            if (size != m_size)
            {
                // Resize self
                var oldSize = m_size;
                m_size = size;
                RequestRebuild();

                // Resize children
                foreach (var element in m_children)
                {
                    var anchor = element.m_anchor;
                    var newElementSize = element.m_size;
                    if (anchor.HasFlag(Anchor.Right) && anchor.HasFlag(Anchor.Left))
                    {
                        newElementSize.X += (size.X - oldSize.X);
                    }
                    if (anchor.HasFlag(Anchor.Top) && anchor.HasFlag(Anchor.Bottom))
                    {
                        newElementSize.Y += (size.Y - oldSize.Y);
                    }
                    element.Size = newElementSize;
                    if (anchor != Anchor.TopLeft)
                    {
                        element.RequestRebuild(true);
                    }
                }
            }
        }
    }
}

