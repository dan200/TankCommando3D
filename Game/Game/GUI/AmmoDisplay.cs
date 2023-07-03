using Dan200.Core.GUI;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class AmmoDisplay : Element
    {
        private int m_ammo;

        public int Ammo
        {
            get
            {
                return m_ammo;
            }
            set
            {
                if(m_ammo != value)
                {
                    m_ammo = value;
                    RequestRebuild();
                }
            }
        }

        public AmmoDisplay()
        {
            Height = LowResUI.NumbersFont.GetHeight(LowResUI.NumbersFontSize);
            m_ammo = 0;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var position = Position;
            builder.AddText(string.Format("{0}B", m_ammo), position, LowResUI.NumbersFont, LowResUI.NumbersFontSize, Colour.White, TextAlignment.Right);
        }
    }
}
