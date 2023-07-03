using Dan200.Core.Components;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Multiplayer;
using Dan200.Core.Network;
using Dan200.Core.Physics;
using Dan200.Core.Render;
using Dan200.Game.Level;
using Dan200.Game.Script;
using Dan200.Game.User;
using System;
using System.Collections.Generic;
using System.Linq;
using Dan200.Game.Components;
using Dan200.Game.Components.Player;
using System.IO;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using System.Text;
using Dan200.Game.Options;
using Dan200.Core.Audio;
using Dan200.Core.Interfaces;
using Dan200.Core.Components.Misc;
using Dan200.Core.Assets;
using Dan200.Game.Systems.AI;
using Dan200.Game.Components.Weapons;

namespace Dan200.Game.Game
{
    internal class InGameState : LevelState
    {
        private Entity m_player;
        private ControlsDisplay m_controls;

        public InGameState(Game game, string levelLoadPath) : base(game, levelLoadPath, LevelLoadFlags.Default)
        {
        }

        public override void AddSystems(Core.Level.Level level, LevelSaveData save)
		{
            base.AddSystems(level, save);

            level.AddSystem(new ChatterSystem(), save);
            level.AddSystem(new NoiseSystem(), save);
        }

        public override void Enter(GameState previous)
        {
			base.Enter(previous);

            // Find a spawn point
            var spawnTransform = Matrix4.Identity;
            var spawnPoints = Level.GetComponents<PlayerSpawnPointComponent>().ToArray();
            if (spawnPoints.Length > 0)
            {
                var spawnPoint = spawnPoints[GlobalRandom.Int(0, spawnPoints.Length - 1)];
                spawnTransform = spawnPoint.Transform;
            }
            else
            {
                App.LogWarning("No spawn points in level, spawning player at origin");
            }

            // Create a player entity
            var playerProperties = new LuaTable();
            playerProperties["Name"] = "Player";
            playerProperties["Position"] = spawnTransform.Position.ToLuaValue();
            playerProperties["Rotation"] = (spawnTransform.GetRotationAngles() * Mathf.RADIANS_TO_DEGREES).ToLuaValue();
			m_player = EntityPrefab.Get("entities/player.entity").Instantiate(Level, playerProperties, 1); // TODO

            // Set the player's settings
            var playerSettings = m_player.GetComponent<PlayerSettingsComponent>();
            playerSettings.Settings = Game.User.Settings;

            // Give the access to input devices
            var input = m_player.GetComponent<InputComponent>();
            foreach(var device in Game.InputDevices)
            {
                input.Devices.AddDevice(device);
            }

            // Attach the camera to the player
            var playerCamera = m_player.GetComponent<PlayerCameraComponent>();
            CameraProvider = playerCamera;

            // Create the pause menu
            m_controls = new ControlsDisplay(playerSettings.Settings, input.Mapper);
            m_controls.Anchor = Anchor.TopLeft | Anchor.BottomRight;
            m_controls.LocalPosition = Vector2.Zero;
            m_controls.Size = Game.Screen.Size;
            m_controls.Visible = !(this is TestState);
            Game.Screen.Elements.Add(m_controls);

            // Pause the game
            if(m_controls.Visible)
            {
                Level.Clock.Rate = 0.0f;
                Game.Screen.ModalDialog = m_controls;
            }
        }

		public override void Update(float dt)
        {
            base.Update(dt);

            var pause = m_player.GetComponent<InputComponent>().Mapper.GetInput("Pause");
            var fire = m_player.GetComponent<InputComponent>().Mapper.GetInput("Fire");
            var _throw = m_player.GetComponent<InputComponent>().Mapper.GetInput("Throw");
            if (m_controls.Visible)
            {
                // Pause menu is open, close it
                if(pause.Pressed || fire.Pressed)
                {
                    m_controls.Visible = false;
                    Level.Clock.Rate = 1.0f;
                    if (Game.Screen.ModalDialog == m_controls)
                    {
                        Game.Screen.ModalDialog = null;
                    }
                }
                else if(_throw.Pressed)
                {
                    Game.Over = true;
                }
            }
            else
            {
                // Pause menu is closed, open it
                if(pause.Pressed)
                {
                    m_controls.Visible = true;
                    Level.Clock.Rate = 0.0f;
                    Game.Screen.ModalDialog = m_controls;
                }
                else if (m_player.GetComponent<HealthComponent>().IsDead && fire.Pressed)
                {
                    // Restart after game over
                    Restart();
                }
            }
        }

        public override void Leave(GameState next)
        {
            Game.Screen.Elements.Remove(m_controls);
            m_controls.Dispose();
            m_controls = null;

            base.Leave(next);
        }

        public virtual void Restart()
        {
            CutToState(new InGameState(Game, Level.Data.Path));
        }
    }
}
