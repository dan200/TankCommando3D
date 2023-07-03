#if SDL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Network;
using Dan200.Core.Network.Builtin;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Core.Window;
using Dan200.Core.Window.SDL2;
using SDL2;
using System.IO;
using Dan200.Core.Window.Headless;
using Dan200.Core.Render.OpenGL;
using OpenTK.Graphics.OpenGL;

#if STEAM
using Steamworks;
using Dan200.Core.Network.Steamworks;
#endif

namespace Dan200.Core.Platform.SDL2
{
	internal class SDL2Platform : IPlatform
	{
		private PlatformID m_platform;
		private INetwork m_network;
		private List<SDL2Window> m_windows;
		private HeadlessWindow m_headlessWindow;
        private bool m_headless;

#if STEAM
		private bool m_steamworksInitialised;
#endif
		private bool m_sdlInitialised;
		private bool m_sdlImageInitialised;
		private bool m_openGLInitialised;

		public bool SupportsMultipleWindows
		{
			get
			{
                return !m_headless;
			}
		}

		public PlatformID PlatformID
		{
			get
			{
				return m_platform;
			}
		}

		public INetwork Network
		{
			get
			{
				return m_network;
			}
		}

		public string SystemLanguage
		{
			get
			{
#if STEAM
				return ((SteamworksNetwork)m_network).Language;
#else
				return CultureInfo.CurrentUICulture.Name.Replace('-', '_');
#endif
			}
		}

        private SDL2Platform(bool headless)
		{
			// Determine platform
			string platformString = SDL.SDL_GetPlatform();
			switch (platformString)
			{
				case "Windows":
					{
						m_platform = PlatformID.Windows;
						break;
					}
				case "Mac OS X":
					{
						m_platform = PlatformID.MacOS;
						break;
					}
				case "Linux":
					{
						m_platform = PlatformID.Linux;
						break;
					}
				default:
					{
						m_platform = PlatformID.Unknown;
						break;
					}
			}

			// Defer network creation until later
			m_network = null;

			// Create windows
			m_windows = new List<SDL2Window>();
            m_headless = headless;
		}

		public void Dispose()
		{
			// Dispose windows
			while (m_windows.Count > 0)
			{
				var window = m_windows[0];
				window.Dispose();
				App.Assert(!m_windows.Contains(window));
			}
			m_windows = null;

			// Shutdown SDL
			if (m_sdlImageInitialised)
			{
				SDL_image.IMG_Quit();
				App.Log("SDL2_Image shut down");
				m_sdlImageInitialised = false;
			}
			if (m_sdlInitialised)
			{
				SDL.SDL_Quit();
				App.Log("SDL2 shut down");
				m_sdlInitialised = false;
			}

#if STEAM
			// Shutdown steamworks
			if (m_steamworksInitialised)
			{
				SteamAPI.Shutdown();
				App.Log("Steamworks shut down");
				m_steamworksInitialised = false;
			}
#endif
			m_network = null;
		}

		private void UpdateWindows(float dt)
		{
			// Update windows
			foreach(var window in m_windows)
			{
				window.Update(dt);
			}
		}

		public IWindow CreateWindow(string title, int width, int height, bool fullscreen, bool maximised, bool vsync)
		{
            if (m_headless)
			{
				if (m_headlessWindow != null)
				{
					throw new InvalidOperationException("Only one window is supported in headless mode");
				}
				m_headlessWindow = new HeadlessWindow(title, width, height);
				return m_headlessWindow;
			}
			else
			{
				var window = new SDL2Window(this, title, width, height, fullscreen, maximised, vsync);
				m_windows.Add(window);
				return window;
			}
		}

		public void RemoveWindow(SDL2Window window)
		{
			App.Assert(m_windows.Contains(window));
			m_windows.Remove(window);
		}

		public void ShowMessageBox(string title, string message, bool isError)
		{
			try
			{
				SDLUtils.CheckResult("SDL_ShowSimpleMessageBox", SDL.SDL_ShowSimpleMessageBox(
					SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
					title,
					message,
					IntPtr.Zero
				));
			}
			catch (SDLException e)
			{
				App.LogError("{0}", e.Message);
			}
		}

