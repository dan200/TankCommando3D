using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Util;

namespace Dan200.Core.Network
{
    internal enum AchievementCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    internal interface INetwork
    {
        bool SupportsAchievements { get; }
        bool SupportsStatistics { get; }
        bool SupportsWorkshop { get; }
        bool SupportsLeaderboards { get; }
        bool SupportsFriends { get; }
        bool SupportsConnections { get; }
		bool SupportsRemoteSave { get; }

        ILocalUser LocalUser { get; }
        IWorkshop Workshop { get; }
		IWritableFileStore RemoteSaveStore { get; }

        IRemoteUserConnectionListener ConnectionListener { get; }
        IRemoteUserConnection ConnectTo(IRemoteUser user);

		long GetGlobalStatistic(Statistic statistic);
        void UploadStatistics();
        int GetConcurrentPlayers();

        void SetAchievementCorner(AchievementCorner corner);

        bool OpenWorkshopItem(ulong id);
        bool OpenWorkshopHub();
        bool OpenWorkshopHub(string[] filterTags);
        bool OpenAchievementsHub();
        bool OpenLeaderboard(ulong id);

        Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent);
        Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries);
    }
}
