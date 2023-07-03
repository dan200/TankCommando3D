using Dan200.Core.Level;
using Dan200.Core.Serialisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct AmmoComponentData
    {
        [Range(Min = 0)]
        public int ClipSize;

        [Optional(Default = false)]
        public bool Infinite;
    }

    internal class AmmoComponent : Component<AmmoComponentData>
    {
        private AmmoComponentData m_properties;
        private int m_ammoInClip;

        public int AmmoInClip
        {
            get
            {
                return m_ammoInClip;
            }
        }

        protected override void OnInit(in AmmoComponentData properties)
        {
            m_properties = properties;
            m_ammoInClip = properties.ClipSize;
        }

        protected override void OnShutdown()
        {
        }

        public int ConsumeAmmo(int count)
        {
            if (m_properties.Infinite)
            {
                return count;
            }
            else
            {
                int ammoTaken = Math.Min(count, m_ammoInClip);
                m_ammoInClip -= ammoTaken;
                return ammoTaken;
            }
        }
    }
}
