using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Multiplayer;
using Dan200.Core.Util;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    internal class Player
    {
        public int ID;
        public string Name;
        public bool IsLocal;
        public Entity Avatar;
        public IConnection Connection;

        public Player()
        {
            ID = 0;
            IsLocal = false;
            Name = "Player";
            Avatar = null;
            Connection = null;
        }
    }

    internal class PlayerCollection : IReadOnlyCollection<Player>
    {
        private Dictionary<int, Player> m_playersByID;
        private List<Player> m_players;
        private int m_nextUnusedPlayerID;

        public int Count
        {
            get
            {
                return m_players.Count;
            }
        }

        public PlayerCollection(Core.Level.Level owner)
        {
            m_playersByID = new Dictionary<int, Player>();
            m_players = new List<Player>();
            m_nextUnusedPlayerID = 1;
        }

        public Player Lookup(int id)
        {
            Player player;
            if (m_playersByID.TryGetValue(id, out player))
            {
                return player;
            }
            return null;
        }

        public void Add(Player player)
        {
            AddImpl(player, AssignID());
        }

        public void Add(Player player, int id)
        {
            App.Assert(id != 0, "Attempt to add a player with ID 0");
            App.Assert(!m_playersByID.ContainsKey(id), "Attempt to re-use a player ID");
            AddImpl(player, id);
        }

        private void AddImpl(Player player, int id)
        {
            App.Assert(player.ID == 0, "Attempt to add a player that already has an ID");

            // Add the player
            player.ID = id;
            m_playersByID.Add(id, player);
            m_players.Add(player);
        }

        public void Remove(Player player, string reason)
        {
            App.Assert(m_players.Contains(player));

            // Remove the player
            var id = player.ID;
            m_players.UnorderedRemove(player);
            m_playersByID.Remove(id);
        }

        public List<Player>.Enumerator GetEnumerator()
        {
            return m_players.GetEnumerator();
        }

        IEnumerator<Player> IEnumerable<Player>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int AssignID()
        {
            return m_nextUnusedPlayerID++;
        }
    }

    internal abstract class PlayerMessage : IMessage
    {
        public int PlayerID;

        public virtual void Reset()
        {
            PlayerID = 0;
        }

        public virtual void Encode(NetworkWriter writer)
        {
            writer.WriteCompact(PlayerID);
        }

        public virtual void Decode(NetworkReader reader)
        {
            PlayerID = reader.ReadCompactInt();
        }
    }

    internal class PlayerJoinedMessage : PlayerMessage
    {
        public string Name;
        public int AvatarEntityID;

        public override void Reset()
        {
            base.Reset();
            Name = "Player";
            AvatarEntityID = 0;
        }

        public override void Encode(NetworkWriter writer)
        {
            base.Encode(writer);
            writer.Write(Name);
            writer.WriteCompact(AvatarEntityID);
        }

        public override void Decode(NetworkReader reader)
        {
            base.Decode(reader);
            Name = reader.ReadString();
            AvatarEntityID = reader.ReadCompactInt();
        }
    }

    internal class PlayerLeftMessage : PlayerMessage
    {
        public string Reason;

        public override void Reset()
        {
            base.Reset();
            Reason = string.Empty;
        }

        public override void Encode(NetworkWriter writer)
        {
            base.Encode(writer);
            writer.Write(Reason);
        }

        public override void Decode(NetworkReader reader)
        {
            base.Decode(reader);
            Reason = reader.ReadString();
        }
    }

    internal class AssignPlayerMessage : PlayerMessage
    {
    }
}