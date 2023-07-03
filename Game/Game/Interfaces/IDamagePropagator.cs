using Dan200.Core.Level;
using Dan200.Game.Components.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Interfaces
{
    internal interface IDamagePropagator : IComponentInterface
    {
        Entity DamageOrigin
        {
            get;
            set;
        }
    }
}
