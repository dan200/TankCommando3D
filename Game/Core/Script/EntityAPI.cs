using Dan200.Core.Lua;
using Dan200.Core.Level;
using Dan200.Core.Systems;
using Dan200.Core.Components;

namespace Dan200.Core.Script
{
    internal class EntityAPI : LuaAPI
    {
        private Level.Level m_level;

        public EntityAPI(Level.Level level) : base("entity")
        {
            m_level = level;
        }

        [LuaMethod]
        public LuaArgs create(in LuaArgs args)
        {
            var type = args.GetString(0);
            var properties = args.GetOptionalTable(1, LuaTable.Empty);
            var prefab = EntityPrefab.Get(type);
            var entity = prefab.Instantiate(m_level, properties);
            return new LuaArgs(LuaEntity.Wrap(entity));
        }

		[LuaMethod]
		public LuaArgs find(in LuaArgs args)
		{
			var path = args.GetString(0);
            var root = args.GetOptionalObject<LuaEntity>(1);
            var entity = m_level.GetSystem<NameSystem>().Lookup(path, (root != null) ? root.Entity : null);
			if (entity != null)
			{
                return new LuaArgs(LuaEntity.Wrap(entity));
			}
			else
			{
				return LuaArgs.Nil;
			}
		}

        [LuaMethod]
        public LuaArgs destroy(in LuaArgs args)
        {
            var entity = args.GetObject<LuaEntity>(0);
			var includeChildren = args.GetOptionalBool(1, true);
            if(entity.Entity.Dead)
            {
                throw new LuaError("Entity is already destroyed");
            }
            m_level.Entities.Destroy(entity.Entity, includeChildren);
            return LuaArgs.Empty;
        }
    }
}