		public bool OpenFileBrowser(string path)
		{
			App.Log("Opening file browser to {0}", path);
			try
			{
				Process.Start(path);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool OpenTextEditor(string path)
		{
			App.Log("Opening {0} in text editor", path);
			var info = new ProcessStartInfo(path);
			info.UseShellExecute = true;
			try
			{
				Process.Start(info);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private bool OpenOverlayWebBrowser(string url)
		{
#if STEAM
			return ((SteamworksNetwork)m_network).OpenOverlayWebBrowser(url);
#else
			return false;
#endif
		}

		private bool OpenExternalWebBrowser(string url)
		{
            if (App.Steam && PlatformID == PlatformID.Linux) // Steam's Linux sandbox doesn't like browsers :(
            {
                return false;
            }

			App.Log("Opening web browser to {0}", url);
			try
			{
				Process.Start(url);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool OpenWebBrowser(string url, WebBrowserType prefferedType)
		{
			if (prefferedType == WebBrowserType.Overlay)
			{
				return OpenOverlayWebBrowser(url) || OpenExternalWebBrowser(url);
			}
			else
			{
				return OpenExternalWebBrowser(url) || OpenOverlayWebBrowser(url);
			}
		}

		private bool InitSteamworks()
		{
#if STEAM
			App.Assert(!m_steamworksInitialised);

			// Setup Steamworks
			if (!App.Debug)
			{
				// Relaunch game under Steam if launched externally
				if (File.Exists("steam_appid.txt"))
				{
					File.Delete("steam_appid.txt");
				}

				AppId_t appID = (App.Info.SteamAppID > 0) ? new AppId_t(App.Info.SteamAppID) : AppId_t.Invalid;
				if (SteamAPI.RestartAppIfNecessary(appID))
				{
					App.Log("Relaunching game in Steam");
					return false;
				}
			}

			// Init Steamworks
			if (!SteamAPI.Init())
			{
				throw new SteamworksException("SteamAPI_Init", "If this is the first time you have launched " + App.Info.Title + ", try restarting Steam");
			}
			App.Log("Steamworks initialised");
			m_steamworksInitialised = true;
#endif
			return true;
		}

		private void InitSDL2()
		{
			App.Assert(!m_sdlInitialised);
			App.Assert(!m_sdlImageInitialised);

			// Print SDL version
			SDL.SDL_version version;
			SDL.SDL_GetVersion(out version);
			App.Log("SDL version: {0}.{1}.{2}", version.major, version.minor, version.patch);
			SDL.SDL_VERSION(out version);

			// Init SDL
			SDL.SDL_SetHint(SDL.SDL_HINT_VIDEO_MAC_FULLSCREEN_SPACES, "1");
			SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
			uint flags = 0;
            if (!m_headless)
			{
				flags |= SDL.SDL_INIT_VIDEO;
				flags |= SDL.SDL_INIT_GAMECONTROLLER;
				flags |= SDL.SDL_INIT_JOYSTICK;
				flags |= SDL.SDL_INIT_HAPTIC;
			}
			SDLUtils.CheckResult("SDL_Init", SDL.SDL_Init(flags));
			App.Log("SDL2 Initialised");
			m_sdlInitialised = true;

			SDLUtils.CheckResult("IMG_Init", SDL_image.IMG_Init(
				SDL_image.IMG_InitFlags.IMG_INIT_PNG
			));
			m_sdlImageInitialised = true;
			App.Log("SDL2_Image initialised");
		}

		public void InitOpenGL()
		{
			if (!m_openGLInitialised)
			{
                if (!m_headless)
				{
					// Prepare OpenGL for use
					GL.LoadAll(SDL.SDL_GL_GetProcAddress);
					SDL.SDL_ClearError(); // For some reason an SDL2 error is raised during GL.LoadAll()
				}
				m_openGLInitialised = true;
			}
		}

		private void InitSavePath()
		{
#if STEAM
			// Save to the Steam userdata folder
			string savePath;
			SteamworksUtils.CheckResult("SteamUser.GetUserDataFolder", SteamUser.GetUserDataFolder(out savePath, 4096));
			App.SavePath = savePath;
#else
			if (App.Debug)
			{
				// Save to Debug directory
				App.SavePath = "../Saves".Replace('/', Path.DirectorySeparatorChar);
			}
			else
			{
				// Save to the system save directory provided by SDL2
				var savePath = SDL.SDL_GetPrefPath(App.Info.DeveloperName, App.Info.Title);
				SDLUtils.CheckResult("SDL_GetPrefPath", savePath);
				App.SavePath = savePath.Replace(App.Info.DeveloperName + Path.DirectorySeparatorChar, "");
				Directory.CreateDirectory(App.SavePath);
			}
#endif
		}

		private void InitAssetPath()
		{
			if (App.Debug)
			{
				// In debug, use the assets folder in the solution
				App.AssetPath = "../../assets";
			}
			else
			{
				if (App.PlatformID == PlatformID.MacOS)
				{
					// On OSX, use a local path to avoid ambiguity between .app and .exe location
					App.AssetPath = "assets";
				}
				else
				{
					// Normally, use the assets folder in the .exe directory
					string basePath = SDL.SDL_GetBasePath();
					App.AssetPath = Path.Combine(basePath, "assets");
				}
			}
			if (!Directory.Exists(App.AssetPath))
			{
				throw new IOException("Could not locate directory " + App.AssetPath);
			}
		}

		private void InitNetwork()
		{
			// Create network
#if STEAM
			m_network = new SteamworksNetwork();
#else
			m_network = new NullNetwork();
#endif
		}

		public static void Run(IGame game, string[] args)
		{
			App.Assert(App.Platform == null);
            var arguments = new ProgramArguments(args);
            var headless = arguments.GetBool("headless");
            var platform = new SDL2Platform(headless);
			try
			{
				// Init the app
				App.Platform = platform;
				App.Init(game.GetInfo(), arguments);

				// Init the platform
				if (!platform.InitSteamworks())
				{
					return; // Authentication failed
				}
				platform.InitSDL2();

				// Setup directories
				platform.InitSavePath();
				platform.InitAssetPath();

				// Start logging (now that we know where to save the logs)
				App.StartLogging();

	            // Load SDL controller database
	            var gameControllerDBPath = Path.Combine(App.AssetPath, "gamecontrollerdb.txt");
	            if (File.Exists(gameControllerDBPath))
	            {
	                try
	                {
						SDLUtils.CheckResult("SDL_GameControllerAddMappingsFromFile", SDL.SDL_GameControllerAddMappingsFromFile(gameControllerDBPath));
	                    App.Log("Loaded gamepad mappings from gamecontrollerdb.txt");
	                }
	                catch (SDLException)
	                {
						App.LogError("Failed to load gamepad mappings from gamecontrollerdb.txt");
	                }
	            }
	            else
	            {
	                App.LogError("gamecontrollerdb.txt not found");
	            }

				// Init the network
				platform.InitNetwork();

				// Init the game
	            game.Init();

				// Main loop
				uint lastFrameStart = SDL.SDL_GetTicks();
				while (!game.Over)
				{
					// Get time
					uint frameStart = SDL.SDL_GetTicks();
					uint delta = frameStart - lastFrameStart;
					float dt = Mathf.Max((float)delta / 1000.0f, 0.0f);
					if (dt > 0.0f)
					{
						App.FPS = Mathf.Lerp(App.FPS, (1.0f / dt), 0.2f);
					}

					// Handle SDL events
					SDL.SDL_Event e;
					while (!game.Over && SDL.SDL_PollEvent(out e) != 0)
					{
						switch (e.type)
						{
							case SDL.SDL_EventType.SDL_QUIT:
								{
									game.Over = true;
									break;
								}
							default:
								{
									platform.HandleEvent(ref e);
									break;
								}
						}
					}

#if STEAM
                    // Handle Steamworks events
                    if (!game.Over)
                    {
                        SteamAPI.RunCallbacks();
                    }
#endif

					// Update windows and input
					if(!game.Over)
					{
						platform.UpdateWindows(dt);
					}

					// Update the game
					if (!game.Over)
					{
						game.Update(dt);
					}

					// Render the game
                    if (!game.Over && !headless)
					{
						game.Render();
					}

					// Sleep if necessary
					if (!game.Over)
					{
						uint minFrameTime = 1000 / App.MAX_FPS;
						uint frameTime = (SDL.SDL_GetTicks() - frameStart);
						if (frameTime < minFrameTime)
						{
							SDL.SDL_Delay(minFrameTime - frameTime);
						}
					}

					// Update timer for next frame
					lastFrameStart = frameStart;
				}
			}
#if !DEBUG
			catch (Exception e)
			{
				App.HandleError(e);
			}
#endif
			finally
			{
	            // Shutdown the game
                game.Shutdown();

				// Shutdown the platform
				platform.Dispose();

				// Shutdown the app
				App.Shutdown();
				App.Platform = null;
			}
		}

		private void HandleEvent(ref SDL.SDL_Event e)
		{
			for (int i = 0; i < m_windows.Count; ++i)
			{
				var window = m_windows[i];
				window.HandleEvent(ref e);
			}
		}
	}
}
#endif
