using System.Collections.Generic;

namespace Dan200.Core.Network
{
    internal enum LeaderboardType
    {
        Global,
        Local,
        Friends,
    }

    internal struct LeaderboardEntry
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly int Score;

        public LeaderboardEntry(int rank, string name, int score)
        {
            Rank = rank;
            Name = name;
            Score = score;
        }
    }

    internal class Leaderboard
    {
        public readonly ulong ID;
        public readonly LeaderboardType Type;
        public readonly List<LeaderboardEntry> Entries;

        public Leaderboard(ulong id, LeaderboardType type)
        {
            ID = id;
            Type = type;
            Entries = new List<LeaderboardEntry>();
        }
    }
}
