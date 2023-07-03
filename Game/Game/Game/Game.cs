
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Audio;
using Dan200.Core.Audio.Null;
using Dan200.Core.Audio.OpenAL;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Network;
using Dan200.Core.Render;
using Dan200.Core.Window;
using Dan200.Core.Math;
using Dan200.Core.Level;
using Dan200.Core.Components;
using Dan200.Core.Util;
using Dan200.Core.Systems;
using Dan200.Core.Physics;
using Dan200.Core.Script;

using Dan200.Game.GUI;
using Dan200.Game.Level;
using Dan200.Game.Components;
using Dan200.Game.Components.Player;
using Dan200.Game.User;
using Dan200.Core.Render.OpenGL;
using Dan200.Game.Systems;

namespace Dan200.Game.Game
{
    internal class Game : IGame
    {
        public const float TARGET_RESOLUTION_Y = 360.0f;
        public const float TARGET_SCREEN_HEIGHT = 1080.0f;

        private User.User m_user;
        private IWindow m_window;

        private IAudio m_audio;
        private INetwork m_network;
        private Language m_language;
        private GameThreadPool m_threadPool;

        private ViewCollection m_views;

        private DebugCameraController m_debugCameraController;
        private GUI.Console m_console;

        private SkyInstance m_sky;
        private Screen m_screen;

        private IRenderer m_renderer;
        private RenderTexture m_worldRenderTexture;
        private RenderTexture m_upscaleRenderTexture;
        private IRenderGeometry<ScreenVertex> m_fullScreenQuad;
        private BlitEffectHelper m_blitEffect;
        private PostEffectHelper m_postEffect;
        private PostEffectHelper m_postEffectFXAA;

        private StateMachine<GameState> m_stateMachine;

        private class ScreenshotRequest
        {
            public int CustomWidth = 0; 
            public int CustomHeight = 0;
            public Promise<Bitmap> Promise = null;
        }
        private List<ScreenshotRequest> m_screenshotRequests;

        private struct PromiseTask
        {
            public Func<bool> TryFinish;
        }
        private List<PromiseTask> m_promiseTasks;

        private bool m_over;

        public IWindow Window
        {
            get
            {
                return m_window;
            }
        }

        public IAudio Audio
        {
            get
            {
                return m_audio;
            }
        }

        public SkyInstance Sky
        {
            get
            {
                return m_sky;
            }
            set
            {
                m_sky = value;
            }
        }

        public Screen Screen
        {
            get
            {
                return m_screen;
            }
        }

        public User.User User
        {
            get
            {
                return m_user;
            }
        }

        public INetwork Network
        {
            get
            {
                return m_network;
            }
        }

        public Language Language
        {
            get
            {
                return m_language;
            }
            set
            {
                m_language = value;
                if (m_screen != null)
                {
                    m_screen.Language = m_language;
                }
            }
        }

        public GameThreadPool ThreadPool
        {
            get
            {
                return m_threadPool;
            }
        }

        public DeviceCollection InputDevices
        {
            get
            {
                return m_window.InputDevices;
            }
        }

        internal class ViewCollection : IEnumerable<View>
        {
            private List<View> m_views;

            public int Count
            {
                get
                {
                    return m_views.Count;
                }
            }

            public View this[int index]
            {
                get
                {
                    return m_views[index];
                }
            }

            public ViewCollection()
            {
                m_views = new List<View>();
            }

            public void Add(View view)
            {
                if (!m_views.Contains(view))
                {
                    m_views.Add(view);
                }
            }

            public void Remove(View view)
            {
                m_views.Remove(view);
            }

            public List<View>.Enumerator GetEnumerator()
            {
                return m_views.GetEnumerator();
            }

            IEnumerator<View> IEnumerable<View>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public ViewCollection Views
        {
            get
            {
                return m_views;
            }
        }

        public View MainView
        {
            get
            {
                var Enumerator = m_views.GetEnumerator();
                if (Enumerator.MoveNext())
                {
                    return Enumerator.Current;
                }
                return null;
            }
        }

