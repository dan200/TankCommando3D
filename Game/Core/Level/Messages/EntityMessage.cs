using Dan200.Core.Multiplayer;

namespace Dan200.Core.Level.Messages
{
    internal abstract class EntityMessage : IMessage
    {
        public int EntityID;

        public virtual void Reset()
        {
            EntityID = 0;
        }

        public virtual void Encode(NetworkWriter writer)
        {
            writer.WriteCompact(EntityID);
        }

        public virtual void Decode(NetworkReader reader)
        {
            EntityID = reader.ReadCompactInt();
        }
    }
}
