using Dan200.Core.Components;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Game.GUI;
using Dan200.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Components.Misc;
using Dan200.Game.Components.Weapons;
using Dan200.Game.Systems.AI;
using Dan200.Game.Components.AI;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerHUDComponentData
    {
    }

    [RequireSystem(typeof(GUISystem))]
    [RequireSystem(typeof(ChatterSystem))]
    [RequireComponent(typeof(InputComponent))]
    [RequireComponent(typeof(PlayerMovementComponent))]
    [RequireComponent(typeof(PlayerWeaponHolderComponent))]
    [RequireComponent(typeof(PlayerInteractionComponent))]
    [RequireComponent(typeof(PlayerTrackerComponent))]
    [RequireComponent(typeof(PlayerStatsComponent))]
    [RequireComponent(typeof(HealthComponent))]
    [AfterComponent(typeof(TankMoverComponent))]
    internal class PlayerHUDComponent : Component<PlayerHUDComponentData>, IUpdate
    {
        private const float MARGIN = 18.0f;

        private ChatterSystem m_chatterSystem;
        private PlayerMovementComponent m_playerMovement;
        private PlayerWeaponHolderComponent m_playerWeapons;
        private PlayerInteractionComponent m_playerInteraction;
        private PlayerTrackerComponent m_playerTracker;
        private PlayerStatsComponent m_playerStats;
        private HealthComponent m_playerHealth;

        private Container m_container;
        private Crosshair m_crosshair;
        private HealthDisplay m_health;
        private AmmoDisplay m_ammo;
        private ChatterDisplay m_chatter;
        private TrackerDisplay m_tracker;
        private GameOverDisplay m_gameOver;

        protected override void OnInit(in PlayerHUDComponentData properties)
        {
            m_chatterSystem = Level.GetSystem<ChatterSystem>();
            m_playerMovement = Entity.GetComponent<PlayerMovementComponent>();
            m_playerWeapons = Entity.GetComponent<PlayerWeaponHolderComponent>();
            m_playerInteraction = Entity.GetComponent<PlayerInteractionComponent>();
            m_playerTracker = Entity.GetComponent<PlayerTrackerComponent>();
            m_playerStats = Entity.GetComponent<PlayerStatsComponent>();
            m_playerHealth = Entity.GetComponent<HealthComponent>();
            var gui = Level.GetSystem<GUISystem>();

            m_tracker = new TrackerDisplay(gui.MainCamera);
            gui.Screen.Elements.Add(m_tracker);

            m_container = new Container(gui.Screen.Width - 2.0f * MARGIN, gui.Screen.Height - 2.0f * MARGIN);
            m_container.Anchor = Anchor.TopLeft | Anchor.BottomRight;
            m_container.LocalPosition = new Vector2(MARGIN, MARGIN);
            gui.Screen.Elements.Add(m_container);

            m_crosshair = new Crosshair();
            m_crosshair.LocalPosition = Vector2.Zero;
            m_crosshair.Anchor = Anchor.Centre;
            m_container.Elements.Add(m_crosshair);

            m_health = new HealthDisplay();
            m_health.LocalPosition = new Vector2(0.0f, -m_health.Height);
            m_health.Anchor = Anchor.BottomLeft;
            m_container.Elements.Add(m_health);

            m_ammo = new AmmoDisplay();
            m_ammo.LocalPosition = new Vector2(0.0f, -m_ammo.Height);
            m_ammo.Anchor = Anchor.BottomRight;
            m_container.Elements.Add(m_ammo);

            m_chatter = new ChatterDisplay();
            m_chatter.LocalPosition = new Vector2(0.0f, 0.0f);
            m_chatter.Size = m_container.Size;
            m_chatter.Anchor = Anchor.TopLeft | Anchor.BottomRight;
            m_container.Elements.Add(m_chatter);

            m_gameOver = new GameOverDisplay();
            m_gameOver.Anchor = Anchor.TopLeft | Anchor.BottomRight;
            m_gameOver.LocalPosition = Vector2.Zero;
            m_gameOver.Size = m_container.Size;
            m_container.Elements.Add(m_gameOver);
        }

        protected override void OnShutdown()
        {
            var screen = Level.GetSystem<GUISystem>().Screen;

            screen.Elements.Remove(m_tracker);
            m_tracker.Dispose();
            m_tracker = null;

            screen.Elements.Remove(m_container);
            m_container.Dispose();
            m_container = null;
        }

        public void Update(float dt)
        {                
            m_health.Health = m_playerHealth.Health;

            m_crosshair.Visible = !m_playerHealth.IsDead;
            m_crosshair.Highlight = (m_playerInteraction.InteractableUnderCursor != null);

            m_tracker.Visible = !m_playerHealth.IsDead;
            m_playerTracker.PopulaterTrackerGUI(m_tracker);

            m_gameOver.Visible = m_playerHealth.IsDead;
            m_gameOver.TanksKilled = m_playerStats.GetStat(PlayerStatistic.TanksKilled);

            var weapon = m_playerWeapons.Weapon;
            var weaponGun = (weapon != null) ? weapon.GetComponent<GunComponent>() : null;
            if(weaponGun != null)
            {
                m_ammo.Visible = true;
                m_ammo.Ammo = weaponGun.AmmoInClip;
            }
            else
            {
                m_ammo.Visible = false;
            }

            Chatter newChatter;
            if(m_chatterSystem.GetNextChatter(out newChatter, !m_chatter.IsTextShowing))
            {
                m_chatter.ShowText(
                    string.Format("[{0}] {1}", newChatter.Speaker, newChatter.Dialogue)
                );
            }
        }
    }
}
