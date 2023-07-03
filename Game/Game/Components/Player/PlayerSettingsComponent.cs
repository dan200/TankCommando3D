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
    internal struct PlayerSettingsComponentData
    {
    }

    [AfterComponent(typeof(InputComponent))]
    internal class PlayerSettingsComponent : Component<PlayerSettingsComponentData>
	{
		private Settings m_settings;
        private InputComponent m_input;

		public Settings Settings
		{
			get
			{
				return m_settings;
			}
			set
			{
				App.Assert(value != null);
                if (m_settings != value)
                {
                    m_settings = value;
                    m_settings.ApplyInputMappings(m_input.Mapper);
                }
			}
		}

        protected override void OnInit(in PlayerSettingsComponentData properties)
        {
            m_input = Entity.GetComponent<InputComponent>();
            m_settings = new Settings();
            m_settings.ApplyInputMappings(m_input.Mapper);
        }

        protected override void OnShutdown()
        {
        }
	}
}
