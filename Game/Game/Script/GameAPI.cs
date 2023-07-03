using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Core.Util;
using Dan200.Game.Game;
using System.Text;

namespace Dan200.Game.Script
{
    internal class GameAPI : LuaAPI
    {
        private LevelState m_state;

        public GameAPI(LevelState state) : base("game")
        {
            m_state = state;
        }

		[LuaMethod]
		public LuaArgs getTitle(in LuaArgs args)
		{
			return new LuaArgs(App.Info.Title);
		}

		[LuaMethod]
		public LuaArgs getVersion(in LuaArgs args)
		{
			return new LuaArgs(App.Info.Version.ToString());
		}

		[LuaMethod]
		public LuaArgs getPlatform(in LuaArgs args)
		{
			return new LuaArgs(App.Platform.Type.ToString());
		}

        [LuaMethod]
        public LuaArgs getLanguage(in LuaArgs args)
        {
            return new LuaArgs(m_state.Game.Language.Code);
        }

        [LuaMethod]
        public LuaArgs canTranslate(in LuaArgs args)
        {
            var language = m_state.Game.Language;
            var key = args.GetString(0);
            return new LuaArgs(language.CanTranslate(key));
        }

        [LuaMethod]
        public LuaArgs translate(in LuaArgs args)
        {
            var language = m_state.Game.Language;
            var key = args.GetString(0);
            if (args.Length > 1)
            {
                object[] strings = new object[args.Length - 1];
                for (int i = 1; i < args.Length; ++i)
                {
                    strings[i - 1] = args.ToString(i);
                }
                return new LuaArgs(
                    language.Translate(key, strings)
                );
            }
            else
            {
                return new LuaArgs(
                    language.Translate(key)
                );
            }
        }

		[LuaMethod]
		public LuaArgs loadLevel(in LuaArgs args)
		{
			var levelPath = args.GetString(0);
			m_state.Game.QueueState(new InGameState(m_state.Game, levelPath));
			return LuaArgs.Empty;
		}

        [LuaMethod]
        public LuaArgs editLevel(in LuaArgs args)
        {
            string levelPath;
            if (args.IsNil(0) && m_state is LevelState)
            {
                levelPath = m_state.Level.Data.Path;
            }
            else
            {
                levelPath = args.GetString(0);
            }
            m_state.Game.QueueState(new EditorState(m_state.Game, levelPath, levelPath));
            return LuaArgs.Empty;
        }

        [LuaMethod]
		public LuaArgs restartLevel(in LuaArgs args)
		{
            if(m_state is InGameState)
            {
                ((InGameState)m_state).Restart();
            }
            return LuaArgs.Empty;
		}

		[LuaMethod]
		public LuaArgs quit(in LuaArgs args)
		{
			m_state.Game.Over = true;
			return LuaArgs.Empty;
		}
    }
}

