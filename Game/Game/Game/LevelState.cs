using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.Geometry;
using Dan200.Core.Input;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Multiplayer;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Game.Level;
using Dan200.Game.Script;
using System;
using System.IO;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Util;
using System.Collections.Generic;
using Dan200.Core.Components;
using System.Text;
using Dan200.Core.Script;
using Dan200.Core.Interfaces;
using Dan200.Game.Components.Editor;
using Dan200.Core.Components.Core;

namespace Dan200.Game.Game
{
    internal abstract class LevelState : GameState
    {
        private Core.Level.Level m_level;
        private Entity m_rootEntity;
		private LevelRenderer m_levelRenderer;
        private ICameraProvider m_cameraProvider;

        public Core.Level.Level Level
        {
            get
            {
                return m_level;
            }
        }

        public Entity RootEntity
        {
            get
            {
                return m_rootEntity;
            }
        }

        public ICameraProvider CameraProvider
        {
            get
            {
                return m_cameraProvider;
            }
            protected set
            {
                m_cameraProvider = value;
            }
        }

		public BitField DebugDrawComponents;
		public BitField DebugDrawSystems;

		private static LevelSaveData LoadSave(string path)
		{
			try
			{
				using (var stream = File.OpenRead(path))
				{
					return LevelSaveData.Load(stream);
				}
			}
			catch (IOException e)
			{
				App.LogError("Error loading {0}: {1}", path, e.Message);
				return null;
			}
		}

        protected void SetupEntityCreationInfoForEditor(EntityPrefab prefab, LuaTable properties, List<EntityCreationInfo> o_infos, bool makeEditable, int rootParentID=0)
        {
            // Setup the info normally
            int firstInfoIndex = o_infos.Count;
            prefab.SetupCreationInfo(Level, properties, o_infos, rootParentID);
            int count = o_infos.Count - firstInfoIndex;
            App.Assert(count > 0);

            // Strip out all non-editable components
            var editableComponents = ComponentRegistry.GetComponentsImplementingInterface<IEditable>();
            for (int i = firstInfoIndex; i < firstInfoIndex + count; ++i)
            {
                var info = o_infos[i];
                var nonEditableComponents = info.Components & ~editableComponents;
                foreach (var id in nonEditableComponents)
                {
                    info.RemoveComponent(id);
                }
                o_infos[i] = info;
            }

            if (makeEditable)
            {
                // Add editor component to the root
                var editorComponentID = ComponentRegistry.GetComponentID<EditorComponent>();
                var firstInfo = o_infos[firstInfoIndex];
                var editorComponentProperties = new LuaTable(2);
                editorComponentProperties["Prefab"] = prefab.Path;
                editorComponentProperties["Properties"] = properties;
                firstInfo.AddComponent(editorComponentID, editorComponentProperties);
                o_infos[firstInfoIndex] = firstInfo;
            }
        }

        public LevelState(Game game, string levelPath, LevelLoadFlags flags) : base(game)
        {
			// Create level
			App.Log("Loading level {0}", levelPath);
            var data = Assets.Get<LevelData>(levelPath);
			m_level = new Core.Level.Level(Side.Both);
			m_level.Data = data;
			m_level.InEditor = (flags & LevelLoadFlags.Editor) != 0;

            // Add systems
            AddSystems(m_level, null);

            // Setup entity info for the root
            var infos = new List<EntityCreationInfo>();
            var rootPrefab = EntityPrefab.Get("entities/level.entity");
            if (m_level.InEditor)
            {
                SetupEntityCreationInfoForEditor(rootPrefab, LuaTable.Empty, infos, false);
            }
            else
            {
                rootPrefab.SetupCreationInfo(m_level, LuaTable.Empty, infos);
            }

            // Setup entity info from the level data
            var rootID = infos[0].ID;
            foreach (var entityData in data.Entities)
			{
                var prefab = EntityPrefab.Get(entityData.Type);
                var properties = entityData.Properties;
                var firstInfoIdx = infos.Count;
                if (m_level.InEditor)
                {
                    SetupEntityCreationInfoForEditor(prefab, properties, infos, true, rootID);
                }
                else
                {
                    prefab.SetupCreationInfo(m_level, properties, infos, rootID);
                }
            }

            // Create the entities
            m_level.Entities.Create(infos);
            m_level.PromoteNewComponents();
            m_rootEntity = m_level.Entities.Lookup(rootID);
            App.Assert(m_rootEntity != null);

            // Create everything else
            m_levelRenderer = new LevelRenderer(game.Window.Renderer, m_level);
            m_cameraProvider = null;

			// Setup debug draw
			DebugDrawSystems = BitField.Empty;
			DebugDrawComponents = BitField.Empty;

            // Start the level script
            if (m_level.Data.ScriptPath != null && !m_level.InEditor)
            {
                var script = LuaScript.Get(m_level.Data.ScriptPath);
                m_level.GetSystem<ScriptSystem>().RunScript(script);
            }
        }

