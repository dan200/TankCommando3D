using Dan200.Core.Level;
using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal enum DamageType
    {
        Generic = 0,
        Fall,
        Projectile,
        Explosion,
    }

    internal struct Damage
    {
        public DamageType Type;
        public float Ammount;
        public Entity Origin;
        public Vector3 Position;
        public Vector3 Direction;
    }
}
