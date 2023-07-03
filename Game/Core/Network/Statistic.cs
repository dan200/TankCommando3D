using System;
using System.Collections.Generic;
using Dan200.Core.Main;

namespace Dan200.Core.Network
{
	internal class Statistic
	{
		public readonly string ID;

		private struct LinkedAchievement
		{
			public Achievement Achievement;
			public int UnlockValue;
			public int[] NotifyValues;
		}
		private List<LinkedAchievement> m_linkedAchievements;

		public Statistic(string id)
		{
			ID = id;
			m_linkedAchievements = null;
		}

		public void LinkToAchievement(Achievement achievement, int unlockValue, int[] notifyValues = null)
		{
			App.Assert(unlockValue > 0);
			if (notifyValues != null)
			{
				int lastNotifyValue = 0;
				foreach (int notifyValue in notifyValues)
				{
					App.Assert(notifyValue > lastNotifyValue && notifyValue < unlockValue);
					lastNotifyValue = notifyValue;
				}
			}

			if (m_linkedAchievements == null)
			{
				m_linkedAchievements = new List<LinkedAchievement>();
			}

			LinkedAchievement link;
			link.Achievement = achievement;
			link.UnlockValue = unlockValue;
			link.NotifyValues = notifyValues;
			m_linkedAchievements.Add(link);
		}

		public void UnlockLinkedAchievements(int oldValue, int newValue, Action<Achievement> unlockCallback, Action<Achievement, int, int> progressCallback)
		{
			if (m_linkedAchievements != null)
			{
				foreach(var link in m_linkedAchievements)
				{
					if (newValue >= link.UnlockValue)
					{
						if (unlockCallback != null)
						{
							unlockCallback.Invoke(link.Achievement);
						}
					}
					else if(link.NotifyValues != null)
					{
						if (progressCallback != null)
						{
							for (int i = link.NotifyValues.Length - 1; i >= 0; --i)
							{
								var notifyValue = link.NotifyValues[i];
								if (oldValue < notifyValue && newValue >= notifyValue)
								{
									progressCallback.Invoke(link.Achievement, newValue, link.UnlockValue);
									break;
								}
							}
						}
					}
				}
			}
		}
	}
}
