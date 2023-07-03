#if STEAM
using Dan200.Core.Main;
using Dan200.Core.Multiplayer;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dan200.Core.Network.Steamworks
{
    internal class SteamworksRemoteConnection : IRemoteUserConnection
    {
		private const int MAX_PACKET_SIZE = 8192;
        private const int PING_INTERVAL = 5000;

		public bool IsLocal
        {
            get
            {
                return false;
            }
        }

        public TimeSpan PingTime
        {
            get
            {
				return new TimeSpan(m_ping * TimeSpan.TicksPerMillisecond);
            }
        }

        public ConnectionStatus Status
        {
            get
            {
                return m_status;
            }
        }

        public IRemoteUser RemoteUser
        {
            get
            {
                return m_remoteUser;
            }
        }

		private SteamworksNetwork m_network;
        private SteamworksRemoteUser m_remoteUser;
        private ConnectionStatus m_status;

		private byte[] m_sendBuffer;
		private NetworkWriter m_sendBufferWriter;

		private byte[] m_receiveBuffer;
		private NetworkReader m_receiveBufferReader;

        private int m_lastPingTime;
		private ushort m_lastPingID;
		private int m_ping;

		public SteamworksRemoteConnection(SteamworksNetwork network, SteamworksRemoteUser remoteUser)
        {
			m_network = network;
            m_remoteUser = remoteUser;
            m_status = ConnectionStatus.Connecting;

			m_sendBuffer = new byte[MAX_PACKET_SIZE];
			m_sendBufferWriter = new NetworkWriter(new MemoryStream(m_sendBuffer, true));

			m_receiveBuffer = null;
			m_receiveBufferReader = null;

			SendPing();
        }

        public void Dispose()
        {
            if(m_status != ConnectionStatus.Disconnected)
            {
                Disconnect();
            }
        }

        public void Send(IMessage message)
        {
            CheckConnected();

			// Encode the packett
			m_sendBufferWriter.Position = 0;
			m_sendBufferWriter.Write((byte)1);
			MessageFactory.Encode(message, m_sendBufferWriter);
			int size = (int)m_sendBufferWriter.Position;

            // Send the packet
			SteamNetworking.SendP2PPacket(m_remoteUser.SteamID, m_sendBuffer, (uint)size, EP2PSend.k_EP2PSendReliable, 0);
        }

		private void SendDisconnect()
		{
			// Encode the packet
			m_sendBufferWriter.Position = 0;
			m_sendBufferWriter.Write((byte)0);

			// Send the packet
			SteamNetworking.SendP2PPacket(m_remoteUser.SteamID, m_sendBuffer, 1, EP2PSend.k_EP2PSendReliable, 0);
		}

		private void SendPing()
		{
			// Encode the packet
			m_sendBufferWriter.Position = 0;
			m_sendBufferWriter.Write((byte)2);
			m_sendBufferWriter.Write(++m_lastPingID);

			// Send the packet
			m_lastPingTime = Environment.TickCount;
			SteamNetworking.SendP2PPacket(m_remoteUser.SteamID, m_sendBuffer, 3, EP2PSend.k_EP2PSendReliable, 0);
		}

		private void SendPong(ushort id)
		{
			// Encode the packet
			m_sendBufferWriter.Position = 0;
			m_sendBufferWriter.Write((byte)3);
			m_sendBufferWriter.Write(id);

			// Send the packet
			SteamNetworking.SendP2PPacket(m_remoteUser.SteamID, m_sendBuffer, 3, EP2PSend.k_EP2PSendReliable, 0);
		}

		public bool Receive(out Packet o_packet)
        {
            CheckNotDisconnected();

			// Check for errorss
			EP2PSessionError error;
			if (m_network.ReadErrorFrom(m_remoteUser.SteamID, out error))
			{
				m_status = ConnectionStatus.Disconnected;
				o_packet = Packet.Error(error.ToString());
				return true;
			}

			// Check for packets
			if (m_network.PeekPacketFrom(m_remoteUser.SteamID))
			{
				// If this is our first packet, just return a connection message
				if (m_status == ConnectionStatus.Connecting)
                {
                    m_status = ConnectionStatus.Connected;
                    o_packet = Packet.Connect;
                    return true;
                }

				// Otherwise, read the packet
				byte[] bytes;
				int offset;
				int size;
				if(m_network.ReadPacketFrom(m_remoteUser.SteamID, out bytes, out offset, out size))
                {
					// Decode the packet
					if (m_receiveBuffer != bytes)
					{
						m_receiveBuffer = bytes;
						m_receiveBufferReader = new NetworkReader(new MemoryStream(bytes, false));
					}
					m_receiveBufferReader.Position = offset;

					var header = m_receiveBufferReader.ReadByte();
					if (header == 0)
					{
						// Disconnect
						o_packet = Packet.Disconnect;
						Disconnect();
						return true;
					}
					else if (header == 1)
					{
						// Messagee
						var message = MessageFactory.Decode(m_receiveBufferReader);
						o_packet = new Packet(message);
						return true;
					}
					else if (header == 2)
					{
						// Ping
						var id = m_receiveBufferReader.ReadUShort();
						SendPong(id);
						o_packet = Packet.Ping;
						return true;
					}
					else if(header == 3)
					{
						// Pong
						var id = m_receiveBufferReader.ReadUShort();
						if (id == m_lastPingID)
						{
							var now = Environment.TickCount;
							m_ping = (now - m_lastPingTime);
						}
						o_packet = Packet.Pong;
						return false;
					}
                }
            }

			// No packets were available
            // See if it's time to send a ping
            if( (Environment.TickCount - m_lastPingTime) > PING_INTERVAL)
			{
				SendPing();
			}
			o_packet = default(Packet);
            return false;
        }

        public void Flush()
        {
        }

        public void Disconnect()
        {
            CheckNotDisconnected();
			SendDisconnect();
            SteamNetworking.CloseP2PSessionWithUser(m_remoteUser.SteamID);
			m_network.RemoveInterestedPacketSender(m_remoteUser.SteamID);
            m_status = ConnectionStatus.Disconnected;
        }

        private void CheckConnected()
        {
			App.Assert (Status == ConnectionStatus.Connected);
        }

        private void CheckNotDisconnected()
        {
			App.Assert (Status != ConnectionStatus.Disconnected);
        }
    }
}
#endif
