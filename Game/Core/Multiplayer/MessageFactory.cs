using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Dan200.Core.Multiplayer
{
    internal static class MessageFactory
    {
        private static Dictionary<Type, int> s_typeToId = new Dictionary<Type, int>();
        private static Dictionary<int, IMessage> s_idToInstance = new Dictionary<int, IMessage>();

        public static void ClearMessageTypes()
        {
            s_typeToId.Clear();
            s_idToInstance.Clear();
        }

        public static void RegisterType<TMessage>() where TMessage : IMessage, new()
        {
            var type = typeof(TMessage);
            if (s_typeToId.ContainsKey(type))
            {
                throw new IOException("Message type " + type.Name + " is already registered");
            }

            var id = s_typeToId.Count;
            s_typeToId.Add(type, id);
            try
            {
                s_idToInstance.Add(id, new TMessage());
            }
            catch (TargetInvocationException e)
            {
                throw App.Rethrow(e.InnerException);
            }
        }

        public static TMessage Create<TMessage>() where TMessage : IMessage, new()
        {
            int id;
            var type = typeof(TMessage);
            if (s_typeToId.TryGetValue(type, out id))
            {
                return (TMessage)Create(id);
            }
            else
            {
                throw new IOException("Message type " + type.Name + " is not registered");
            }
        }

        private static IMessage Create(int id)
        {
            IMessage instance;
            if (s_idToInstance.TryGetValue(id, out instance))
            {
                instance.Reset();
                return instance;
            }
            else
            {
                throw new IOException("Unrecognised message ID: " + id);
            }
        }

        public static void Encode(IMessage message, NetworkWriter writer)
        {
            int id;
            var type = message.GetType();
            if (s_typeToId.TryGetValue(type, out id))
            {
                writer.WriteCompact(id);
                message.Encode(writer);
            }
            else
            {
                throw new IOException("Message type " + type.Name + " is not registered");
            }
        }

        public static IMessage Decode(NetworkReader reader)
        {
            var id = reader.ReadCompactInt();
            var instance = Create(id);
            instance.Decode(reader);
            return instance;
        }
    }
}
