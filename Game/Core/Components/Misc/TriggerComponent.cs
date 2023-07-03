using System;
using System.Collections.Generic;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Script;
using Dan200.Core.Util;
using Dan200.Core.Components.Physics;

namespace Dan200.Core.Components.Misc
{
    internal struct TriggerEventArgs
    {
        public readonly Entity Entity;

        public TriggerEventArgs(Entity entity)
        {
            Entity = entity;
        }
    }

    internal struct TriggerComponentData
    {
    }

    [RequireComponent(typeof(PhysicsComponent))]
    internal class TriggerComponent : Component<TriggerComponentData>, ILuaScriptable
    {
        private List<Entity> m_occupants;

        public int NumOccupants
        {
            get
            {
                return m_occupants.Count;
            }
        }

        public event StructEventHandler<TriggerComponent, TriggerEventArgs> OnFirstEnter;
        public event StructEventHandler<TriggerComponent, TriggerEventArgs> OnEnter;
        public event StructEventHandler<TriggerComponent, TriggerEventArgs> OnLeave;
        public event StructEventHandler<TriggerComponent, TriggerEventArgs> OnFinalLeave;

        protected override void OnInit(in TriggerComponentData properties)
        {
            m_occupants = new List<Entity>();

            var physics = Entity.GetComponent<PhysicsComponent>();
            physics.OnCollisionStart += OnCollisionStart;
            physics.OnCollisionEnd += OnCollisionEnd;
        }

        protected override void OnShutdown()
        {
            var physics = Entity.GetComponent<PhysicsComponent>();
            physics.OnCollisionStart -= OnCollisionStart;
            physics.OnCollisionEnd -= OnCollisionEnd;

            if (m_occupants.Count > 0)
            {
                Entity finalOccupant = null;
                foreach (var occupant in m_occupants)
                {
                    if(OnLeave != null)
                    {
                        OnLeave.Invoke(this, new TriggerEventArgs(occupant));
                    }
                    finalOccupant = occupant;
                }
                if (OnFinalLeave != null)
                {
                    OnFinalLeave.Invoke(this, new TriggerEventArgs(finalOccupant));
                }
                m_occupants.Clear();
            }
        }

        private void OnCollisionStart(PhysicsComponent sender, CollisionStartEventArgs e)
        {
            // Ignore the collision
            e.IgnoreCollision = true;

            // Update occupants
            App.Assert(!m_occupants.Contains(e.HitEntity));
            m_occupants.Add(e.HitEntity);
            if(m_occupants.Count == 1 && OnFirstEnter != null)
            {
                OnFirstEnter.Invoke(this, new TriggerEventArgs(e.HitEntity));
            }
            if (OnEnter != null)
            {
                OnEnter.Invoke(this, new TriggerEventArgs(e.HitEntity));
            }
        }

        private void OnCollisionEnd(PhysicsComponent sender, CollisionEndEventArgs e)
        {
            // Update occupants
            App.Assert(m_occupants.Contains(e.HitEntity));
            m_occupants.UnorderedRemove(e.HitEntity);
            if (OnLeave != null)
            {
                OnLeave.Invoke(this, new TriggerEventArgs(e.HitEntity));
            }
            if (m_occupants.Count == 0 && OnFinalLeave != null)
            {
                OnFinalLeave.Invoke(this, new TriggerEventArgs(e.HitEntity));
            }
        }

        [LuaMethod]
        public LuaArgs onFirstTriggerEnter(in LuaArgs args)
        {
            var function = args.GetFunction(0);
            OnFirstEnter += delegate (TriggerComponent sender, TriggerEventArgs ev) {
                function.Invoke(new LuaArgs(LuaEntity.Wrap(ev.Entity)));
            };
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs onTriggerEnter(in LuaArgs args)
        {
            var function = args.GetFunction(0);
            OnEnter += delegate(TriggerComponent sender, TriggerEventArgs ev) {
                function.Invoke(new LuaArgs(LuaEntity.Wrap(ev.Entity)));
            };
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs onTriggerLeave(in LuaArgs args)
        {
            var function = args.GetFunction(0);
            OnLeave += delegate (TriggerComponent sender, TriggerEventArgs ev) {
                function.Invoke(new LuaArgs(LuaEntity.Wrap(ev.Entity)));
            };
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs onFinalTriggerLeave(in LuaArgs args)
        {
            var function = args.GetFunction(0);
            OnFinalLeave += delegate (TriggerComponent sender, TriggerEventArgs ev) {
                function.Invoke(new LuaArgs(LuaEntity.Wrap(ev.Entity)));
            };
            return LuaArgs.Empty;
        }
    }
}
