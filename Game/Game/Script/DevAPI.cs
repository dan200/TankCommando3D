using Dan200.Core.Assets;
using Dan200.Core.Input;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.GUI;
using System;
using System.Text;
using Dan200.Core.Level;
using System.IO;
using Dan200.Core.Async;
using Dan200.Core.Render;

namespace Dan200.Game.Script
{
    internal class DevAPI : LuaAPI
    {
		private LevelState m_state;

        public DevAPI(LevelState state) : base("dev")
        {
			m_state = state;
        }

        [LuaMethod]
        public LuaArgs screenshot(in LuaArgs args)
        {
			string savePath;
			if (!args.IsNil(0))
			{
				savePath = args.GetString(0);
				if (Path.GetExtension(savePath) != ".png")
				{
					savePath = savePath + ".png";
				}
				savePath = Path.Combine("screenshots", savePath);
			}
			else
			{
				savePath = Path.Combine("screenshots", System.DateTime.Now.ToString("s").Replace(":", "-") + ".png");
			}
			   
			var promise = m_state.Game.QueueScreenshot();
			m_state.Game.QueueTaskAfterPromise(promise, delegate(Promise<Bitmap> result) {
				try
				{
                    var bitmap = result.Complete();
					var fullPath = Path.Combine(App.SavePath, savePath);
					try
					{
						Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        bitmap.Save(fullPath);
						App.Log("Screenshot saved to {0}", fullPath);
					}
					catch (IOException e)
					{
						App.LogError("Failed to save screenshot: {0}", e.Message);
					}
				}
				catch(Exception e)
				{
					App.LogError("Failed to take screenshot: {0}", e.Message);
				}
			} );

			return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs reloadAssets(in LuaArgs args)
        {
            Assets.ReloadAll();
            return LuaArgs.Empty;
        }

		[LuaMethod]
		public LuaArgs setDebugDraw(in LuaArgs args)
		{
			var channel = args.GetString(0);
			var on = args.GetBool(1);

			var systemID = ComponentRegistry.GetSystemID(channel);
			if (systemID >= 0)
			{
				m_state.DebugDrawSystems[systemID] = on;
			}

			var componentID = ComponentRegistry.GetComponentID(channel);
			if (componentID >= 0)
			{
				m_state.DebugDrawComponents[componentID] = on;
			}

			return LuaArgs.Empty;
		}

		[LuaMethod]
		public LuaArgs toggleDebugDraw(in LuaArgs args)
		{
			var channel = args.GetString(0);

			var systemID = ComponentRegistry.GetSystemID(channel);
			if (systemID >= 0)
			{
				m_state.DebugDrawSystems[systemID] = !m_state.DebugDrawSystems[systemID];
			}

			var componentID = ComponentRegistry.GetComponentID(channel);
			if (componentID >= 0)
			{
				m_state.DebugDrawComponents[componentID] = !m_state.DebugDrawComponents[componentID];
			}

			return LuaArgs.Empty;
		}

		[LuaMethod]
		public LuaArgs setGUIVisible(in LuaArgs args)
		{
			var visible = args.GetBool(0);
			m_state.Game.RenderUI = visible;
			return LuaArgs.Empty;
		}

		[LuaMethod]
		public LuaArgs getGUIVisible(in LuaArgs args)
		{
			return new LuaArgs(m_state.Game.RenderUI);
		}

        [LuaMethod]
        public LuaArgs setDebugCamera(in LuaArgs args)
        {
            var active = args.GetBool(0);
            m_state.Game.UseDebugCamera = active;
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getDebugCamera(in LuaArgs args)
        {
            return new LuaArgs(m_state.Game.UseDebugCamera);
        }
    }
}

