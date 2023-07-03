using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Main;
using System;
using System.Diagnostics;
using Dan200.Core.Platform;
using Dan200.Core.Assets;

namespace Dan200.Core.Network.Builtin
{
    internal class NullNetwork : INetwork
    {
        private NullLocalUser m_localUser;

        public bool SupportsAchievements
        {
            get
            {
                return false;
            }
        }

        public bool SupportsStatistics
        {
            get
            {
                return false;
            }
        }

        public bool SupportsWorkshop
        {
            get
            {
                return false;
            }
        }

        public bool SupportsLeaderboards
        {
            get
            {
                return false;
            }
        }

        public bool SupportsFriends
        {
            get
            {
                return false;
            }
        }

        public bool SupportsConnections
        {
            get
            {
                return false;
            }
        }

		public bool SupportsRemoteSave
		{
			get
			{
				return false;
			}
		}

        public ILocalUser LocalUser
        {
            get
            {
                return m_localUser;
            }
        }

        public IWorkshop Workshop
        {
            get
            {
                return null;
            }
        }

		public IWritableFileStore RemoteSaveStore
		{
			get
			{
				return null;
			}
		}

        public IRemoteUserConnectionListener ConnectionListener
        {
            get
            {
                return null;
            }
        }

        public NullNetwork()
        {
            m_localUser = new NullLocalUser();
        }

        public IRemoteUserConnection ConnectTo(IRemoteUser user)
        {
            throw new NotImplementedException();
        }

        public long GetGlobalStatistic(Statistic statistic)
        {
            return 0;
        }

        public void UploadStatistics()
        {
        }

        public int GetConcurrentPlayers()
        {
            return 1;
        }

        public void SetAchievementCorner(AchievementCorner corner)
        {
        }

        public bool OpenWorkshopItem(ulong id)
        {
            return false;
        }

        public bool OpenWorkshopHub()
        {
            return false;
        }

        public bool OpenWorkshopHub(string[] filterTags)
        {
            return false;
        }

        public bool OpenAchievementsHub()
        {
            return false;
        }

        public bool OpenLeaderboard(ulong id)
        {
            return false;
        }

        public Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent)
        {
            throw new NotImplementedException();
        }

        public Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries)
        {
            throw new NotImplementedException();
        }
    }
}
