using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Render;

namespace Dan200.Core.Network
{
    internal interface ILocalUser
    {
        ulong ID { get; }
        string DisplayName { get; }

		void UnlockAchievement(Achievement achievement);
        void RemoveAchievement(Achievement achievement);
        void UpdateAchievementProgress(Achievement achivement, int currentValue, int unlockValue, bool notify);

		void AddStatistic(Statistic statistic, int count);
        void SetStatistic(Statistic statistic, int count);
        int GetStatistic(Statistic statistic);

        Promise SubmitLeaderboardScore(ulong id, int score);

        IRemoteUser[] GetFriends();
        Bitmap GetAvatar();
    }
}
