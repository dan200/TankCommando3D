using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.GUI;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Game.GUI;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    internal class LoadState : GameState
    {
        private Image m_backdrop;
        private Label m_text;

        private enum LoadStage
        {
            Startup = 0,
            LoadingNewAssets,
            UnloadingOldAssets,
            LoadingAtlases,
            LoadingAnims,
            Finalising,
            Finished
        }

        private LoadStage m_stage;
        private Func<GameState> m_nextStateFunc;
        private float m_timeInStage;

        private CompoundPromise m_loadPromise;
        private int m_initialAssets;
        private DateTime m_initialTime;

        public LoadState(Game game, Func<GameState> nextState) : base(game)
        {
            m_stage = LoadStage.Startup;
            m_nextStateFunc = nextState;

            m_backdrop = new Image(LowResUI.Blank, Game.Screen.Width, Game.Screen.Height);
            m_backdrop.Anchor = Anchor.Top | Anchor.Left | Anchor.Bottom | Anchor.Right;
            m_backdrop.LocalPosition = Vector2.Zero;

			m_text = new Label(LowResUI.NumbersFont, 64, "H0", Colour.White, TextAlignment.Center, game.Screen.Width);
			m_text.Anchor = Anchor.Left | Anchor.Right;
            m_text.LocalPosition = new Vector2(0.0f, -0.5f * m_text.Height);

            m_timeInStage = 0.0f;
        }

		public override void Enter(GameState previous)
        {
            Game.Sky = null;
            Game.Screen.Elements.Add(m_backdrop);
            Game.Screen.Elements.Add(m_text);
        }

		public override void Leave(GameState next)
		{
			Game.Screen.Elements.Remove(m_backdrop);
			m_backdrop.Dispose();

			Game.Screen.Elements.Remove(m_text);
			m_text.Dispose();
		}

		public override void Update(float dt)
        {
            // Animate
            if (m_loadPromise != null)
            {
                int percent = (m_loadPromise.CurrentProgress * 100) / m_loadPromise.TotalProgress;
                m_text.Text = string.Format("H{0}", percent);
            }

            // Advance state
            m_timeInStage += dt;
            switch (m_stage)
            {
                case LoadStage.LoadingNewAssets:
                    {
                        // Load all assets
                        if (m_loadPromise == null)
                        {
                            m_initialAssets = Assets.Count;
                            m_initialTime = DateTime.UtcNow;
                            m_loadPromise = Assets.LoadAllAsync(Game.ThreadPool.IOTasks);
                        }
                        Assets.CompleteAsyncLoads(new TimeSpan(50 * TimeSpan.TicksPerMillisecond));
						if (m_loadPromise.IsReady)
                        {
                            App.Log("Loaded {0} assets in {1} seconds", Assets.Count - m_initialAssets, (DateTime.UtcNow - m_initialTime).TotalSeconds);
                            NextStage();
                        }
                        break;
                    }
                case LoadStage.UnloadingOldAssets:
                    {
                        // Unload some assets
                        var loaded = Assets.Count;
                        Assets.UnloadUnsourced();
                        if (loaded > Assets.Count)
                        {
                            App.Log("Unloaded {0} assets", loaded - Assets.Count);
                        }
                        NextStage();
                        break;
                    }
                case LoadStage.LoadingAtlases:
                    {
                        NextStage();
                        break;
                    }
                case LoadStage.LoadingAnims:
                    {
                        LuaAnimation.ReloadAll();
                        NextStage();
                        break;
                    }
                case LoadStage.Finalising:
                    {
                        Game.SelectLanguage();
                        GC.Collect();
                        NextStage();
                        break;
                    }
                default:
                    {
                        NextStage();
                        break;
                    }
            }
        }

		public override void OnConsoleCommand(string command)
		{
		}

		public override void PopulateCamera(View view)
		{
		}

		public override void Draw(IRenderer renderer, View view)
        {
        }

        private void NextStage()
        {
            if (m_stage < LoadStage.Finished)
            {
                m_stage++;
                m_timeInStage = 0.0f;
                if (m_stage == LoadStage.Finished)
                {
                    var nextState = m_nextStateFunc.Invoke();
                    Game.QueueState(nextState);
                }
            }
        }
    }
}
