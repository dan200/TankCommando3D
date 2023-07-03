using System;
using System.Collections.Generic;
using Dan200.Core.Geometry;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Game.Level;
using Dan200.Core.Lua;
using Dan200.Core.Input;
using Dan200.Core.Interfaces;
using Dan200.Core.Components;
using Dan200.Game.User;
using Dan200.Core.Main;
using Dan200.Core.Systems;
using Dan200.Core.Components.Misc;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerInputComponentData
    {
    }

    [RequireSystem(typeof(GUISystem))]
    [RequireComponent(typeof(InputComponent))]
    [RequireComponent(typeof(PlayerSettingsComponent))]
    internal class PlayerInputComponent : Component<PlayerInputComponentData>, IUpdatePrePhysics
	{
		private const float BASE_MOUSE_SENSITIVITY = 0.025f; // Degrees per pixel
		private const float BASE_GAMEPAD_X_SENSITIVITY = 180.0f; // Degrees per second when stick at maximum
		private const float BASE_GAMEPAD_Y_SENSITIVITY = 90.0f; // Degrees per second when stick at maximum
        private const float GAMEPAD_MOVE_CURVE_POWER = 1.5f;
        private const float GAMEPAD_LOOK_CURVE_POWER = 2.5f;

        private GUISystem m_gui;
		private InputComponent m_input;
        private PlayerSettingsComponent m_settings;

		public bool Jump
		{
			get;
			private set;
		}

		public bool Interact
		{
			get;
			private set;
		}

        public bool Fire
        {
            get;
            private set;
        }

        public bool Throw
        {
            get;
            private set;
        }

        public bool Run
		{
			get;
			private set;
		}

        public float Forward
		{
			get;
			private set;
		}

        public float Right
		{
			get;
			private set;
		}

        public bool Crouch
        {
            get;
            private set;
        }

        public float YawDelta
		{
			get;
			private set;
		}

        public float PitchDelta
		{
			get;
			private set;
		}

        protected override void OnInit(in PlayerInputComponentData properties)
        {
            m_gui = Level.GetSystem<GUISystem>();
			m_input = Entity.GetComponent<InputComponent>();
            m_settings = Entity.GetComponent<PlayerSettingsComponent>();
        }

        protected override void OnShutdown()
        {
            // Release mouse
            if (m_input.Devices.Mouse != null)
            {
                m_input.Devices.Mouse.Locked = false;
            }
        }

        public void UpdatePrePhysics(float dt)
        {
            // Lock/release mouse
            var blockedByGUI = (m_gui.Screen.ModalDialog != null);
            if(m_input.Devices.Mouse != null)
            {
                m_input.Devices.Mouse.Locked = !blockedByGUI;
            }

            // Move
			var movement = Vector2.Zero;
            var run = false;
            var jump = false;
            var crouch = false;
            if (!blockedByGUI)
            {
                float forwardValue = m_input.Mapper.GetAxis("MoveForward", "MoveBack").Value;
                float strafeValue = m_input.Mapper.GetAxis("StrafeRight", "StrafeLeft").Value;
                forwardValue = Mathf.Sign(forwardValue) * Mathf.Pow(Mathf.Abs(forwardValue), GAMEPAD_MOVE_CURVE_POWER);
                strafeValue = Mathf.Sign(strafeValue) * Mathf.Pow(Mathf.Abs(strafeValue), GAMEPAD_MOVE_CURVE_POWER);
                movement.X = strafeValue;
                movement.Y = forwardValue;
                run = m_input.Mapper.GetInput("Run").Held;
                jump = m_input.Mapper.GetInput("Jump").Pressed;
                crouch = m_input.Mapper.GetInput("Crouch").Held;
            }
            if (movement.LengthSquared > 1.0f)
			{
				movement = movement.Normalise();
			}
			Forward = movement.Y;
			Right = movement.X;
            Run = run;
			Jump = jump;
            Crouch = crouch;

			// Look
			var yawDelta = 0.0f;
			var pitchDelta = 0.0f;
            if (!blockedByGUI)
            {
                if (m_input.Devices.Mouse != null && m_input.Devices.Mouse.Locked)
                {
                    var sensitivity = Mathf.ToRadians(BASE_MOUSE_SENSITIVITY * m_settings.Settings.MouseSensitivity);
                    yawDelta += m_input.Devices.Mouse.Delta.X * sensitivity;
                    pitchDelta += -m_input.Devices.Mouse.Delta.Y * sensitivity * (m_settings.Settings.InvertMouseY ? -1.0f : 1.0f);
                }

                float yawValue = m_input.Mapper.GetAxis("LookRight", "LookLeft").Value;
                float pitchValue = m_input.Mapper.GetAxis("LookUp", "LookDown").Value;
                yawValue = Mathf.Sign(yawValue) * Mathf.Pow(Mathf.Abs(yawValue), GAMEPAD_LOOK_CURVE_POWER);
                pitchValue = Mathf.Sign(pitchValue) * Mathf.Pow(Mathf.Abs(pitchValue), GAMEPAD_LOOK_CURVE_POWER);

                yawDelta += yawValue * dt * Mathf.ToRadians(BASE_GAMEPAD_X_SENSITIVITY);
                pitchDelta += pitchValue * dt * Mathf.ToRadians(BASE_GAMEPAD_Y_SENSITIVITY);
            }
			YawDelta = yawDelta;
			PitchDelta = pitchDelta;

            // Actions
            var interact = false;
            var fire = false;
            var _throw = false;
            if (!blockedByGUI)
            {
                interact = m_input.Mapper.GetInput("Interact").Pressed;
                fire = m_input.Mapper.GetInput("Fire").Held;
                _throw = m_input.Mapper.GetInput("Throw").Held;
            }
            Interact = interact;
            Fire = fire;
            Throw = _throw;
        }
    }
}
