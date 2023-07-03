using System;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Util;

namespace Dan200.Core.Lua
{
    internal struct LuaEvent
    {
        private List<LuaFunction> m_subscribers;

        public bool HasSubscribers
        {
            get
            {
                return m_subscribers != null && m_subscribers.Count > 0;
            }
        }

        public void Subscribe(LuaFunction function)
        {
            if(m_subscribers == null)
            {
                m_subscribers = new List<LuaFunction>();
            }
            m_subscribers.Add(function);
        }

        public void Unsubscribe(LuaFunction function)
        {
            App.Assert(m_subscribers != null && m_subscribers.Contains(function));
            m_subscribers.UnorderedRemove(function);
        }

        public void Invoke(in LuaArgs args)
        {
            if (m_subscribers != null)
            {
                foreach (var subscriber in m_subscribers)
                {
                    try
                    {
                        subscriber.Call(args);
                    }
                    catch(LuaError e)
                    {
                        App.LogError(e.Message);
                    }
                }
            }
        }
    }
}
