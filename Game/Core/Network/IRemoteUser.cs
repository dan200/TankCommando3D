using Dan200.Core.Render;

namespace Dan200.Core.Network
{
    internal interface IRemoteUser
    {
        ulong ID { get; }
        string DisplayName { get; }
        OnlineStatus Status { get; }

        Bitmap GetAvatar();
    }
}
