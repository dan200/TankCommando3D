using Dan200.Core.Util;
using Dan200.Core.Network;
using System;

namespace Dan200.Game.User
{
	// Remember to add new achievements to the Steam parter site:
    // https://partner.steamgames.com/apps/achievements/<appid>
	internal static class Achievements
    {
		public static Achievement TestAchievement = new Achievement("TestAchievement");

		public static Achievement[] ALL_ACHIEVEMENTS = {
            TestAchievement,
		};
    }
}