        public bool RenderUI;
        public bool UseDebugCamera;

        public bool Over
        {
            get
            {
                return m_over;
            }
            set
            {
                m_over = value;
            }
        }

        public GameState CurrentState
        {
            get
            {
                return m_stateMachine.CurrentState;
            }
        }

        public GUI.Console Console
        {
            get
            {
                return m_console;
            }
        }

        public RenderStats RenderStats
        {
            get
            {
                return m_renderer.RenderStats;
            }
        }

        private static void RegisterAssetTypes()
        {
            // Clear
            Assets.UnregisterAllTypes();

            // Engine
            Assets.RegisterType<BinaryAsset>("bin");
            Assets.RegisterType<Effect>("effect");
            Assets.RegisterType<EntityPrefab>("entity");
            Assets.RegisterType<Font>("fnt");
            Assets.RegisterType<Language>("lang");
            Assets.RegisterType<LevelData>("level");
            Assets.RegisterType<LuaScript>("lua");
            Assets.RegisterType<Map>("map");
            Assets.RegisterType<MaterialFile>("mtl");
            Assets.RegisterType<Model>("obj");
            Assets.RegisterType<ParticleStyle>("pfx");
            Assets.RegisterType<PhysicsMaterial>("physicsMaterial");
            Assets.RegisterType<TextAsset>("txt");

            bool headless = App.Arguments.GetBool("headless");
            if (!headless)
            {
                Assets.RegisterType<OpenGLFragmentShader>("frag");
                Assets.RegisterType<OpenGLVertexShader>("vert");
                Assets.RegisterType<OpenGLShaderInclude>("glsl");
                Assets.RegisterType<OpenGLTexture>("png");
            }

            if (headless || App.Arguments.GetBool("nosound"))
            {
                Assets.RegisterType<NullMusic>("ogg");
                Assets.RegisterType<NullSound>("wav");
            }
            else
            {
                Assets.RegisterType<OpenALMusic>("ogg");
                Assets.RegisterType<OpenALSound>("wav");
            }

            // Game
            Assets.RegisterType<Sky>("sky");
        }

        private static void RegisterComponents()
        {
            ComponentRegistry.Reset();

            // Core
            CoreSystems.Register();
            CoreComponents.Register();

            // Game
            GameSystems.Register();
            GameComponents.Register();

            ComponentRegistry.Finalise();
        }

        private User.User LoadUser()
        {
            var user = new User.User(Network);
            if (App.Arguments.GetBool("defaults"))
            {
                App.Log("Using default settings.");
                user.Settings.Load(Path.Combine(App.AssetPath, "default_settings.txt"));
                user.Settings.Save();
            }
            if (App.Arguments.GetBool("reset_progress"))
            {
                App.Log("Resetting progress.");
                user.Progress.Reset();
                user.Progress.Save();
            }
            return user;
        }

        public Game()
        {
        }

        public GameInfo GetInfo()
        {
            var info = new GameInfo();
            info.Title = "Tank Commando 3D";
            info.Website = "http://www.dan200.net";
            info.DeveloperName = "Daniel Ratcliffe";
            var version = typeof(Game).Assembly.GetName().Version;
            info.Version = new Version(version.Major, version.Minor, version.Build);
            info.SteamAppID = 0;
            info.Achievements = Achievements.ALL_ACHIEVEMENTS;
            info.Statistics = Statistics.ALL_STATISTICS;
            return info;
        }

