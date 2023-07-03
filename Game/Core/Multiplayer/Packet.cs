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
        public static Packet Connect = new Packet(PacketType.Connect, null);
        public static Packet Disconnect = new Packet(PacketType.Disconnect, null);
        public static Packet Ping = new Packet(PacketType.Ping, null);
        public static Packet Pong = new Packet(PacketType.Pong, null);

        public static Packet Error(string message)
        {
            return new Packet(PacketType.Error, message);
        }

        public readonly PacketType Type;
        private readonly object Data;

        public IMessage Message
        {
            get
            {
                return Data as IMessage;
            }
        }

        public string ErrorMessage
        {
            get
            {
                return Data as string;
            }
        }

        public Packet(IMessage message) : this(PacketType.Message, message)
        {
        }

        private Packet(PacketType type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}
