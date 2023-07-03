namespace Dan200.Core.Multiplayer
{
    // An interface for messages which can be sent over the network.
    // Note: MessageFactory will reuse instances of this class, so make sure your
    // Decode() method fills out your object completely and does not leave stale data.
    internal interface IMessage
    {
        void Reset();
        void Encode(NetworkWriter writer);
        void Decode(NetworkReader reader);
    }
}
