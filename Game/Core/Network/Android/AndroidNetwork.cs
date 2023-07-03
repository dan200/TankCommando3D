#if ANDROID
using Android.Content;
using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Main;
using System;
using System.Diagnostics;

namespace Dan200.Core.Network.Android
{
    internal class AndroidNetwork : INetwork
    {
        private AndroidLocalUser m_localUser;

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

        public AndroidNetwork()
        {
            m_localUser = new AndroidLocalUser();
        }

        public long GetGlobalStatistic(string statisticID)
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

        public bool OpenFileBrowser(string path)
        {
            return false;
        }

        public bool OpenTextEditor(string path)
        {
            return false;
        }

        public bool OpenWebBrowser(string url, WebBrowserType preferredType)
        {
            App.Log("Opening web browser to {0}", url);
            try
            {
                var intent = new Intent(Intent.ActionView);
                intent.SetData(global::Android.Net.Uri.Parse(url));
				App.AndroidWindow.Context.StartActivity(intent);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
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

        public bool OpenSteamControllerConfig(ISteamController controller)
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
#endif
