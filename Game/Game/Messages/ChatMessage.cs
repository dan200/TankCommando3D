using Dan200.Core.Multiplayer;

namespace Dan200.Game.Messages
{
    internal class ChatMessage : IMessage
    {
        public string Chat;

        public void Reset()
        {
            Chat = "";
        }

        public void Encode(NetworkWriter writer)
        {
            writer.Write(Chat);
        }

        public void Decode(NetworkReader reader)
        {
            Chat = reader.ReadString();
        }
    }
}
