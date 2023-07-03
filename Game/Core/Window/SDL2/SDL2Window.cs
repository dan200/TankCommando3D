#if SDL
using Dan200.Core.Input;
using Dan200.Core.Input.SDL2;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SDL2;
using System;
using System.Collections.Generic;
using Dan200.Core.Platform.SDL2;
using Dan200.Core.Render.OpenGL;
using Dan200.Core.Math;

#if STEAM
using Dan200.Core.Input.Steamworks;
#endif

namespace Dan200.Core.Window.SDL2
{
    internal class SDL2Window : IWindow
    {
        private IntPtr m_window;
        private IntPtr m_openGLContext;
        private bool m_vsync;

        private int m_width;
        private int m_height;
        private bool m_closed;

		private SDL2Platform m_platform;
        private DeviceCollection m_devices;
        private SDL2Mouse m_mouse;
        private SDL2Keyboard m_keyboard;
        private List<SDL2Gamepad> m_gamepads;
        private List<SDL2Joystick> m_joysticks;
		private OpenGLRenderer m_renderer;

        public string Title
        {
            get
            {
                return SDL.SDL_GetWindowTitle(m_window);
            }
            set
            {
                SDL.SDL_SetWindowTitle(m_window, value);
            }
        }

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public Vector2I Size
        {
            get
            {
                return new Vector2I(m_width, m_height);
            }
        }

        public bool Closed
        {
            get
            {
                return m_closed;
            }
        }

