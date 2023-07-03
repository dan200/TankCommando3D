using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using Dan200.Core.Network;

namespace Dan200.Game.User
{
    // Remember to add new statistics to the Steam parter site:
    // https://partner.steamgames.com/apps/stats/<appid>
	internal static class Statistics
    {
		public static Statistic TestStatistic = new Statistic("TestStatistic");

		static Statistics()
		{
            // When linking statistics to achievements, make sure you update "Progress Stat" on the Steam parter site:
            // https://partner.steamgames.com/apps/achievements/<appid>
            TestStatistic.LinkToAchievement(Achievements.TestAchievement, 100, new int[] { 10, 25, 50 });
		}

		public static Statistic[] ALL_STATISTICS = {
            TestStatistic,
		};
    }
}
