using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Main;
using Dan200.Core.Render;
using System;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Network.Builtin
{
    internal class NullLocalUser : ILocalUser
    {
        public ulong ID
        {
            get
            {
                return 0;
            }
        }

        public string DisplayName
        {
            get
            {
                return Environment.UserName;
            }
        }

        public NullLocalUser()
        {
        }

		public void UnlockAchievement(Achievement achievement)
        {
        }

        public void RemoveAchievement(Achievement achievement)
        {
        }

        public void IndicateAchievementProgress(Achievement achievement, int currentValue, int unlockValue)
        {
        }

		public void AddStatistic(Statistic statistic, int count)
        {
        }

        public void SetStatistic(Statistic statistic, int count)
        {
        }

        public int GetStatistic(Statistic statistic)
        {
            return 0;
        }

        public void UploadStats()
        {
        }

        public Promise SubmitLeaderboardScore(ulong id, int score)
        {
            throw new NotImplementedException();
        }

        public IRemoteUser[] GetFriends()
        {
            return new IRemoteUser[0];
        }

        public Bitmap GetAvatar()
        {
            return null;
        }
    }
}

