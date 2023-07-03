using System;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    internal struct InputOrigin
    {
        public static readonly InputOrigin Invalid = new InputOrigin();

        public readonly DeviceCategory DeviceCategory;
        public readonly string InputName;

        public bool IsValid
        {
            get
            {
                return InputName != null;
            }
        }

        public InputOrigin(DeviceCategory category, string inputName)
        {
            App.Assert(inputName != null);
            DeviceCategory = category;
            InputName = inputName;
        }

        public override string ToString()
        {
            if (IsValid)
            {
                return DeviceCategory + "." + InputName;
            }
            else
            {
                return "Invalid";
            }
        }
    }

    internal class InputMapper
    {
        private class InputMapping
        {
            public readonly Input Input;
            public readonly List<InputOrigin> Sources;
            public readonly List<Pair<Input, DeviceCategory>> SourceInputs;

            public InputMapping(string name)
            {
                Input = new Input(name, "?");
                Sources = new List<InputOrigin>();
                SourceInputs = new List<Pair<Input, DeviceCategory>>();
            }
        }

        private DeviceCollection m_devices;
        private Dictionary<string, InputMapping> m_mappings;
        private DeviceCategory m_activeDeviceCategory;
        private bool m_sourceInputsNeedUpdate;

        public DeviceCollection Devices
        {
            get
            {
                return m_devices;
            }
        }

        public InputMapper(DeviceCollection devices)
        {
            m_devices = devices;
            m_mappings = new Dictionary<string, InputMapping>();
            m_activeDeviceCategory = DeviceCategory.Keyboard;
            m_sourceInputsNeedUpdate = false;
            devices.OnDeviceAdded += OnDeviceAdded;
            devices.OnDeviceRemoved += OnDeviceRemoved;
        }

        private void OnDeviceAdded(DeviceCollection sender, DeviceChangedEventArgs e)
        {
            m_sourceInputsNeedUpdate = true;
        }

        private void OnDeviceRemoved(DeviceCollection sender, DeviceChangedEventArgs e)
        {
            m_sourceInputsNeedUpdate = true;
        }

        public void UnmapAllInputs()
        {
            if (m_mappings.Count > 0)
            {
                foreach (var mapping in m_mappings.Values)
                {
                    mapping.Sources.Clear();
                    mapping.SourceInputs.Clear();
                    mapping.Input.Prompt = "?";
                }
            }
        }

        public void UnmapInput(string input)
        {
            InputMapping mapping;
            if(m_mappings.TryGetValue(input, out mapping))
            {
                mapping.Sources.Clear();
                mapping.SourceInputs.Clear();
                mapping.Input.Prompt = "?";
            }
        }

        public void MapInput(string input, Key key)
        {
            MapInput(input, new InputOrigin(DeviceCategory.Keyboard, key.ToString()));
        }

        public void MapInput(string input, MouseButton button)
        {
            MapInput(input, new InputOrigin(DeviceCategory.Mouse, button.ToString()));
        }

        public void MapInput(string input, GamepadButton button)
        {
            MapInput(input, new InputOrigin(DeviceCategory.Gamepad, button.ToString()));
        }

        public void MapInput(string input, GamepadAxis axis)
        {
            MapInput(input, new InputOrigin(DeviceCategory.Gamepad, axis.ToString()));
        }

        public void MapInput(string input, InputOrigin origin)
        {
            App.Assert(origin.IsValid);
            var mapping = EnsureMapping(input);
            mapping.Sources.Add(origin);
            UpdateSourceInputs(mapping);
            UpdatePrompt(mapping);
        }

        public Input GetInput(string input)
        {
            var mapping = EnsureMapping(input);
            return mapping.Input;
        }

        public Axis GetAxis(string positive, string negative)
        {
            return new Axis(GetInput(positive), GetInput(negative));
        }

        private void UpdateSourceInputs(InputMapping mapping)
        {
            mapping.SourceInputs.Clear();
            foreach(var source in mapping.Sources)
            {
                foreach(var device in m_devices.GetDevices(source.DeviceCategory))
                {
                    var sourceInput = device.GetInput(source.InputName);
                    if(sourceInput != null)
                    {
                        mapping.SourceInputs.Add(Pair.Create(sourceInput, device.Category));
                    }
                }
            }
        }

        private void UpdatePrompt(InputMapping mapping)
        {
            mapping.Input.Prompt = "?";
            foreach(var pair in mapping.SourceInputs)
            {
                if(ArePromptsCompatible(m_activeDeviceCategory, pair.Second))
                {
                    mapping.Input.Prompt = pair.First.Prompt;
                    break;
                }
            }
        }

        public void Update()
        {
            // Update input sources
            bool promptsNeedUpdate = false;
            if (m_sourceInputsNeedUpdate)
            {
                foreach (var mapping in m_mappings.Values)
                {
                    UpdateSourceInputs(mapping);
                }
                m_sourceInputsNeedUpdate = false;
                promptsNeedUpdate = true;
            }

            // Update input values
            var activeDeviceCategory = m_activeDeviceCategory;
            foreach(var mapping in m_mappings.Values)
            {
                float value = 0.0f;
                foreach(var pair in mapping.SourceInputs)
                {
                    var input = pair.First;
                    value = Mathf.Max(value, input.Value);
                    if(input.Value > 0.0f)
                    {
                        activeDeviceCategory = pair.Second;
                    }
                }
                mapping.Input.Update(value);
            }
            if(activeDeviceCategory != m_activeDeviceCategory)
            {
                m_activeDeviceCategory = activeDeviceCategory;
                promptsNeedUpdate = true;
            }

            // Update prompts
            if(promptsNeedUpdate)
            {
                foreach (var mapping in m_mappings.Values)
                {
                    UpdatePrompt(mapping);
                }
            }
        }

        private bool ArePromptsCompatible(DeviceCategory category, DeviceCategory other)
        {
            switch (category)
            {
                case DeviceCategory.Mouse:
                case DeviceCategory.Keyboard:
                    return other == DeviceCategory.Mouse || other == DeviceCategory.Keyboard;
                default:
                    return other == category;
            }
        }

        private InputMapping EnsureMapping(string input)
        {
            InputMapping result;
            if(!m_mappings.TryGetValue(input, out result))
            {
                result = new InputMapping(input);
                m_mappings.Add(input, result);
            }
            return result;
        }
    }
}
