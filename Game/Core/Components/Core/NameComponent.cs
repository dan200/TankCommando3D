using System;
using Dan200.Core.Animation;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Core.Systems;
using Dan200.Core.Serialisation;

namespace Dan200.Core.Components.Core
{
    internal struct NameComponentData
    {
        [Optional]
        public string Name;
    }

	[AfterComponent(typeof(HierarchyComponent))]
	[RequireSystem(typeof(NameSystem))]
    internal class NameComponent : EditableComponent<NameComponentData>, IHierarchyListener, ILuaScriptable
    {
        private NameSystem m_system;

		public string Name
		{
			get
			{
                return m_system.GetName(Entity);
			}
		}

		public string Path
		{
			get
			{
                return m_system.GetPath(Entity);
			}
		}

        protected override void OnInit(in NameComponentData properties)
        {
            m_system = Level.GetSystem<NameSystem>();
            m_system.AddEntity(Entity, properties.Name);
        }

        protected override void Reset(in NameComponentData properties)
        {
            m_system.RemoveEntity(Entity);
            m_system.AddEntity(Entity, properties.Name);
        }

        protected override void OnShutdown()
        {
            m_system.RemoveEntity(Entity);
        }

		public void OnParentChanged(Entity oldParent, Entity newParent)
		{
            m_system.MoveEntity(Entity, oldParent, newParent);
		}

        [LuaMethod]
        public LuaArgs getName(in LuaArgs args)
        {
            return new LuaArgs(Name);
        }

        [LuaMethod]
        public LuaArgs getPath(in LuaArgs args)
        {
            return new LuaArgs(Path);
        }
	}
}
