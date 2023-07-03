using System;
using System.Collections.Generic;
using Dan200.Core.Geometry;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Lua;
using Dan200.Core.Input;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;

namespace Dan200.Core.Components.Misc
{
    internal struct InputComponentData
    {
    }

    internal class InputComponent : Component<InputComponentData>, IUpdate
	{
        private DeviceCollection m_devices;
        private InputMapper m_mapper;

        public DeviceCollection Devices
        {
            get
            {
                return m_devices;
            }
        }

        public InputMapper Mapper
        {
            get
            {
                return m_mapper;
            }
        }

        protected override void OnInit(in InputComponentData properties)
        {
            m_devices = new DeviceCollection();
            m_mapper = new InputMapper(m_devices);
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            m_mapper.Update();
        }
	}
}
