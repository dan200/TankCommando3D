using System;
using Dan200.Core.GUI;
using Dan200.Core.Level;
using Dan200.Core.Render;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.GUI
{
    internal struct GUINineSliceComponentData
    {
        public string Image;

        [Optional(255, 255, 255, 255)]
        public Colour Colour;
    }

    [RequireComponent(typeof(GUIElementComponent))]
    internal class GUINineSliceComponent : Component<GUINineSliceComponentData>, IGUIRebuild
    {
        private GUIElementComponent m_element;
        private ITexture m_texture;
        private Colour m_colour;

        public ITexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                if (m_texture != value)
                {
                    m_texture = value;
                    m_element.RequestRebuild();
                }
            }
        }

        public Colour Colour
        {
            get
            {
                return m_colour;
            }
            set
            {
                if (m_colour != value)
                {
                    m_colour = value;
                    m_element.RequestRebuild();
                }
            }
        }

        protected override void OnInit(in GUINineSliceComponentData properties)
        {
            m_element = Entity.GetComponent<GUIElementComponent>();
            m_texture = Dan200.Core.Render.Texture.Get(properties.Image, true);
            m_colour = properties.Colour;
        }

        protected override void OnShutdown()
        {
        }

        public void Rebuild(GUIBuilder builder)
        {
            var area = m_element.Area;
            var xMargin = m_texture.Width * 0.25f;
            var yMargin = m_texture.Height * 0.25f;
            builder.AddNineSlice(area.TopLeft, area.BottomRight, xMargin, yMargin, xMargin, yMargin, m_texture);
        }
    }
}
