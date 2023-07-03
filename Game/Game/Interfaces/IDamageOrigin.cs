using Dan200.Core.Level;
using Dan200.Game.Components.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Interfaces
{
    internal interface IDamageOrigin : IComponentInterface
    {
        void NotifyDamageDealt(HealthComponent hurtComponent, in Damage damage);
    }
}
