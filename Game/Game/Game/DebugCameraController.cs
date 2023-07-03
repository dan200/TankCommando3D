using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;

namespace Dan200.Game.Game
{
    internal class DebugCameraController : ICameraProvider
    {
        private const float TURN_SPEED = 180.0f * Mathf.DEGREES_TO_RADIANS;
        private const float MOUSE_TURN_SPEED = 0.1f * Mathf.DEGREES_TO_RADIANS;
        private const float MOVE_SPEED = 5.0f;
        private const float SHIFT_MOVE_SPEED = 10.0f;

        private Game m_game;

        public Matrix4 Transform;
        public Vector3 Velocity;
        public float FOV;

        public DebugCameraController(Game game)
        {
            m_game = game;
            Transform = Matrix4.Identity;
            Velocity = Vector3.Zero;
            FOV = 60.0f * Mathf.DEGREES_TO_RADIANS;
        }

        public void Update(float dt)
        {
            // Gather input
            float yaw = 0.0f;
            float pitch = 0.0f;
            float up = 0.0f;
            float forward = 0.0f;
            float strafe = 0.0f;
            float absYaw = 0.0f;
            float absPitch = 0.0f;
            bool shift = false;
            var keyboard = m_game.InputDevices.Keyboard;
            var mouse = m_game.InputDevices.Mouse;
            if (keyboard != null)
            {
                if (keyboard.GetInput(Key.Left).Held)
                {
                    yaw -= 1.0f;
                }
                if (keyboard.GetInput(Key.Right).Held)
                {
                    yaw += 1.0f;
                }
                if (keyboard.GetInput(Key.Down).Held)
                {
                    pitch -= 1.0f;
                }
                if (keyboard.GetInput(Key.Up).Held)
                {
                    pitch += 1.0f;
                }
                if (keyboard.GetInput(Key.Q).Held)
                {
                    up -= 1.0f;
                }
                if (keyboard.GetInput(Key.E).Held)
                {
                    up += 1.0f;
                }
                if (keyboard.GetInput(Key.W).Held)
                {
                    forward += 1.0f;
                }
                if (keyboard.GetInput(Key.S).Held)
                {
                    forward -= 1.0f;
                }
                if (keyboard.GetInput(Key.A).Held)
                {
                    strafe -= 1.0f;
                }
                if (keyboard.GetInput(Key.D).Held)
                {
                    strafe += 1.0f;
                }
                if (keyboard.GetInput(Key.LeftShift).Held)
                {
                    shift = true;
                }
            }
            if (mouse != null)
            {
                if (mouse.Locked || mouse.GetInput(MouseButton.Right).Held)
                {
                    absYaw += mouse.Delta.X * MOUSE_TURN_SPEED;
                    absPitch += -mouse.Delta.Y * MOUSE_TURN_SPEED;
                }
            }
            foreach(var gamepad in m_game.InputDevices.Gamepads)
            {
                yaw += gamepad.GetInput(GamepadAxis.RightStickRight).Value;
                yaw -= gamepad.GetInput(GamepadAxis.RightStickLeft).Value;
                pitch += gamepad.GetInput(GamepadAxis.RightStickUp).Value;
                pitch -= gamepad.GetInput(GamepadAxis.RightStickDown).Value;
                up += gamepad.GetInput(GamepadAxis.RightTrigger).Value;
                up -= gamepad.GetInput(GamepadAxis.LeftTrigger).Value;
                forward += gamepad.GetInput(GamepadAxis.LeftStickUp).Value;
                forward -= gamepad.GetInput(GamepadAxis.LeftStickDown).Value;
                strafe += gamepad.GetInput(GamepadAxis.LeftStickRight).Value;
                strafe -= gamepad.GetInput(GamepadAxis.LeftStickLeft).Value;
            }
            yaw = Mathf.Clamp(yaw, -1.0f, 1.0f);
            pitch = Mathf.Clamp(pitch, -1.0f, 1.0f);
            up = Mathf.Clamp(up, -1.0f, 1.0f);
            forward = Mathf.Clamp(forward, -1.0f, 1.0f);
            strafe = Mathf.Clamp(strafe, -1.0f, 1.0f);

            // Apply input
            var previousPos = Transform.Position;
            var moveSpeed = shift ? SHIFT_MOVE_SPEED : MOVE_SPEED;
            Velocity =
                up * Vector3.YAxis * moveSpeed +
                forward * Transform.Forward * moveSpeed +
                strafe * Transform.Right * moveSpeed;

            Transform.Position += Velocity * dt;
            Transform.Rotation = Transform.Rotation * Matrix3.CreateRotationY((yaw * TURN_SPEED * dt) + absYaw);
            Transform.Rotation = Matrix3.CreateRotationX((-pitch * TURN_SPEED * dt) - absPitch) * Transform.Rotation;
        }

        public void Populate(Camera camera)
        {
            // Setup camera transform
            camera.Transform = Transform;
            camera.Velocity = Velocity;
            camera.FOV = FOV;
        }
    }
}