        public bool Fullscreen
        {
            get
            {
                uint flags = SDL.SDL_GetWindowFlags(m_window);
                return (flags &
                    ((uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN | (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP)
                ) != 0;
            }
            set
            {
                App.Log(value ? "Entering fullscreen" : "Exiting fullscreen");
                SDLUtils.CheckResult("SDL_SetWindowFullscreen", SDL.SDL_SetWindowFullscreen(m_window, value ?
                    (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0
                ));
            }
        }

        public bool Maximised
        {
            get
            {
                uint flags = SDL.SDL_GetWindowFlags(m_window);
                return (flags & (uint)SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
            }
        }

        public bool VSync
        {
            get
            {
                return m_vsync;
            }
            set
            {
                if (m_vsync != value)
                {
                    m_vsync = value;
                    VSyncChanged();
                }
            }
        }

        public bool Focus
        {
            get
            {
                return (SDL.SDL_GetKeyboardFocus() == m_window);
            }
        }

        public bool MouseFocus
        {
            get
            {
                return (SDL.SDL_GetMouseFocus() == m_window && SDL.SDL_GetKeyboardFocus() == m_window);
            }
        }

        public DisplayType DisplayType
        {
            get
            {
                return DisplayType.Monitor;
            }
        }

        public DeviceCollection InputDevices
        {
            get
            {
                return m_devices;
            }
        }

		public IRenderer Renderer
		{
			get
			{
				return m_renderer;
			}
		}

        public event StructEventHandler<IWindow> OnClosed;
        public event StructEventHandler<IWindow> OnResized;

		public SDL2Window(SDL2Platform platform, string title, int width, int height, bool fullscreen, bool maximised, bool vsync)
        {
			// Setup window
			m_platform = platform;
            if (fullscreen)
            {
                App.Log("Creating fullscreen window");
            }
            else
            {
                App.Log("Creating window sized {0}x{1}", width, height);
            }
            m_window = SDL.SDL_CreateWindow(
                title,
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                width, height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                SDL.SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS |
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                (fullscreen ? SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0) |
                (maximised ? SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED : 0)
            );
            SDLUtils.CheckResult("SDL_CreateWindow", m_window);

            // Create OpenGL context
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 1);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, 1);
            m_openGLContext = SDL.SDL_GL_CreateContext(m_window);
            SDLUtils.CheckResult("SDL_GL_CreateContext", m_openGLContext);

			// Prepare OpenGL for use
            GraphicsContext.CurrentContext = m_openGLContext;
			m_platform.InitOpenGL();

            // Get OpenGL info
            MakeCurrent();
			App.Log("GL Vendor: {0}", GL.GetString(StringName.Vendor));
			App.Log("GL Version: {0}", GL.GetString(StringName.Version));
			App.Log("GLSL Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
        	GLUtils.CheckError();

            // Cache some things
            SDL.SDL_GetWindowSize(m_window, out m_width, out m_height);
            m_closed = false;

            // Configure the new context
            m_vsync = vsync;
            VSyncChanged();

            // Create input devices
            m_mouse = new SDL2Mouse(this);
            m_keyboard = new SDL2Keyboard(this);
            m_gamepads = new List<SDL2Gamepad>();
            m_joysticks = new List<SDL2Joystick>();
            for (int joystickIndex = 0; joystickIndex < SDL.SDL_NumJoysticks(); ++joystickIndex)
            {
                if (SDL.SDL_IsGameController(joystickIndex) == SDL.SDL_bool.SDL_TRUE)
                {
                    m_gamepads.Add(new SDL2Gamepad(this, joystickIndex));
                }
                else
                {
                    m_joysticks.Add(new SDL2Joystick(this, joystickIndex));
                }
            }

            // Populate the device collection
            m_devices = new DeviceCollection();
            m_devices.AddDevice(m_mouse);
            m_devices.AddDevice(m_keyboard);
            foreach (var joystick in m_joysticks)
            {
                m_devices.AddDevice(joystick);
            }
            foreach(var gamepad in m_gamepads)
            {
                m_devices.AddDevice(gamepad);   
            }

			// Create the renderer
			m_renderer = new OpenGLRenderer(this);
        }

        private void VSyncChanged()
        {
            if (m_vsync)
            {
                if (SDL.SDL_GL_SetSwapInterval(-1) < 0)
                {
                    SDLUtils.CheckResult("SDL_GL_SetSwapInterval", SDL.SDL_GL_SetSwapInterval(1));
                    App.Log("VSync enabled");
                }
                else
                {
                    App.Log("Adaptive VSync enabled");
                }
            }
            else
            {
                SDLUtils.CheckResult("SDL_GL_SetSwapInterval", SDL.SDL_GL_SetSwapInterval(0));
                App.Log("VSync disabled");
            }
        }

        public void Dispose()
        {
			// Dispose self
			if (!m_closed)
			{
				Close();
			}
			m_renderer.Dispose();
            SDL.SDL_DestroyWindow(m_window);
			m_platform.RemoveWindow(this);
        }

        public void SetIcon(Bitmap bitmap)
        {
            var srgbCopy = bitmap.ToColourSpace(ColourSpace.SRGB);
            using (var bits = srgbCopy.Lock())
            {
                var surface = SDLUtils.CreateSurfaceFromBits(bits);
                try
                {
                    SDL.SDL_SetWindowIcon(m_window, surface);
                }
                finally
                {
                    SDL.SDL_FreeSurface(surface);
                }
            }
        }

		public void Update(float dt)
		{
			m_keyboard.Update();
			m_mouse.Update();
            foreach(var joystick in m_joysticks)
            {
                joystick.Update();
            }
            foreach(var gamepad in m_gamepads)
            {
                gamepad.Update();
            }
		}

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            // Handle window event
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    {
                        // Window changed
                        if (e.window.windowID == SDL.SDL_GetWindowID(m_window))
                        {
                            switch (e.window.windowEvent)
                            {
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                                    {
                                        Resize();
                                        break;
                                    }
                                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    {
                                        Close();
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    {
                        int joystickIndex = e.jdevice.which;
                        if (SDL.SDL_IsGameController(joystickIndex) == SDL.SDL_bool.SDL_TRUE)
                        {
                            // New gamepad
                            bool alreadyAdded = false;
                            for (int i = 0; i < m_gamepads.Count; ++i)
                            {
                                if (m_gamepads[i].JoystickIndex == joystickIndex)
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }
                            if (!alreadyAdded)
                            {
                                var gamepad = new SDL2Gamepad(this, joystickIndex);
                                m_gamepads.Add(gamepad);
                                m_devices.AddDevice(gamepad);
                            }
                        }
                        else
                        {
                            // New joystick
                            bool alreadyAdded = false;
                            for (int i = 0; i < m_joysticks.Count; ++i)
                            {
                                if (m_joysticks[i].JoystickIndex == joystickIndex)
                                {
                                    alreadyAdded = true;
                                    break;
                                }
                            }
                            if (!alreadyAdded)
                            {
                                var joystick = new SDL2Joystick(this, joystickIndex);
                                m_joysticks.Add(joystick);
                                m_devices.AddDevice(joystick);
                            }
                        }
                        break;
                    }
                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    {
                        // Lost joystick or gamepad
                        int instanceID = e.jdevice.which;
                        for (int i = m_joysticks.Count - 1; i >= 0; --i)
                        {
                            if (m_joysticks[i].InstanceID == instanceID)
                            {
                                m_joysticks[i].Disconnect();
                                m_joysticks.UnorderedRemoveAt(i);
                            }
                        }
                        for (int i = m_gamepads.Count - 1; i >= 0; --i)
                        {
                            if (m_gamepads[i].InstanceID == instanceID)
                            {
                                m_gamepads[i].Disconnect();
                                m_gamepads.UnorderedRemoveAt(i);
                            }
                        }
                        break;
                    }
            }

            // Handle input events
            m_keyboard.HandleEvent(ref e);
            m_mouse.HandleEvent(ref e);
            foreach (var joystick in m_joysticks)
            {
                joystick.HandleEvent(ref e);
            }
            foreach (var gamepad in m_gamepads)
            {
                gamepad.HandleEvent(ref e);
            }
        }

        public void MakeCurrent()
        {
            SDLUtils.CheckResult("SDL_GL_MakeCurrent", SDL.SDL_GL_MakeCurrent(m_window, m_openGLContext));
            GraphicsContext.CurrentContext = m_openGLContext;
        }

        public void SwapBuffers()
        {
            SDL.SDL_GL_SwapWindow(m_window);
        }

        private void Resize()
        {
            SDL.SDL_GetWindowSize(m_window, out m_width, out m_height);
            if (OnResized != null)
            {
                OnResized.Invoke(this, StructEventArgs.Empty);
            }
        }

        private void Close()
        {
			App.Assert(!m_closed);
            m_closed = true;
            if (OnClosed != null)
            {
                OnClosed.Invoke(this, StructEventArgs.Empty);
            }
        }
    }
}
#endif