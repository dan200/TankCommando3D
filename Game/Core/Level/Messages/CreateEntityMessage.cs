using Dan200.Core.Lua;
using Dan200.Core.Multiplayer;

namespace Dan200.Core.Level.Messages
{
    internal class CreateEntityMessage : IMessage
    {
        public int EntityID;
        public int TypeID;
        public LuaTable Properties;
        public EntityMessage InitialState;

        public void Reset()
        {
            EntityID = 0;
            TypeID = 0;
            Properties = LuaTable.Empty;
            InitialState = null;
        }

        public void Encode(NetworkWriter writer)
        {
            writer.WriteCompact(EntityID);
            writer.WriteCompact(TypeID);
            writer.Write(Properties);
            if (InitialState != null)
            {
                writer.Write(true);
                MessageFactory.Encode(InitialState, writer);
            }
            else
            {
                writer.Write(false);
            }
        }

        public void Decode(NetworkReader reader)
        {
            EntityID = reader.ReadCompactInt();
            TypeID = reader.ReadCompactInt();
            Properties = reader.ReadLuaValue().GetTable();
            if (reader.ReadBool())
            {
                InitialState = (EntityMessage)MessageFactory.Decode(reader);
            }
            else
            {
                InitialState = null;
            }
        }
    }
}
