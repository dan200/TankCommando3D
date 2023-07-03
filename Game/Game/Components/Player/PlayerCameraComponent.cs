using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Game.Game;
using Dan200.Game.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerCameraComponentData
    {
    }

    [RequireComponent(typeof(PlayerMovementComponent))]
    [RequireComponent(typeof(PlayerSettingsComponent))]
    internal class PlayerCameraComponent : Component<PlayerCameraComponentData>, ICameraProvider
    {
        private PlayerMovementComponent m_movement;
        private PlayerSettingsComponent m_settings;

        protected override void OnInit(in PlayerCameraComponentData properties)
        {
            m_movement = Entity.GetComponent<PlayerMovementComponent>();
            m_settings = Entity.GetComponent<PlayerSettingsComponent>();
        }

        protected override void OnShutdown()
        {
        }

        public void Populate(Camera camera)
        {
            camera.Transform = Matrix4.CreateLookAt(
                m_movement.EyePos,
                m_movement.EyePos + m_movement.EyeLook,
                Vector3.YAxis
            );
            camera.Velocity = m_movement.Velocity;
            camera.FOV = m_settings.Settings.FOV * Mathf.DEGREES_TO_RADIANS;
        }
    }
}
