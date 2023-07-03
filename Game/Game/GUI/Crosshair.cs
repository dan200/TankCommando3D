using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Game.Components;
using Dan200.Core.GUI;

namespace Dan200.Game.GUI
{
    internal class Crosshair : Element
    {
        private Texture m_texture;
        private bool m_highlight;

        public bool Highlight
        {
            get
            {
                return m_highlight;
            }
            set
            {
                if(m_highlight != value)
                {
                    m_highlight = value;
                    RequestRebuild();
                }
            }
        }

		public Crosshair()
        {
            m_texture = Texture.Get("gui/crosshair.png", true);
            m_highlight = false;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var pos = Position;
            var size = new Vector2(0.5f * m_texture.Width, m_texture.Height);

            var region = m_highlight ?
                new Quad(0.5f, 0.0f, 0.5f, 1.0f) :
                new Quad(0.0f, 0.0f, 0.5f, 1.0f);
            builder.AddQuad(pos - 0.5f * size, pos + 0.5f * size, m_texture, region);
        }
    }
}
