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
    internal class HealthDisplay : Element
    {
        private float m_health;

        public float Health
        {
            get
            {
                return m_health;
            }
            set
            {
                if(m_health != value)
                {
                    m_health = value;
                    RequestRebuild();
                }
            }
        }

        public HealthDisplay()
        {
            Height = LowResUI.NumbersFont.GetHeight(LowResUI.NumbersFontSize);
            m_health = 0.0f;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var intHealth = (int)Math.Floor(m_health);
            if(m_health > 0.0f)
            {
                intHealth = Math.Max(intHealth, 1);
            }

            var position = Position;
            builder.AddText(string.Format("H{0}", intHealth), position, LowResUI.NumbersFont, LowResUI.NumbersFontSize, Colour.White, TextAlignment.Left );
        }
    }
}
