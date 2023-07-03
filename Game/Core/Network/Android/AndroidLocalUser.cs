#if ANDROID
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Main;
using Java.Util;
using System;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Network.Android
{
    internal class AndroidLocalUser : ILocalUser
    {
        private FolderFileStore m_saveStore;

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
                return "AndroidUser";
            }
        }

        public string Language
        {
            get
            {
                return Locale.Default.ToString();
            }
        }

        public IWritableFileStore RemoteSaveStore
        {
            get
            {
                return m_saveStore;
            }
        }

        public AndroidLocalUser()
        {
            var savePath = App.SavePath;
            Directory.CreateDirectory(savePath);
            m_saveStore = new FolderFileStore(savePath);
        }

        public void UnlockAchievement(string achievementID)
        {
        }

        public void RemoveAchievement(string achievementID)
        {
        }

        public void IndicateAchievementProgress(string achievementID, int currentValue, int unlockValue)
        {
        }

        public void AddStatistic(string statisticID, int count)
        {
        }

        public void SetStatistic(string statisticID, int count)
        {
        }

        public int GetStatistic(string statisticID)
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
    }
}
#endif
