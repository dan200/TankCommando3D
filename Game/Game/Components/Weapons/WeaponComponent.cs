using Dan200.Core.Components.Core;
using Dan200.Core.Level;
using Dan200.Core.Serialisation;
using Dan200.Game.Components.Player;
using Dan200.Game.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct WeaponComponentData
    {
        [Optional(Default = true)]
        public bool CanBePickedUp;
    }

    [RequireComponent(typeof(TransformComponent))]
    internal class WeaponComponent : Component<WeaponComponentData>, IInteractable
    {
        private WeaponComponentData m_properties;

        protected override void OnInit(in WeaponComponentData properties)
        {
            m_properties = properties;
        }

        protected override void OnShutdown()
        {
        }

        public bool CanInteract(Entity player, Interaction interaction)
        {
            if (interaction == Interaction.UseOnce && m_properties.CanBePickedUp)
            {
                var playerWeapon = player.GetComponent<PlayerWeaponHolderComponent>();
                if (playerWeapon != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Interact(Entity player, Interaction interaction)
        {
            if (CanInteract(player, interaction))
            {
                player.GetComponent<PlayerWeaponHolderComponent>().TakeWeapon(this);
                return true;
            }
            return false;
        }
    }
}
