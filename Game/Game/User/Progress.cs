using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Network;
using System;
using System.Text;
using Dan200.Core.Util;
using System.Collections.Generic;
using System.IO;
using Dan200.Core.Lua;

namespace Dan200.Game.User
{
    internal class Progress
    {
        private readonly INetwork m_network;
        public readonly string Path;

        private Dictionary<Statistic, int> m_statistics;
        private HashSet<Achievement> m_achievements;

        public Progress(INetwork network, string path)
        {
            m_network = network;
            Path = path;
            m_statistics = new Dictionary<Statistic, int>();
            m_achievements = new HashSet<Achievement>();
            Reset();
        }

        public void Reset()
        {
            m_achievements.Clear();
            m_statistics.Clear();
        }

        public bool Load()
        {
            App.Assert(Path != null);
            if (File.Exists(Path))
            {
                try
                {
                    // Start from the defaults
                    Reset();

                    // Parse the file
                    LuaTable table;
                    using (var stream = File.OpenRead(Path))
                    {
                        var lon = new LONDecoder(stream);
                        table = lon.DecodeValue().GetTable();
                    }

					// Read the settings
					foreach (var statistic in App.Info.Statistics)
                    {
						int oldValue = m_statistics.ContainsKey(statistic) ? m_statistics[statistic] : 0;
						m_statistics[statistic] = table.GetOptionalInt("Statistics." + statistic.ID, oldValue);
                    }
					foreach (var achievement in App.Info.Achievements)
                    {
                        if (table.GetOptionalBool("Achievements." + achievement.ID, false))
                        {
                            m_achievements.Add(achievement);
                        }
                    }

                    // Send stat and achivement updates to the network
                    foreach (var pair in m_statistics)
                    {
                        var statistic = pair.Key;
                        var value = pair.Value;
                        statistic.UnlockLinkedAchievements(value, value, UnlockAchievement, null);
                        if (m_network.SupportsStatistics)
                        {
                            m_network.LocalUser.SetStatistic(statistic, value);
                        }
                    }
                    foreach (var achievement in m_achievements)
                    {
                        if (m_network.SupportsAchievements)
                        {
                            m_network.LocalUser.UnlockAchievement(achievement);
                        }
                    }
                }
                catch (Exception e)
                {
                    App.LogError("Error parsing {0}: {1}", System.IO.Path.GetFileName(Path), e.Message);
                    App.LogError("Using default settings");
                    Reset();
                }
                return true;
            }
            return false;
        }

        public void Save()
        {
            App.Assert(Path != null);

            // Build the settings table
            var table = new LuaTable();

            // Statistics and achievements
            foreach (var pair in m_statistics)
            {
                var statistic = pair.Key;
                var value = pair.Value;
                table["Statistics." + statistic.ID] = value;
            }
            foreach (var achievement in m_achievements)
            {
                table["Achievements." + achievement.ID] = true;
            }

            // Write the table out
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            using (var output = File.Open(Path, FileMode.Create))
            {
                var lon = new LONEncoder(output);
                lon.EncodeComment("Game Progress");
                lon.Encode(table);
            }

            // Upload to the network
            if (m_network.SupportsStatistics)
            {
                m_network.UploadStatistics();
            }
        }

        public void IndicateAchievementProgress(Achievement achievement, int currentValue, int unlockValue)
        {
            if (m_network.SupportsAchievements)
            {
                m_network.LocalUser.IndicateAchievementProgress(achievement, currentValue, unlockValue);
            }
        }

        public void UnlockAchievement(Achievement achievement)
        {
            if(m_achievements.Add(achievement))
            {
                App.Log("Unlocked achievement " + achievement.ID);
            }
            if (m_network.SupportsAchievements)
            {
                m_network.LocalUser.UnlockAchievement(achievement);
            }
        }

        public bool IsAchievementUnlocked(Achievement achievement)
        {
            return m_achievements.Contains(achievement);
        }

        public void RemoveAchievement(Achievement achievement)
        {
            m_achievements.Remove(achievement);
            if (m_network.SupportsAchievements)
            {
                m_network.LocalUser.RemoveAchievement(achievement);
            }
        }

        public void RemoveAllAchievements()
        {
			foreach (var achievement in App.Info.Achievements)
            {
                RemoveAchievement(achievement);
            }
        }

        public void IncrementStatistic(Statistic stat)
        {
            AddStatistic(stat, 1);
        }

        public void AddStatistic(Statistic stat, int count)
        {
            SetStatistic(stat, GetStatistic(stat) + count);
        }

        public void SetStatistic(Statistic stat, int value)
        {
            int oldValue = m_statistics.ContainsKey(stat) ? m_statistics[stat] : 0;
            m_statistics[stat] = value;
            if (m_network.SupportsStatistics)
            {
				m_network.LocalUser.SetStatistic(stat, value);
            }
			stat.UnlockLinkedAchievements(oldValue, value, UnlockAchievement, IndicateAchievementProgress);
        }

        public int GetStatistic(Statistic stat)
        {
            return m_statistics.ContainsKey(stat) ? m_statistics[stat] : 0;
        }

        public void ResetAllStatistics()
        {
			foreach (var statistic in App.Info.Statistics)
            {
                SetStatistic(statistic, 0);
            }
        }
    }
}
