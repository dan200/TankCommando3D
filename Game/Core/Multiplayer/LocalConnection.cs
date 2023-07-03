using Dan200.Core.Main;
using Dan200.Core.Util;
using System;

namespace Dan200.Core.Multiplayer
{
    internal class LocalConnection : IConnection
    {
        private RingBuffer m_sendBuffer;
        private NetworkWriter m_sendBufferWriter;

        private RingBuffer m_receiveBuffer;
        private NetworkReader m_receiveBufferReader;

        private ConnectionStatus m_status;

        public bool IsLocal
        {
            get
            {
                return true;
            }
        }

        public TimeSpan PingTime
        {
            get
            {
                return TimeSpan.Zero;
            }
        }

        public ConnectionStatus Status
        {
            get
            {
                return m_status;
            }
        }

        public LocalConnection(Server server)
        {
            // Bit of a hack: steal the internals of an object the server actually created
            var connection = server.CreateLocalConnection();
            m_sendBuffer = connection.m_sendBuffer;
            m_sendBufferWriter = connection.m_sendBufferWriter;
            m_receiveBuffer = connection.m_receiveBuffer;
            m_receiveBufferReader = connection.m_receiveBufferReader;
            m_status = connection.m_status;
        }

        public LocalConnection(RingBuffer sendBuffer, RingBuffer receiveBuffer)
        {
            m_sendBuffer = sendBuffer;
			m_sendBufferWriter = new NetworkWriter(m_sendBuffer.OpenForWrite());

            m_receiveBuffer = receiveBuffer;
			m_receiveBufferReader = new NetworkReader(m_receiveBuffer.OpenForRead());

            m_status = ConnectionStatus.Connecting;
            m_sendBufferWriter.Write((byte)0); // Send a "handshake" to wake up the receiver
        }

        public void Dispose()
        {
            if (m_status != ConnectionStatus.Disconnected)
            {
                Disconnect();
            }
        }

        public bool Receive(out Packet o_packet)
        {
            CheckNotDisconnected();
            if (m_receiveBuffer.BytesAvailable > 0)
            {
                if (m_status == ConnectionStatus.Connecting)
                {
                    m_status = ConnectionStatus.Connected;
                    o_packet = Packet.Connect;
                    m_receiveBufferReader.ReadByte(); // Throw away the handshake
                    return true;
                }

                bool hasPacket = m_receiveBufferReader.ReadBool();
                if (!hasPacket)
                {
                    Disconnect();
                    o_packet = Packet.Disconnect;
                    return true;
                }

                var message = MessageFactory.Decode(m_receiveBufferReader);
                o_packet = new Packet(message);
                return true;
            }
            o_packet = default(Packet);
            return false;
        }

        public void Send(IMessage message)
        {
            CheckConnected();
            m_sendBufferWriter.Write(true);
            MessageFactory.Encode(message, m_sendBufferWriter);
        }

        public void Flush()
        {
            CheckConnected();
        }

        public void Disconnect()
        {
            CheckNotDisconnected();
            m_status = ConnectionStatus.Disconnected;
            m_sendBufferWriter.Write(false);
        }

        private void CheckConnected()
        {
            App.Assert(Status == ConnectionStatus.Connected);
        }

        private void CheckNotDisconnected()
        {
            App.Assert(Status != ConnectionStatus.Disconnected);
        }
    }
}