        public void Init()
        {
            m_screenshotRequests = new List<ScreenshotRequest>();
            m_promiseTasks = new List<PromiseTask>();

            m_over = false;
            RenderUI = true;
            UseDebugCamera = false;

            // Init threads
            m_threadPool = new GameThreadPool(1, Environment.ProcessorCount);

            // Init network
            m_network = App.Platform.Network;
            if (m_network.SupportsAchievements)
            {
                m_network.SetAchievementCorner(AchievementCorner.TopRight);
            }

            // Init user
            m_user = LoadUser();

            // Init window
            var title = App.Info.Title + " " + App.Info.Version;
            if (App.Debug && App.Steam)
            {
                title += " (Steam Debug build)";
            }
            else if (App.Debug)
            {
                title += " (Debug build)";
            }
            m_window = App.Platform.CreateWindow(
                title,
                m_user.Settings.WindowWidth,
                m_user.Settings.WindowHeight,
                m_user.Settings.Fullscreen,
                m_user.Settings.WindowMaximised,
                m_user.Settings.VSync
            );
            {
                var icon = new Bitmap(Path.Combine(App.AssetPath, "icon.png"));
                m_window.SetIcon(icon);
            }
            m_window.OnClosed += delegate
            {
                Over = true;
            };
            m_window.OnResized += delegate
            {
                Resize();
                if (!m_window.Fullscreen)
                {
                    if (m_window.Maximised)
                    {
                        m_user.Settings.WindowMaximised = true;
                    }
                    else
                    {
                        m_user.Settings.WindowMaximised = false;
                        m_user.Settings.WindowWidth = m_window.Width;
                        m_user.Settings.WindowHeight = m_window.Height;
                    }
                    m_user.Settings.Save();
                }
            };
            App.Log("Display type: {0}", m_window.DisplayType);

            // Create renderer
            m_renderer = m_window.Renderer;
            m_renderer.MakeCurrent();

            // Init audio
            if (App.Arguments.GetBool("nosound"))
            {
                m_audio = new NullAudio();
            }
            else
            {
                m_audio = new OpenALAudio();
            }
            foreach(var category in EnumConverter.GetValues<AudioCategory>())
            {
                m_audio.SetVolume(category, m_user.Settings.Volume[category]);
            }

            // Register asset types
            RegisterAssetTypes();

            // Register component types
            RegisterComponents();

            // Load early assets
            var earlyAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "early"));
            var earlyAssets = new AssetSource("early", earlyAssetFileStore);
            Assets.RemoveAllSources();
            Assets.AddSource(earlyAssets);
            Assets.LoadAll();

            // Find mods
            Mods.Refresh(Network);

            // Load language
            SelectLanguage();

            // Load debug stuff
            m_debugCameraController = new DebugCameraController(this);

            // Create views
            m_views = new ViewCollection();

            // Create render stuff
            var width = (User.Settings.FullscreenWidth > 0) ? Math.Min(Window.Width, User.Settings.FullscreenWidth) : Window.Width;
            var height = (User.Settings.FullscreenHeight > 0) ? Math.Min(Window.Height, User.Settings.FullscreenHeight) : Window.Height;
            var aamode = User.Settings.AntiAliasingMode;

            var downscale = Math.Max((int)Mathf.Round(height / TARGET_RESOLUTION_Y), 1);
            m_worldRenderTexture = new RenderTexture(width / downscale, height / downscale, ColourSpace.Linear, false);
            m_upscaleRenderTexture = new RenderTexture(width, height, ColourSpace.Linear, false);
            m_fullScreenQuad = CreateFullscreenQuad();
            m_blitEffect = new BlitEffectHelper(m_renderer);
            m_postEffect = new PostEffectHelper(m_renderer, ShaderDefines.Empty);
            var fxaaDefines = new ShaderDefines();
            fxaaDefines.Define("FXAA");
            m_postEffectFXAA = new PostEffectHelper(m_renderer, fxaaDefines);

            // Create main view
            var aspectRatio = (float)Window.Width / (float)Window.Height;

            var view = new View(null, Quad.UnitSquare);
            view.Camera.AspectRatio = aspectRatio * view.Viewport.AspectRatio;
            view.PostProcessSettings.Gamma = User.Settings.Gamma;
            m_views.Add(view);

            // Create debug renderer
            App.DebugDraw = new DebugDraw( m_renderer );

            // Create screen
            var screenScale = (float)m_worldRenderTexture.Height / TARGET_RESOLUTION_Y;
            m_screen = new Screen(
                InputDevices, Language, m_window, m_audio,
                aspectRatio * TARGET_SCREEN_HEIGHT * screenScale, TARGET_SCREEN_HEIGHT * screenScale
            );

