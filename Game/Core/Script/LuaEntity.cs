using Dan200.Core.Lua;
using Dan200.Core.Level;
using System.Collections.Generic;
using Dan200.Core.Interfaces;
using System.Reflection;
using System;
using Dan200.Core.Main;

namespace Dan200.Core.Script
{
    [LuaType("entity")]
    internal class LuaEntity : LuaObject
    {
        private static Dictionary<Entity, LuaEntity> s_wrappedEntities = new Dictionary<Entity, LuaEntity>();

        public static LuaEntity Wrap( Entity entity )
        {
            App.Assert(entity != null);
            LuaEntity result;
            lock (s_wrappedEntities)
            {
                if (!s_wrappedEntities.TryGetValue(entity, out result))
                {
                    result = new LuaEntity(entity);
                    s_wrappedEntities.Add(entity, result);
                }
            }
            return result;
        }

        public Entity Entity;

        private LuaEntity(Entity entity)
        {
            App.Assert(entity != null);
            Entity = entity;
        }

        public override void Dispose()
        {
            App.Assert(Entity != null);
            lock (s_wrappedEntities)
            {
                s_wrappedEntities.Remove(Entity);
            }
            Entity = null;
        }

        [LuaMethod]
        public LuaArgs getID(in LuaArgs args)
        {
            return new LuaArgs(Entity.ID);
        }

        [LuaMethod]
        public LuaArgs isDead(in LuaArgs args)
        {
            return new LuaArgs(Entity.Dead);
        }

        [LuaMethod]
        public LuaArgs isVisible(in LuaArgs args)
        {
            if(Entity.Dead)
            {
                throw new LuaError("Entity is destroyed");
            }
            return new LuaArgs(Entity.Visible);            
        }

        [LuaMethod]
        public LuaArgs setVisible(in LuaArgs args)
        {
            if (Entity.Dead)
            {
                throw new LuaError("Entity is destroyed");
            }
            Entity.Visible = args.GetBool(0);
            return LuaArgs.Empty;
        }

        private delegate LuaArgs ComponentLuaMethod<TComponent>(TComponent o, in LuaArgs args) where TComponent : ComponentBase;

        private static LuaCFunction CreateComponentLuaMethodCaller<TComponent>(MethodInfo method) where TComponent : ComponentBase
        {
            var methodCallDelegate = (ComponentLuaMethod<TComponent>)Delegate.CreateDelegate(typeof(ComponentLuaMethod<TComponent>), method);
            return (LuaCFunction)delegate (in LuaArgs args)
            {
                var entity = args.GetObject<LuaEntity>(0);
                if (entity.Entity.Dead)
                {
                    throw new LuaError("Entity is destroyed");
                }
                var component = entity.Entity.GetComponent<TComponent>();
                if (component == null)
                {
                    int componentID = ComponentRegistry.GetComponentID<TComponent>();
                    string componentName = ComponentRegistry.GetComponentName(componentID);
                    throw new LuaError("Entity " + entity.Entity.ID + " does not have required component " + componentName);
                }
                var subArgs = args.Select(1); // TODO: Surely we can do this more effeciently
                return methodCallDelegate.Invoke(component, subArgs);
            };
        }

        private static Func<MethodInfo, LuaCFunction> CreateComponentLuaMethodCallerCreator(Type type)
        {
            var thisType = typeof(LuaEntity);
            var createMethodCallerGeneric = thisType.GetMethod("CreateComponentLuaMethodCaller", BindingFlags.Static | BindingFlags.NonPublic);
            var createMethodCallerConcrete = createMethodCallerGeneric.MakeGenericMethod(type);
            return (Func<MethodInfo, LuaCFunction>)Delegate.CreateDelegate(typeof(Func<MethodInfo, LuaCFunction>), createMethodCallerConcrete);
        }

        public static void CustomiseMetatable(LuaTable metatable)
        {
            var indexTable = metatable.GetTable("__index");
            var scriptableComponents = ComponentRegistry.GetComponentsImplementingInterface<ILuaScriptable>();
            foreach (var componentID in scriptableComponents)
            {
                var type = ComponentRegistry.GetComponentType(componentID);
                var methodCreator = CreateComponentLuaMethodCallerCreator(type);
                foreach (var method in type.GetMethods())
                {
                    var name = method.Name;
                    var attribute = method.GetCustomAttribute<LuaMethodAttribute>();
                    if (attribute != null)
                    {
                        if (attribute.CustomName != null)
                        {
                            name = attribute.CustomName;
                        }
                        App.Assert(indexTable.IsNil(name), "Multiple scriptable components have lua methods named " + name + ". These must be unique");
                        indexTable[name] = methodCreator.Invoke(method);
                    }
                }
            }
        }
    }
}

