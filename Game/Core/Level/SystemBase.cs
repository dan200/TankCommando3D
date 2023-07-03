using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Level
{
    internal abstract class SystemBase
    {
        private Level m_level;

        public Level Level
        {
            get
            {
                return m_level;
            }
        }

        public void Init(Level level, LuaTable properties)
        {
            m_level = level;
            OnInit(properties);
        }

        public void Shutdown()
        {
            OnShutdown();
        }

        protected abstract void OnInit(LuaTable properties);
        protected abstract void OnShutdown();

        public override string ToString()
        {
            var systemID = ComponentRegistry.GetSystemID(this);
            return string.Format("[{0}]", ComponentRegistry.GetSystemName(systemID));
        }
    }
}