		public virtual void AddSystems(Core.Level.Level level, LevelSaveData save)
		{
            level.AddSystem(new AudioSystem(Game.Audio), save);
            level.AddSystem(new GUISystem(Game.Screen, Game.MainView.Camera), save);
            level.AddSystem(new LightingSystem(), save);
            level.AddSystem(new NameSystem(), save);

            var script = level.AddSystem(new ScriptSystem(), save);
            script.AddAPI(new DevAPI(this));
            script.AddAPI(new GameAPI(this));
            script.AddAPI(new ConsoleAPI(this));

		}

		protected virtual string GetMusicPath(GameState previous)
        {
            return m_level.Data.MusicPath;
        }

		public override void Enter(GameState previous)
        {
            // Transfer state
			if (previous is LevelState)
			{
				var level = (LevelState)previous;
				DebugDrawSystems = level.DebugDrawSystems;
				DebugDrawComponents = level.DebugDrawComponents;
			}

            // Choose the sky
            if (m_level.Data.SkyPath != null)
            {
                Game.Sky = Sky.Get(m_level.Data.SkyPath);
            }
            else
            {
                Game.Sky = null;
            }
        }

        public override void Update(float dt)
        {
            CommonUpdate(dt);
        }

        public override void PopulateCamera(View view)
        {
            if (m_cameraProvider != null)
            {
                m_cameraProvider.Populate(view.Camera);
            }
        }

		public override void Leave(GameState next)
        {
			m_levelRenderer.Dispose();
            m_level.Dispose();
        }

		public override void OnConsoleCommand(string command)
		{
			// Parse the command
			var script = Level.GetSystem<ScriptSystem>();
			LuaFunction function;
			try
			{
				function = script.LoadString("return " + command, "=console");
			}
			catch (LuaError)
			{
				try
				{
					function = script.LoadString(command, "=console");
				}
				catch (LuaError e)
				{
					App.LogError("{0}", e.Value.ToString());
					return;
				}					
			}

			// Execute the command
			try
			{
				var results = function.Call(LuaArgs.Empty);
				var output = new StringBuilder();
				if (results.Length > 0)
				{
					for (int i = 0; i < results.Length; ++i)
					{
						output.Append(results.ToString(i));
						if (i < results.Length - 1)
						{
							output.Append('\t');
						}
					}
					App.Log("{0}", output.ToString());
				}
			}
			catch (LuaError e)
			{
				App.LogError("{0}", e.Value.ToString());
			}		
		}

        private void CommonUpdate(float dt)
        {
            // Update level(s)
            m_level.PromoteNewComponents();
            m_level.Update(dt, Game.ThreadPool.WorkerTasks);
			m_level.DebugDraw(DebugDrawSystems, DebugDrawComponents);
        }

		public override void Draw(IRenderer renderer, View view)
        {
            // Setup level lighting
            if (Game.Sky != null)
            {
                var lighting = m_level.GetSystem<LightingSystem>();
                lighting.AmbientLight.Colour = Game.Sky.AmbientColour;
            }

            // Draw level
            m_levelRenderer.Draw(renderer, view, Game.User.Settings.EnableShadows);
        }

        protected void CutToState(GameState state)
        {
            Game.QueueState(state);
        }

        protected void LoadToState(Func<GameState> stateFunction)
        {
            CutToState(new LoadState(Game, stateFunction));
        }

        protected Ray BuildRay(View view, Vector2 screenPosition, float length)
        {
            // Convert screen coords to camera space direction
            var viewCenter = view.Viewport.Center * Game.Screen.Size;
            var viewSize = view.Viewport.Size * Game.Screen.Size;

            float x = (screenPosition.X - viewCenter.X) / (0.5f * viewSize.X);
            float y = (screenPosition.Y - viewCenter.Y) / (0.5f * viewSize.Y);
            var dirCS = new Vector3(
                Mathf.Tan(0.5f * view.Camera.FOV) * (x * view.Camera.AspectRatio),
                -Mathf.Tan(0.5f * view.Camera.FOV) * y,
                1.0f
            ).Normalise();

            // Convert camera space direction to world space ray
            var cameraTrans = view.Camera.Transform;
            return new Ray(
                cameraTrans.Position,
                cameraTrans.ToWorldDir(dirCS),
                length
            );
        }
    }
}