            m_console = new GUI.Console(this);
            m_console.ZOrder = (int)ZOrder.Console;
            m_console.Size = new Vector2(Screen.Width, 0.5f * Screen.Height);
            m_console.Anchor = Anchor.Top | Anchor.Left | Anchor.Right;
            m_console.OnCommand += delegate(GUI.Console console, ConsoleCommandEventArgs args) {
                m_stateMachine.CurrentState.OnConsoleCommand(args.Command);                
            };
            m_screen.Elements.Add(m_console);

            // Add the rest of the asset sources:
            // Add the base assets
            var baseAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "base"));
            var baseAssets = new AssetSource("base", baseAssetFileStore);
            Assets.AddSource(baseAssets);

            // Add the main assets
            var mainAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "main"));
            var mainAssets = new AssetSource("main", mainAssetFileStore);
            Assets.AddSource(mainAssets);

            // Add autoload mod assets
            foreach (Mod mod in Mods.AllMods)
            {
                if (mod.AutoLoad)
                {
                    Assets.AddSource(mod.Assets);
                    mod.Loaded = true;
                }
            }

            // Create initial loading state
            m_stateMachine = new StateMachine<GameState>(CreateInitialState());
        }

        public void Shutdown()
        {
            // Dispose state
            m_stateMachine.Shutdown();

            // Stop threads
            m_threadPool.Dispose();

            // Dispose things
            m_worldRenderTexture.Dispose();
            m_upscaleRenderTexture.Dispose();
            m_fullScreenQuad.Dispose();
            m_blitEffect.Dispose();
            m_postEffect.Dispose();
            m_postEffectFXAA.Dispose();
            m_screen.Dispose();

            // Shutdown animation
            LuaAnimation.UnloadAll();

            // Shutdown debug draw
            App.DebugDraw.Dispose();
            App.DebugDraw = null;

            // Unload assets
            Assets.UnloadAll();

            // Dispose video and audio (must be done after asset unload)
            m_audio.Dispose();
        }

        public void SelectLanguage()
        {
            // Choose language
            var languageCode = User.Settings.Language;
            if (User.Settings.Language == "system")
            {
                languageCode = App.Platform.SystemLanguage;
            }
            var newLanguage = Language.GetMostSimilarTo(languageCode);
            if (m_language == null || m_language.Code != newLanguage.Code)
            {
                App.Log("Using language {0} ({1})", newLanguage.Code, newLanguage.EnglishName);
            }

            // Set language
            Language = newLanguage;
        }

        public void Resize()
        {
            m_renderer.MakeCurrent();

            var aspectRatio = (float)Window.Width / (float)Window.Height;
            foreach (var view in m_views)
            {
                if (view.Target == null)
                {
                    view.Camera.AspectRatio = aspectRatio * (view.Viewport.Width / view.Viewport.Height);
                }
            }

            App.Log("Window size is {0}x{1}", Window.Width, Window.Height);
            var width = (User.Settings.FullscreenWidth > 0) ?
                Math.Min(Window.Width, User.Settings.FullscreenWidth) :
                Window.Width;
            var height = (User.Settings.FullscreenHeight > 0) ?
                Math.Min(Window.Height, User.Settings.FullscreenHeight) :
                Window.Height;
            //int scale = User.Settings.AntiAliasingMode == AntiAliasingMode.SSAA ? 2 : 1;
            App.Log("Resolution changed to {0}x{1} (AA:{2})", width, height, User.Settings.AntiAliasingMode);
            var downscale = Math.Max((int)Mathf.Round(height / TARGET_RESOLUTION_Y), 1);
            m_worldRenderTexture.Resize(width / downscale, height / downscale);
            m_upscaleRenderTexture.Resize(width, height);

            var screenScale = (float)m_worldRenderTexture.Height / TARGET_RESOLUTION_Y;
            m_screen.Height = TARGET_SCREEN_HEIGHT * screenScale;
            m_screen.Width = aspectRatio * m_screen.Height;
        }

        public void QueueTaskAfterPromise(Promise promise, Action<Promise> onComplete)
        {
            var task = new PromiseTask();
            task.TryFinish = delegate ()
            {
                if (promise.IsReady)
                {
                    onComplete.Invoke(promise);
                    return true;
                }
                return false;
            };
            m_promiseTasks.Add(task);
        }

        public void QueueTaskAfterPromise<TResult>(Promise<TResult> promise, Action<Promise<TResult>> onComplete)
        {
            var task = new PromiseTask();
            task.TryFinish = delegate ()
            {
                if(promise.IsReady)
                {
                    onComplete.Invoke(promise);
                    return true;
                }
                return false;
            };
            m_promiseTasks.Add(task);
        }

        private IRenderGeometry<ScreenVertex> CreateFullscreenQuad()
        {
            var geometry = new Geometry<ScreenVertex>(Primitive.Triangles, 4, 6);
            geometry.AddVertex(new Vector2(-1.0f, -1.0f), new Vector2(0.0f, 0.0f), Colour.White);
            geometry.AddVertex(new Vector2(1.0f, -1.0f), new Vector2(1.0f, 0.0f), Colour.White);
            geometry.AddVertex(new Vector2(-1.0f, 1.0f), new Vector2(0.0f, 1.0f), Colour.White);
            geometry.AddVertex(new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f), Colour.White);
            geometry.AddIndex(0);
            geometry.AddIndex(1);
            geometry.AddIndex(2);
            geometry.AddIndex(3);
            geometry.AddIndex(2);
            geometry.AddIndex(1);
            return m_renderer.Upload(geometry);
        }

        private GameState CreateInitialState()
        {
            // Load into the level specified on the command line
            return new LoadState(this, delegate ()
            {
                // Editor startup
                var editorStartupLevel = App.Arguments.GetString("editor");
                if (editorStartupLevel != null)
                {
                    return new EditorState(this, editorStartupLevel, editorStartupLevel);
                }

                // Specific level startup
                var startupLevel = App.Arguments.GetString("level");
                if(startupLevel != null)
                {
                    return new InGameState(this, startupLevel);
                }

                // Normal startup
                return new InGameState(this, "levels/tankgame.level");
            });
        }

        public void QueueState(GameState newState)
        {
            m_stateMachine.QueueState(newState);
        }

        public void Update(float dt)
        {
            dt = Mathf.Min(dt, 0.1f);

            // Bind renderer
            m_renderer.MakeCurrent();

            // Start debug draw
            App.DebugDraw.BeginFrame();

            // Update tasks
            for (int i = m_promiseTasks.Count - 1; i >= 0; --i)
            {
                var task = m_promiseTasks[i];
                if (task.TryFinish())
                {
                    m_promiseTasks.UnorderedRemoveAt(i);
                }
            }

            // Update sound
            m_audio.Update(dt);

            // Toggle fullscreen
            var keyboard = InputDevices.Keyboard;
            if ((
                (keyboard.GetInput(Key.LeftAlt).Held || keyboard.GetInput(Key.RightAlt).Held) &&
                 keyboard.GetInput(Key.Return).Pressed
                ) ||
                (
                    App.PlatformID == Core.Platform.PlatformID.MacOS &&
                    (keyboard.GetInput(Key.LeftGUI).Held || keyboard.GetInput(Key.RightGUI).Held) &&
                     keyboard.GetInput(Key.W).Pressed
                ))
            {
                Window.Fullscreen = !Window.Fullscreen;
                User.Settings.Fullscreen = Window.Fullscreen;
                User.Settings.Save();
            }

            // Update screen
            m_screen.Update(dt);

            // Update state
            m_stateMachine.EnterQueuedState();
            m_stateMachine.CurrentState.Update(dt);
            foreach (var view in m_views)
            {
                m_stateMachine.CurrentState.PopulateCamera(view);
                view.Camera.UpdateMatrices();
            }

            // Update debug camera
            if (UseDebugCamera)
            {
                if (m_console.Visible)
                {
                    App.DebugDraw.DrawAxisMarker(MainView.Camera.Transform, 0.5f);
                }

                m_debugCameraController.Update(dt);
                m_debugCameraController.Populate(MainView.Camera);
                MainView.Camera.UpdateMatrices();
            }
            else
            {
                m_debugCameraController.Transform = MainView.Camera.Transform;
                m_debugCameraController.FOV = MainView.Camera.FOV;
            }

            // Update sound
            m_audio.ListenerTransform = MainView.Camera.Transform;
            m_audio.ListenerVelocity = MainView.Camera.Velocity;
        }

        private void RenderView(View view, RenderTexture destination)
        {
            int dw = destination.Width;
            int dh = destination.Height;
            int x = (int)Mathf.Round(view.Viewport.X * dw);
            int y = (int)Mathf.Round(view.Viewport.Y * dh);
            int w = (int)Mathf.Round(view.Viewport.Width * dw);
            int h = (int)Mathf.Round(view.Viewport.Height * dh);

            // Set viewport
            m_renderer.Viewport = new Rect(x, y, w, h);

            if (m_sky != null)
            {
                // Draw the sky
                var backgroundImage = m_sky.Sky.BackgroundImage;
                if (backgroundImage != null)
                {
                    // Draw background image
                    m_renderer.DepthWrite = false;
                    try
                    {
                        m_blitEffect.Texture = Texture.Get(backgroundImage, true);
                        m_renderer.CurrentEffect = m_blitEffect.Instance;
                        m_renderer.Draw(m_fullScreenQuad);
                    }
                    finally
                    {
                        m_renderer.DepthWrite = true;
                    }
                }
                else
                {
                    // Draw background colour
                    var bgColour = m_sky.BackgroundColour;
                    if (bgColour != ColourF.Black)
                    {
                        m_renderer.Clear(bgColour.ToLinear());
                    }
                }
            }

            // Draw the states
            m_stateMachine.CurrentState.Draw(m_renderer, view);
        }

        private void PostProcessView(View view, RenderTexture source, RenderTexture destination)
        {
            int dw = (destination != null) ? destination.Width : m_window.Width;
            int dh = (destination != null) ? destination.Height : m_window.Height;
            int x = (int)Mathf.Round(view.Viewport.X * dw);
            int y = (int)Mathf.Round(view.Viewport.Y * dh);
            int w = (int)Mathf.Round(view.Viewport.Width * dw);
            int h = (int)Mathf.Round(view.Viewport.Height * dh);

            // Set viewport
            GL.Viewport(0, 0, dw, dh);
            GL.Scissor(x, dh - (y + h), w, h);

            // Draw world to screen
            var postEffect = (User.Settings.AntiAliasingMode == AntiAliasingMode.FXAA) ? m_postEffectFXAA : m_postEffect;
            postEffect.Texture = source;
            postEffect.Gamma = view.PostProcessSettings.Gamma;
            postEffect.Saturation = view.PostProcessSettings.Saturation;
            m_renderer.CurrentEffect = postEffect.Instance;
            m_renderer.Draw(m_fullScreenQuad);
        }

        public void Render()
        {
            bool upscaleNeeded =
                (m_window.Width != m_upscaleRenderTexture.Width) ||
                (m_window.Height != m_upscaleRenderTexture.Height);

            // Bind graphics
            m_renderer.MakeCurrent();

            // Clear screen
            m_renderer.Target = null;
            m_renderer.Clear(ColourF.Black);

            // --------------------
            // DRAW OFFSCREEN VIEWS
            // --------------------

            unsafe
            {
                var rendered = stackalloc bool[m_views.Count];
                int i = 0;
                while (true)
                {
                    // Find the next unrendered view
                    RenderTexture target = null;
                    while (i < m_views.Count)
                    {
                        var view = m_views[i];
                        if (view.Target != null && !rendered[i])
                        {
                            target = view.Target;
                            break;
                        }
                        else
                        {
                            i++;
                        }
                    }

                    // If we didn't find a target, all views are rendered
                    if (target == null)
                    {
                        break;
                    }

                    // Bind the target
                    m_renderer.Target = target;
                    try
                    {
                        // Clear the target
                        m_renderer.Clear(ColourF.Black);

                        // Render the view, and all subsequent views with the same target
                        for (int j = i; j < m_views.Count; ++j)
                        {
                            var view = m_views[j];
                            if (view.Target == target)
                            {
                                RenderView(view, target);
                                rendered[j] = true;
                            }
                        }
                    }
                    finally
                    {
                        // Unbind he target
                        m_renderer.Target = null;
                    }
                }
            }

            // ---------------
            // DRAW MAIN VIEWS
            // ---------------

            // Bind the main target
            m_renderer.Target = m_worldRenderTexture;
            try
            {
                // Clear the main target
                m_renderer.Clear(ColourF.Black);

                // Render the main views
                foreach (var view in m_views)
                {
                    if (view.Target == null)
                    {
                        RenderView(view, m_worldRenderTexture);
                    }
                }
            }
            finally
            {
                // Unbind the main target
                m_renderer.Target = null;
            }

            if (upscaleNeeded)
            {
                // Bind the upscale texture
                m_renderer.Target = m_upscaleRenderTexture;
            }
            try
            {
                // Clear the screen/upscale texture
                m_renderer.Clear(ColourF.Black);

                // ----------------------
                // POSTPROCESS MAIN VIEWS
                //-----------------------
                var destination = upscaleNeeded ? m_upscaleRenderTexture : null;
                foreach (var view in m_views)
                {
                    if (view.Target == null)
                    {
                        PostProcessView(view, m_worldRenderTexture, destination);
                    }
                }
                m_renderer.ResetViewport();

                // -------------------
                // DRAW DEBUG GRAPHICS
                // -------------------
                m_renderer.DepthTest = false;
                try
                {
                    App.DebugDraw.EndFrame();
                    App.DebugDraw.Draw(m_renderer, MainView);
                }
                finally
                {
                    m_renderer.DepthTest = true;
                }

                // --------
                // DRAW GUI
                // --------

                if (RenderUI)
                {
                    // Draw GUI
                    m_screen.Draw(m_renderer);
                }
            }
            finally
            {
                if (upscaleNeeded)
                {
                    // Unbind upscale texture
                    m_renderer.Target = null;
                }
            }

            if (upscaleNeeded)
            {
                // -------
                // UPSCALE
                // -------

                // Draw the upscale texture to the screen
                m_blitEffect.Texture = m_upscaleRenderTexture;
                m_renderer.CurrentEffect = m_blitEffect.Instance;
                m_renderer.Draw(m_fullScreenQuad);
            }

            // ------
            // FINISH
            // ------

            if (m_screenshotRequests.Count > 0)
            {
                // Take screenshot
                TakeScreenshots();
            }

            // Present graphics
            m_renderer.Present();
        }

        public Promise<Bitmap> QueueScreenshot(int customWidth = 0, int customHeight = 0)
        {
            App.Assert((customWidth == 0 && customHeight == 0) || (customWidth > 0 && customHeight > 0));
            var request = new ScreenshotRequest();
            request.CustomWidth = customWidth;
            request.CustomHeight = customHeight;
            request.Promise = new Promise<Bitmap>();
            m_screenshotRequests.Add(request);
            return request.Promise;
        }

        private void TakeScreenshots()
        {
            // Capture image
            var bitmap = m_renderer.Capture();
            try
            {
                foreach (var request in m_screenshotRequests)
                {
                    // Process the bitmaps
                    var resultBitmap = bitmap;
                    if (request.CustomWidth > 0 && request.CustomWidth > 0)
                    {
                        resultBitmap = bitmap.Resize(request.CustomWidth, request.CustomWidth, true, true);
                    }
                    else
                    {
                        resultBitmap = bitmap;
                    }

                    // Complete the promise
                    request.Promise.Succeed(resultBitmap);
                }
            }
            finally
            {
                m_screenshotRequests.Clear();
            }
        }
    }
}
