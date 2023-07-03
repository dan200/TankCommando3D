using System;
using Dan200.Core.Network;
using Dan200.Core.Util;

namespace Dan200.Core.Main
{
    internal class GameInfo
    {
        public string Title;
        public Version Version;
        public string Website;
        public string DeveloperName;
        public uint SteamAppID;
		public Achievement[] Achievements;
		public Statistic[] Statistics;

        public GameInfo()
        {
            Title = "Untitled";
            Version = new Version(0, 0, 0);
            Website = "";
            DeveloperName = "";
            SteamAppID = 0;
			Achievements = EmptyArray<Achievement>.Instance;
			Statistics = EmptyArray<Statistic>.Instance;
        }
    }
}

