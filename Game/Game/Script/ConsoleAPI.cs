using Dan200.Core.Input;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.GUI;
using System.Text;

namespace Dan200.Game.Script
{
    internal class ConsoleAPI : LuaAPI
    {
        private Console m_console;

        public ConsoleAPI(LevelState state) : base("console")
        {
            m_console = state.Game.Console;
        }

        [LuaMethod]
        public LuaArgs log(in LuaArgs args)
        {
            var str = args.GetString(0);
            App.Log(LogLevel.Info, str);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs warn(in LuaArgs args)
        {
            var str = args.GetString(0);
            App.Log(LogLevel.Warning, str);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs error(in LuaArgs args)
        {
            var str = args.GetString(0);
            App.Log(LogLevel.Error, str);
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs clear(in LuaArgs args)
        {
            m_console.Clear();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs isOpen(in LuaArgs args)
        {
            return new LuaArgs(m_console.IsOpen);
        }

        [LuaMethod]
        public LuaArgs open(in LuaArgs args)
        {
            m_console.Open();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs close(in LuaArgs args)
        {
            m_console.Close();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs toggle(in LuaArgs args)
        {
            m_console.Toggle();
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs bind(in LuaArgs args)
        {
            var key = args.GetEnum<Key>(0);
            if (!args.IsNil(1))
            {
                var command = args.GetString(1);
                m_console.Bind(key, command);
                return LuaArgs.Empty;
            }
            else
            {
                return new LuaArgs(m_console.GetBinding(key));
            }
        }

        [LuaMethod]
        public LuaArgs unbind(in LuaArgs args)
        {
            var key = args.GetEnum<Key>(0);
            m_console.Unbind(key);
            return LuaArgs.Empty;
        }
    }
}

