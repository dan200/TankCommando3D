#if STEAM
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Main;
using Steamworks;
using System.Collections.Generic;
using Dan200.Core.Render;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamworksLocalUser : ILocalUser
    {
        private SteamworksNetwork m_network;

        public ulong ID
        {
            get
            {
                return SteamUser.GetSteamID().m_SteamID;
            }
        }

        public string DisplayName
        {
            get
            {
                return SteamFriends.GetPersonaName();
            }
        }

        public SteamworksLocalUser(SteamworksNetwork platform)
        {
            m_network = platform;
        }

        public void UnlockAchievement(Achievement achievement)
        {
			m_network.UnlockLocalUserAchievement(achievement);
        }

		public void RemoveAchievement(Achievement achievement)
        {
			m_network.RemoveLocalUserAchievement(achievement);
        }

        public void UpdateAchievementProgress(Achievement achievement, int currentValue, int unlockValue, bool notify)
        {
            m_network.UpdateLocalUserAchievementProgress(achievement, currentValue, unlockValue, notify);
        }

		public void AddStatistic(Statistic statistic, int count)
        {
            m_network.AddLocalUserStat(statistic, count);
        }

        public void SetStatistic(Statistic statistic, int count)
        {
            m_network.SetLocalUserStat(statistic, count);
        }

        public int GetStatistic(Statistic statistic)
        {
			return m_network.GetLocalUserStat(statistic);
        }

        public Promise SubmitLeaderboardScore(ulong id, int score)
        {
            return m_network.SubmitLocalUserLeaderboardScore(id, score);
        }

		public IRemoteUser[] GetFriends()
		{
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			if (friendCount > 0)
			{
				var friends = new IRemoteUser[friendCount];
				for (int i = 0; i < friends.Length; ++i)
				{
					var friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
					friends[i] = new SteamworksRemoteUser(m_network, friendID);
				}
				return friends;
			}
			else
			{
				return new IRemoteUser[0];
			}
		}

		public Bitmap GetAvatar()
		{
			var handle = SteamFriends.GetMediumFriendAvatar(SteamUser.GetSteamID());
			if (handle != 0)
			{
				return m_network.DecodeBitmap(handle);
			}
			return null;
		}
    }
}
#endif
