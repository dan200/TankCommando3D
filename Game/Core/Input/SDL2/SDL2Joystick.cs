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
    internal class SDL2Joystick : IJoystick
    {
        private const float DEFAULT_AXIS_DEADZONE = 0.05f;

        private SDL2Window m_window;
        private int m_joystickIndex;
        private IntPtr m_joystick;
        private IntPtr m_haptic;
        private int m_instanceID;
        private string m_name;

        private bool m_connected;
        private Input[] m_buttons;
        struct Axis
        {
            public Input Positive;
            public Input Negative;
        }
        private Axis[] m_axes;
        struct Hat
        {
            public Input Up;
            public Input Down;
            public Input Left;
            public Input Right;
        }
        private Hat[] m_hats;

        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        public DeviceCategory Category
        {
            get
            {
                return DeviceCategory.Joystick;
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
                foreach(var button in m_buttons)
                {
                    yield return button;
                }
                foreach (var axis in m_axes)
                {
                    yield return axis.Positive;
                    yield return axis.Negative;
                }
                foreach (var hat in m_hats)
                {
                    yield return hat.Up;
                    yield return hat.Down;
                    yield return hat.Left;
                    yield return hat.Right;
                }
            }
        }

        public int NumButtons
        {
            get
            {
                return m_buttons.Length;
            }
        }

        public int NumAxes
        {
            get
            {
                return m_axes.Length;
            }
        }

        public int NumHats
        {
            get
            {
                return m_hats.Length;
            }
        }

        public SDL2Joystick(SDL2Window window, int joystickIndex)
        {
            m_window = window;
            m_joystickIndex = joystickIndex;
            m_joystick = SDL.SDL_JoystickOpen(joystickIndex);
            m_instanceID = SDL.SDL_JoystickInstanceID(m_joystick);
            m_name = SDL.SDL_JoystickName(m_joystick);

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

            // Buttons
            int numButtons = SDL.SDL_JoystickNumButtons(m_joystick);
            m_buttons = new Input[numButtons];
            for (int i = 0; i < m_buttons.Length; ++i)
            {
                m_buttons[i] = new Input("Button" + i, "Button " + i);
            }

            // Axes
            int numAxes = SDL.SDL_JoystickNumAxes(m_joystick);
            m_axes = new Axis[numAxes];
            for (int i = 0; i < m_axes.Length; ++i)
            {
                m_axes[i].Positive = new Input("+Axis" + i, "Axis " + i);
                m_axes[i].Negative = new Input("-Axis" + i, "Axis " + i);
            }

            // Hats
            int numHats = SDL.SDL_JoystickNumHats(m_joystick);
            m_hats = new Hat[numHats];
            for (int i = 0; i < m_hats.Length; ++i)
            {
                m_hats[i].Up = new Input("Hat" + i + "Up", "Hat " + i);
                m_hats[i].Down = new Input("Hat" + i + "Down", "Hat " + i);
                m_hats[i].Left = new Input("Hat" + i + "Left", "Hat " + i);
                m_hats[i].Right = new Input("Hat" + i + "Right", "Hat " + i);
            }

            // State
            m_connected = true;
            App.Log("Joystick connected ({0})", m_name);
            if (m_haptic != IntPtr.Zero)
            {
                App.Log("Rumble supported");
            }

            Update();
        }

        public Input GetInput(string name)
        {
            if (name.StartsWith("Button"))
            {
                int index;
                if (int.TryParse(name.Substring(6), out index) && index >= 0 && index < NumButtons)
                {
                    return GetButtonInput(index);
                }
            }
            else if (name.StartsWith("+Axis"))
            {
                int index;
                if (int.TryParse(name.Substring(5), out index) && index >= 0 && index < NumButtons)
                {
                    return GetAxisInput(index, JoystickAxisDirection.Positive);
                }
            }
            else if (name.StartsWith("-Axis"))
            {
                int index;
                if (int.TryParse(name.Substring(5), out index) && index >= 0 && index < NumButtons)
                {
                    return GetAxisInput(index, JoystickAxisDirection.Negative);
                }
            }
            else if(name.StartsWith("Hat"))
            {
                if(name.EndsWith("Up"))
                {
                    int index;
                    if (int.TryParse(name.Substring(3, name.Length - 2), out index) && index >= 0 && index < NumButtons)
                    {
                        return GetHatInput(index, JoystickHatDirection.Up);
                    }
                }
                else if (name.EndsWith("Down"))
                {
                    int index;
                    if (int.TryParse(name.Substring(3, name.Length - 4), out index) && index >= 0 && index < NumButtons)
                    {
                        return GetHatInput(index, JoystickHatDirection.Down);
                    }
                }
                else if (name.EndsWith("Left"))
                {
                    int index;
                    if (int.TryParse(name.Substring(3, name.Length - 4), out index) && index >= 0 && index < NumButtons)
                    {
                        return GetHatInput(index, JoystickHatDirection.Left);
                    }
                }
                else if (name.EndsWith("Right"))
                {
                    int index;
                    if (int.TryParse(name.Substring(3, name.Length - 5), out index) && index >= 0 && index < NumButtons)
                    {
                        return GetHatInput(index, JoystickHatDirection.Right);
                    }
                }
            }
            return null;
        }

        public Input GetButtonInput(int index)
        {            
            App.Assert(index >= 0 && index <= NumButtons);
            return m_buttons[index];
        }

        public Input GetAxisInput(int index, JoystickAxisDirection direction)
        {
            App.Assert(index >= 0 && index <= NumAxes);
            switch(direction)
            {
                case JoystickAxisDirection.Positive:
                default:
                    return m_axes[index].Positive;
                case JoystickAxisDirection.Negative:
                    return m_axes[index].Negative;
            }
        }

        public Input GetHatInput(int index, JoystickHatDirection direction)
        {
            App.Assert(index >= 0 && index <= NumHats);
            switch (direction)
            {
                case JoystickHatDirection.Up:
                default:
                    return m_hats[index].Up;
                case JoystickHatDirection.Down:
                    return m_hats[index].Down;
                case JoystickHatDirection.Left:
                    return m_hats[index].Left;
                case JoystickHatDirection.Right:
                    return m_hats[index].Right;
            }
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            return Mathf.Saturate((value - deadzone) / (1.0f - deadzone));
        }

        public void Update()
        {
            bool focus = m_window.Focus;
            bool connected = (SDL.SDL_JoystickGetAttached(m_joystick) == SDL.SDL_bool.SDL_TRUE);
            if (focus && connected)
            {
                // Buttons
                for (int i = 0; i < m_buttons.Length; ++i)
                {
                    byte state = SDL.SDL_JoystickGetButton(m_joystick, i);
                    m_buttons[i].Update((state == 1) ? 1.0f : 0.0f);
                }

                // Axes
                for (int i = 0; i < m_axes.Length; ++i)
                {
                    short state = SDL.SDL_JoystickGetAxis(m_joystick, i);
                    if (state >= 0)
                    {
                        float value = ApplyDeadzone((float)state / 32767.0f, DEFAULT_AXIS_DEADZONE);
                        m_axes[i].Positive.Update(value);
                        m_axes[i].Negative.Update(0.0f);
                    }
                    else
                    {
                        float value = ApplyDeadzone((float)state / -32768.0f, DEFAULT_AXIS_DEADZONE);
                        m_axes[i].Positive.Update(0.0f);
                        m_axes[i].Negative.Update(value);
                    }
                }

                // Hats
                for (int i = 0; i < m_hats.Length; ++i)
                {
                    byte state = SDL.SDL_JoystickGetHat(m_joystick, i);
                    m_hats[i].Up.Update( ((state & SDL.SDL_HAT_UP) != 0) ? 1.0f : 0.0f );
                    m_hats[i].Down.Update(((state & SDL.SDL_HAT_DOWN) != 0) ? 1.0f : 0.0f);
                    m_hats[i].Left.Update(((state & SDL.SDL_HAT_LEFT) != 0) ? 1.0f : 0.0f);
                    m_hats[i].Right.Update(((state & SDL.SDL_HAT_RIGHT) != 0) ? 1.0f : 0.0f);
                }
            }
            else
            {
                // Buttons
                foreach (var button in m_buttons)
                {
                    button.Update(0.0f);
                }

                // Axes
                foreach (var axis in m_axes)
                {
                    axis.Positive.Update(0.0f);
                    axis.Negative.Update(0.0f);
                }

                // Hat
                foreach (var hat in m_hats)
                {
                    hat.Up.Update(0.0f);
                    hat.Down.Update(0.0f);
                    hat.Left.Update(0.0f);
                    hat.Right.Update(0.0f);
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
            SDL.SDL_JoystickClose(m_joystick);

            // Buttons
            foreach (var button in m_buttons)
            {
                button.Update(0.0f);
            }

            // Axes
            foreach (var axis in m_axes)
            {
                axis.Positive.Update(0.0f);
                axis.Negative.Update(0.0f);
            }

            // Hat
            foreach (var hat in m_hats)
            {
                hat.Up.Update(0.0f);
                hat.Down.Update(0.0f);
                hat.Left.Update(0.0f);
                hat.Right.Update(0.0f);
            }

            m_connected = false;
            App.Log("Joystick disconnected ({0})", m_name);
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
    }
}
#endif
