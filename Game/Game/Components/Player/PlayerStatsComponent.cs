using Dan200.Core.Level;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Player
{
    internal enum PlayerStatistic
    {
        TanksKilled,
    }

    internal struct PlayerStatsComponentData
    {
    }

    internal class PlayerStatsComponent : Component<PlayerStatsComponentData>
    {
        private Dictionary<PlayerStatistic, int> m_stats;

        protected override void OnInit(in PlayerStatsComponentData properties)
        {
            m_stats = new Dictionary<PlayerStatistic, int>();
        }

        protected override void OnShutdown()
        {
        }

        public void AddStat(PlayerStatistic statistic, int increment=1)
        {
            int currentValue;
            if (m_stats.TryGetValue(statistic, out currentValue))
            {
                m_stats[statistic] = currentValue + increment;
            }
            else
            {
                m_stats.Add(statistic, increment);
            }
        }

        public int GetStat(PlayerStatistic statistic)
        {
            int currentValue;
            if (!m_stats.TryGetValue(statistic, out currentValue))
            {
                currentValue = 0;
            }
            return currentValue;
        }
    }
}
