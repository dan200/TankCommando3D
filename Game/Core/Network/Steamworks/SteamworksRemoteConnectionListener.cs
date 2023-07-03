#if STEAM
using Dan200.Core.Main;
using Dan200.Core.Multiplayer;
using Dan200.Core.Util;
using Steamworks;
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamworksRemoteConnectionListener : IRemoteUserConnectionListener
    {
        private SteamworksNetwork m_network;
		private HashSet<CSteamID> m_newUsers;
        private bool m_open;

        public bool IsOpen
        {
            get
            {
                return m_open;
            }
        }

        public SteamworksRemoteConnectionListener(SteamworksNetwork network)
        {
            m_network = network;
			m_newUsers = new HashSet<CSteamID>();
            m_open = false;

            SteamNetworking.AllowP2PPacketRelay(true);
            m_network.RegisterCallback<P2PSessionRequest_t>(OnP2PSessionRequest);
        }

        public void Dispose()
        {
            if(IsOpen)
            {
                Close();
            }
        }

        public void Open()
        {
			App.Assert (!IsOpen);
            m_open = true;
        }

        public void Close()
        {
			App.Assert (IsOpen);
            m_open = false;
        }

        public bool Accept(out IConnection o_connection)
        {
			IRemoteUserConnection connection;
            if (Accept(out connection))
            {
                o_connection = connection;
                return true;
            }
            o_connection = null;
            return false;
        }

        public bool Accept(out IRemoteUserConnection o_connection)
        {
			var Enumerator = m_newUsers.GetEnumerator();
			if(Enumerator.MoveNext())
            {
				// Get an id
				var steamID = Enumerator.Current;
				m_newUsers.Remove(steamID);
				          
				// Return the connection
                var remoteUser = new SteamworksRemoteUser(m_network, steamID);
                o_connection = new SteamworksRemoteConnection(m_network, remoteUser);
                return true;
            }
            o_connection = null;
            return false;
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t args)
        {
            if( m_open )
            {
                var steamID = args.m_steamIDRemote;
                SteamNetworking.AcceptP2PSessionWithUser(steamID);
				if (m_network.AddInterestedPacketSender(steamID))
				{
					m_newUsers.Add(steamID);
				}
            }
        }
    }
}
#endif
