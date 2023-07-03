using System;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Render;

namespace Dan200.Core.Level
{
    internal abstract class ComponentBase
    {
        [Flags]
        private enum Flags
        {
            Initialised = 1,
            Dead = 8,
        }

        private Entity m_entity;
        private Flags m_flags;

        public Entity Entity
        {
            get
            {
                return m_entity;
            }
        }

        public bool Dead
        {
            get
            {
                return (m_flags & Flags.Dead) != 0;
            }
        }

        public Level Level
        {
            get
            {
                return m_entity.Level;
            }
        }

        public void Init(Entity entity, LuaTable properties)
        {
            App.Assert(m_entity == null);
            m_entity = entity;
            App.Assert((m_flags & Flags.Initialised) == 0);
            OnInit(properties);
            m_flags |= Flags.Initialised;
        }

        public void Shutdown()
        {
            App.Assert((m_flags & Flags.Initialised) != 0);
            App.Assert((m_flags & Flags.Dead) == 0);
            OnShutdown();
            m_flags |= Flags.Dead;
        }

        protected abstract void OnInit(LuaTable properties);
        protected abstract void OnShutdown();

		public override string ToString()
		{
			var componentID = ComponentRegistry.GetComponentID(this);
			return string.Format("[{0} {1}]", ComponentRegistry.GetComponentName(componentID), Entity.ID);
		}
    }
}
