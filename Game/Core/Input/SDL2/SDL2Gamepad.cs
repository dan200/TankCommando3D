#if SDL
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;
using Dan200.Core.Window.SDL2;
using Dan200.Core.Platform.SDL2;
using SDL2;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.SDL2
{
    internal class SDL2Gamepad : IGamepad
    {
        private const float TRIGGER_DEADZONE = 0.117f; // Derived from XINPUT_GAMEPAD_TRIGGER_THRESHOLD
        private const float LEFT_STICK_DEADZONE = 0.239f; // Derived from XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE
        private const float RIGHT_STICK_DEADZONE = 0.265f; // Derived from XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE

        private SDL2Window m_window;
        private int m_joystickIndex;
        private IntPtr m_joystick;
        private IntPtr m_gameController;
        private IntPtr m_haptic;
        private int m_instanceID;
        private string m_name;
        private string m_joystickName;
        private GamepadType m_type;

        private bool m_connected;
        private Dictionary<GamepadButton, Input> m_buttons;
        private Dictionary<GamepadAxis, Input> m_axes;

        public DeviceCategory Category
        {
            get
            {
                return DeviceCategory.Gamepad;
            }
        }

        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        public int JoystickIndex
        {
            get
            {
                return m_joystickIndex;
            }
        }

        public int InstanceID
        {
            get
            {
                return m_instanceID;
            }
        }

        public GamepadType GamepadType
        {
            get
            {
                return m_type;
            }
        }

        public bool CanVibrate
        {
            get
            {
                return m_haptic != IntPtr.Zero;
            }
        }

        public IEnumerable<Input> Inputs
        {
            get
            {
                foreach(var pair in m_buttons)
                {
                    yield return pair.Value;
                }
                foreach (var pair in m_axes)
                {
                    yield return pair.Value;
                }
            }
        }

        public SDL2Gamepad(SDL2Window window, int joystickIndex)
        {
            m_window = window;
            m_joystickIndex = joystickIndex;
            m_gameController = SDL.SDL_GameControllerOpen(m_joystickIndex);
            m_joystick = SDL.SDL_GameControllerGetJoystick(m_gameController);
            m_instanceID = SDL.SDL_JoystickInstanceID(m_joystick);
            m_name = SDL.SDL_GameControllerName(m_gameController);
            m_joystickName = SDL.SDL_JoystickName(m_joystick);
            DetectType();

            // Lets get ready to rumble
            m_haptic = SDL.SDL_HapticOpenFromJoystick(m_joystick);
            if (m_haptic != IntPtr.Zero)
            {
                if (SDL.SDL_HapticRumbleSupported(m_haptic) == (int)SDL.SDL_bool.SDL_FALSE ||
                    SDL.SDL_HapticRumbleInit(m_haptic) < 0)
                {
                    SDL.SDL_HapticClose(m_haptic);
                    m_haptic = IntPtr.Zero;
                }
            }

            // Axes
            m_axes = new Dictionary<GamepadAxis, Input>();
            foreach (GamepadAxis axis in Enum.GetValues(typeof(GamepadAxis)))
            {
                var input = new Input(axis.ToString(), axis.GetPrompt(m_type));
                m_axes.Add(axis, input);
            }

            // Buttons
            m_buttons = new Dictionary<GamepadButton, Input>();
            foreach (GamepadButton button in Enum.GetValues(typeof(GamepadButton)))
            {
                m_buttons.Add(button, new Input(button.ToString(), button.GetPrompt(m_type)));
            }

            // State
            m_connected = true;
            App.Log("{0} controller connected ({1}, {2})", m_type, m_name, m_joystickName);
            if (m_haptic != IntPtr.Zero)
            {
                App.Log("Rumble supported");
            }

            Update();
        }

        public Input GetInput(string name)
        {
            GamepadAxis axis;
            if(EnumConverter.TryParse(name, out axis))
            {
                return GetInput(axis);   
            }
            GamepadButton button;
            if (EnumConverter.TryParse(name, out button))
            {
                return GetInput(button);
            }
            return null;
        }

        public Input GetInput(GamepadButton button)
        {            
            App.Assert(m_buttons.ContainsKey(button));
            return m_buttons[button];
        }

        public Input GetInput(GamepadAxis axis)
        {
            App.Assert(m_axes.ContainsKey(axis));
            return m_axes[axis];
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            return Mathf.Saturate((value - deadzone) / (1.0f - deadzone));
        }

        private void UpdateTrigger(GamepadAxis axis, SDL.SDL_GameControllerAxis sdlAxis)
        {
            short value = SDL.SDL_GameControllerGetAxis(m_gameController, sdlAxis);
            float floatValue = ApplyDeadzone( (float)value / 32767.0f, TRIGGER_DEADZONE );
            m_axes[axis].Update(floatValue);
        }

        private void UpdateJoystick(GamepadAxis upAxis, GamepadAxis downAxis, GamepadAxis leftAxis, GamepadAxis rightAxis, SDL.SDL_GameControllerAxis sdlXAxis, SDL.SDL_GameControllerAxis sdlYAxis, float deadzone)
        {
            short xValue = SDL.SDL_GameControllerGetAxis(m_gameController, sdlXAxis);
            short yValue = SDL.SDL_GameControllerGetAxis(m_gameController, sdlYAxis);
            var position = new Vector2(
                (xValue >= 0) ? ((float)xValue / 32767.0f) : -((float)xValue / -32768.0f),
                (yValue >= 0) ? ((float)yValue / 32767.0f) : -((float)yValue / -32768.0f)
            );
            var magnitude = position.Length;
            if (magnitude > 0.0f)
            {
                var newMagnitude = ApplyDeadzone(magnitude, deadzone);
                position *= (newMagnitude / magnitude);
            }
            m_axes[upAxis].Update(Mathf.Max(-position.Y, 0.0f));
            m_axes[downAxis].Update(Mathf.Max(position.Y, 0.0f));
            m_axes[leftAxis].Update(Mathf.Max(-position.X, 0.0f));
            m_axes[rightAxis].Update(Mathf.Max(position.X, 0.0f));
        }

        public void Update()
        {
            bool focus = m_window.Focus;
            bool connected = (SDL.SDL_GameControllerGetAttached(m_gameController) == SDL.SDL_bool.SDL_TRUE);
            if (focus && connected)
            {
                // Axes
                UpdateTrigger(GamepadAxis.LeftTrigger, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
                UpdateTrigger(GamepadAxis.RightTrigger, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);
                UpdateJoystick(
                    GamepadAxis.LeftStickUp, GamepadAxis.LeftStickDown, GamepadAxis.LeftStickLeft, GamepadAxis.LeftStickRight,
                    SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX,
                    SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY,
                    LEFT_STICK_DEADZONE
                );
                UpdateJoystick(
                    GamepadAxis.RightStickUp, GamepadAxis.RightStickDown, GamepadAxis.RightStickLeft, GamepadAxis.RightStickRight,
                    SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX,
                    SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY,
                    RIGHT_STICK_DEADZONE
                );

                // Buttons
                foreach (var pair in m_buttons)
                {
                    var button = pair.Key;
                    bool held = (SDL.SDL_GameControllerGetButton(m_gameController, (SDL.SDL_GameControllerButton)button) == 1);
                    pair.Value.Update(held ? 1.0f : 0.0f);
                }
            }
            else
            {
                // Axes
                foreach (var pair in m_axes)
                {
                    pair.Value.Update(0.0f);
                }

                // Buttons
                foreach (var pair in m_buttons)
                {
                    pair.Value.Update(0.0f);
                }
            }
        }

        public void Disconnect()
        {
            if (m_haptic != IntPtr.Zero)
            {
                SDL.SDL_HapticClose(m_haptic);
                m_haptic = IntPtr.Zero;
            }
            SDL.SDL_GameControllerClose(m_gameController);

            foreach (var pair in m_buttons)
            {
                pair.Value.Update(0.0f);
            }
            foreach (var pair in m_axes)
            {
                pair.Value.Update(0.0f);
            }

            m_connected = false;
            App.Log("{0} controller disconnected ({1}, {2})", m_type, m_name, m_joystickName);
        }

        public void Vibrate(float strength, float duration)
        {
            strength = Mathf.Clamp(strength, 0.0f, 1.0f);
            duration = Mathf.Max(duration, 0.0f);
            if (m_haptic != IntPtr.Zero)
            {
                SDL.SDL_HapticRumblePlay(m_haptic, strength, (uint)(duration * 1000.0f));
            }
        }

        private void DetectType()
        {
            var name = m_name.ToLowerInvariant();
            if (name.Contains("xbox one") || name.Contains("x-box one"))
            {
                m_type = GamepadType.XboxOne;
            }
            else if (name.Contains("xbox") || name.Contains("x-box") || name.Contains("x360"))
            {
                m_type = GamepadType.Xbox360;
            }
            else if (name.Contains("ps4") || name.Contains("dualshock 4") || name.Contains("playstation 4"))
            {
                m_type = GamepadType.PS4;
            }
            else if (name.Contains("ps3") || name.Contains("dualshock") || name.Contains("playstation"))
            {
                m_type = GamepadType.PS3;
            }
            else
            {
                m_type = GamepadType.Unknown;
            }
        }
    }
}
#endif
