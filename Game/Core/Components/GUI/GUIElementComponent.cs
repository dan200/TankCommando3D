using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Util;
using Dan200.Core.Level;
using Dan200.Core.GUI;
using Dan200.Core.Components.Core;
using Dan200.Core.Serialisation;
using Dan200.Core.Interfaces.Core;

namespace Dan200.Core.Components.GUI
{
    internal interface IGUIRebuild : IComponentInterface
    {
        void Rebuild(GUIBuilder builder);
    }

    internal struct GUIElementComponentData
    {
        [Optional(Default = Anchor.TopLeft)]
        public Anchor Anchor;

        [Optional(0.0f, 0.0f)]
        public Vector2 Position;

        [Optional(Default = 0.0f)]
        public float Width;

        [Optional(Default = 0.0f)]
        public float Height;
    }

    [RequireComponent(typeof(HierarchyComponent))]
    internal class GUIElementComponent : Component<GUIElementComponentData>, IAreaProvider, IAncestryListener
    {
        private GUIElementComponent m_parent;
        private List<GUIElementComponent> m_children;

        private Anchor m_anchor;
        private Vector2 m_position;
        private Vector2 m_size;
        private GUIBuilder m_builder;
        private bool m_rebuildRequested;

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

        protected override void OnInit(in GUIElementComponentData properties)
        {
            m_parent = Entity.GetComponentOnAncestor<GUIElementComponent>();
            m_anchor = properties.Anchor;
            m_position = properties.Position;
            m_size = new Vector2(properties.Width, properties.Height);
            m_builder = new GUIBuilder();
            m_rebuildRequested = true;

            if (m_parent != null)
            {
                m_parent.AddChild(this);
            }
        }

        protected override void OnShutdown()
        {
            if(m_parent != null)
            {
                m_parent.RemoveChild(this);
            }
            m_builder.Dispose();
            m_builder = null;
        }

        public void OnAncestryChanged()
        {
            UpdateParent();
        }

        public void OnComponentAdded(Entity ancestor, ComponentBase component)
        {
            if(component is GUIElementComponent)
            {
                UpdateParent();
            }
        }

        public void OnComponentRemoved(Entity ancestor, ComponentBase component)
        {
            if(m_parent != null && m_parent.Entity == ancestor)
            {
                UpdateParent();
            }
        }

        private void UpdateParent()
        {
            var newParent = Entity.GetComponentOnAncestor<GUIElementComponent>();
            if (newParent != m_parent)
            {
                if (m_parent != null)
                {
                    m_parent.RemoveChild(this);
                }
                m_parent = newParent;
                if (m_parent != null)
                {
                    m_parent.AddChild(this);
                }
            }
        }

        private void AddChild(GUIElementComponent child)
        {
            if(m_children == null)
            {
                m_children = new List<GUIElementComponent>();
            }
            App.Assert(!m_children.Contains(child));
            m_children.Add(child);
        }

        private void RemoveChild(GUIElementComponent child)
        {
            App.Assert(m_children != null && m_children.Contains(child));
            m_children.UnorderedRemove(child);
        }

        public void Draw(IRenderer renderer, ScreenEffectHelper effect)
        {
            App.Assert(Entity.Visible);
            RebuildIfRequested(renderer);
			m_builder.Draw(renderer, effect);
            if (m_children != null)
            {
                foreach (var child in m_children)
                {
                    if (child.Entity.Visible)
                    {
                        child.Draw(renderer, effect);
                    }
                }
            }
        }
        
		public void RequestRebuild(bool recursive = false)
        {
            m_rebuildRequested = true;
            if (recursive && m_children != null)
            {
                foreach (var child in m_children)
                {
                    child.RequestRebuild(true);
                }
            }
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
				m_builder.ClipRegion = (m_parent != null) ? m_parent.Area : Area;
                foreach(var builder in Entity.GetComponentsWithInterface<IGUIRebuild>())
                {
                    builder.Rebuild(m_builder);
                }
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

                if (m_children != null)
                {
                    // Resize children
                    foreach (var child in m_children)
                    {
                        var anchor = child.Anchor;
                        var newElementSize = child.Size;
                        if (anchor.HasFlag(Anchor.Right) && anchor.HasFlag(Anchor.Left))
                        {
                            newElementSize.X += (size.X - oldSize.X);
                        }
                        if (anchor.HasFlag(Anchor.Top) && anchor.HasFlag(Anchor.Bottom))
                        {
                            newElementSize.Y += (size.Y - oldSize.Y);
                        }
                        child.Size = newElementSize;
                        if (anchor != Anchor.TopLeft)
                        {
                            child.RequestRebuild(true);
                        }
                    }
                }
            }
        }
    }
}

