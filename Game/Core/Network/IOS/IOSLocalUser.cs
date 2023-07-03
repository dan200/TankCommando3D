#if IOS
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Main;
using System;
using System.IO;
using Foundation;

namespace Dan200.Core.Network.IOS
{
    internal class IOSLocalUser : ILocalUser
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
                return "iOSUser";
            }
        }

        public string Language
        {
            get
            {
				var languages = NSLocale.PreferredLanguages;
				if (languages.Length > 0)
				{
					return languages[0].Replace('-', '_');
				}
				return "en";
            }
        }

        public IWritableFileStore RemoteSaveStore
        {
            get
            {
                return m_saveStore;
            }
        }

		public IOSLocalUser()
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
