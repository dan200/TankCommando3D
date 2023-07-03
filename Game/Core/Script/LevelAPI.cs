using Dan200.Core.Lua;
using Dan200.Core.Level;
using Dan200.Core.Systems;
using Dan200.Core.Components;

namespace Dan200.Core.Script
{
    internal class LevelAPI : LuaAPI
    {
        private Level.Level m_level;

        public LevelAPI(Level.Level level) : base("level")
        {
            m_level = level;
        }

        [LuaMethod]
        public LuaArgs getPath(in LuaArgs args)
        {
            return new LuaArgs(m_level.Data.Path);
        }

        [LuaMethod]
        public LuaArgs getTime(in LuaArgs args)
        {
            return new LuaArgs(m_level.Clock.Time);
        }

		[LuaMethod]
		public LuaArgs getTimeScale(in LuaArgs args)
		{
			return new LuaArgs(m_level.Clock.Rate);
		}

		[LuaMethod]
		public LuaArgs setTimeScale(in LuaArgs args)
		{
			var rate = args.GetFloat(0);
			if (rate < 0.0f)
			{
				throw new LuaError("Rate must be positive");
			}
			m_level.Clock.Rate = rate;
			return LuaArgs.Empty;
		}
    }
}

