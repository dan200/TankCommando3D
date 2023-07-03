using Dan200.Core.Main;
using Dan200.Core.Util;

namespace Dan200.Core.Multiplayer
{
    internal enum PacketType
    {
        Connect,
        Disconnect,
        Message,
        Error,
        Ping,
        Pong
    }

    internal struct Packet
    {
        public static Packet Connect = new Packet(PacketType.Connect, ByteString.Empty);
        public static Packet Disconnect = new Packet(PacketType.Disconnect, ByteString.Empty);
        public static Packet Ping = new Packet(PacketType.Ping, ByteString.Empty);
        public static Packet Pong = new Packet(PacketType.Pong, ByteString.Empty);

        public static Packet Error(string message)
        {
            return new Packet(PacketType.Error, new ByteString(message));
        }

        public readonly PacketType Type;
        private readonly ByteString Data;

        public ByteString Message
        {
            get
            {
                App.Assert(Type == PacketType.Message);
                return Data;
            }
        }

        public string ErrorMessage
        {
            get
            {
                App.Assert(Type == PacketType.Error);
                return Data.ToString();
            }
        }

        public Packet(ByteString message) : this(PacketType.Message, message)
        {
        }

        private Packet(PacketType type, ByteString data)
        {
            Type = type;
            Data = data;
        }
    }
}
