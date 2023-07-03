using System;
using System.Collections;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    internal struct DeviceChangedEventArgs
    {
        public readonly IDevice Device;

        public DeviceChangedEventArgs(IDevice device)
        {
            Device = device;
        }
    }
    
    internal class DeviceCollection : IEnumerable<IDevice>
    {
        private Dictionary<DeviceCategory, List<IDevice>> m_devices;

        public IKeyboard Keyboard
        {
            get
            {
                return (IKeyboard)GetFirstDevice(DeviceCategory.Keyboard);
            }
        }

        public IMouse Mouse
        {
            get
            {
                return (IMouse)GetFirstDevice(DeviceCategory.Mouse);
            }
        }

        public ITouchscreen Touchscreen
        {
            get
            {
                return (ITouchscreen)GetFirstDevice(DeviceCategory.Touchscreen);
            }
        }

        public IEnumerable<IGamepad> Gamepads
        {
            get
            {
                foreach(var gamepad in GetDevices(DeviceCategory.Gamepad))
                {
                    yield return (IGamepad)gamepad;
                }
            }
        }

        public IEnumerable<IJoystick> Joysticks
        {
            get
            {
                foreach (var joystick in GetDevices(DeviceCategory.Joystick))
                {
                    yield return (IJoystick)joystick;
                }
            }
        }

        public event StructEventHandler<DeviceCollection, DeviceChangedEventArgs> OnDeviceAdded;
        public event StructEventHandler<DeviceCollection, DeviceChangedEventArgs> OnDeviceRemoved;

        public DeviceCollection()
        {
            var categories = EnumConverter.GetValues<DeviceCategory>();
            m_devices = new Dictionary<DeviceCategory, List<IDevice>>(categories.Length);
            foreach(var category in categories)
            {
                m_devices.Add(category, new List<IDevice>());
            }
        }

        public void AddDevice(IDevice device)
        {
            App.Assert(!m_devices[device.Category].Contains(device));
            App.Assert(m_devices[device.Category].Count == 0 || AreMultipleDevicesAllowed(device.Category));
            m_devices[device.Category].Add(device);
            FireOnDeviceAdded(device);
        }

        public void RemoveDevice(IDevice device)
        {
            App.Assert(m_devices[device.Category].Contains(device));
            m_devices[device.Category].Remove(device);
            FireOnDeviceRemoved(device);
        }

        public IDevice GetFirstDevice(DeviceCategory category)
        {
            var devices = m_devices[category];
            return (devices.Count > 0) ? devices[0] : null;
        }

        public IReadOnlyList<IDevice> GetDevices(DeviceCategory category)
        {
            return m_devices[category];
        }

        public IEnumerator<IDevice> GetEnumerator()
        {
            foreach(var pair in m_devices)
            {
                foreach(var device in pair.Value)
                {
                    yield return device;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void FireOnDeviceAdded(IDevice device)
        {
            if(OnDeviceAdded != null)
            {
                OnDeviceAdded.Invoke(this, new DeviceChangedEventArgs(device));
            }
        }

        private void FireOnDeviceRemoved(IDevice device)
        {
            if (OnDeviceRemoved != null)
            {
                OnDeviceRemoved.Invoke(this, new DeviceChangedEventArgs(device));
            }
        }

        private bool AreMultipleDevicesAllowed(DeviceCategory category)
        {
            switch(category)
            {
                case DeviceCategory.Keyboard:
                case DeviceCategory.Mouse:
                case DeviceCategory.Touchscreen:
                    return false;
                default:
                    return true;
            }
        }
    }
}
