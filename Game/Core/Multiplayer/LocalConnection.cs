using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.IO;

namespace Dan200.Core.Multiplayer
{
    internal class LocalConnection : IConnection
    {
        private RingBuffer m_sendBuffer;
        private BinaryWriter m_sendBufferWriter;

        private RingBuffer m_receiveBuffer;
        private BinaryReader m_receiveBufferReader;

        private ConnectionState m_state;

        public bool IsLocal
        {
            get
            {
                return true;
            }
        }

        public float PingTime
        {
            get
            {
                return 0.0f;
            }
        }

        public ConnectionState State
        {
            get
            {
                return m_state;
            }
        }

        public LocalConnection(RingBuffer sendBuffer, RingBuffer receiveBuffer)
        {
            m_sendBuffer = sendBuffer;
            m_sendBufferWriter = new BinaryWriter(m_sendBuffer.OpenForWrite());

            m_receiveBuffer = receiveBuffer;
            m_receiveBufferReader = new BinaryReader(m_receiveBuffer.OpenForRead());

            m_state = ConnectionState.Connecting;
            m_sendBufferWriter.Write((byte)0); // Send a "handshake" to wake up the receiver
        }

        public void Dispose()
        {
            if (m_state != ConnectionState.Disconnected)
            {
                Disconnect();
            }
        }

        public bool Receive(out Packet o_packet)
        {
            CheckNotDisconnected();
            if (m_receiveBuffer.BytesAvailable > 0)
            {
                if (m_state == ConnectionState.Connecting)
                {
                    m_state = ConnectionState.Connected;
                    o_packet = Packet.Connect;
                    m_receiveBufferReader.ReadByte(); // Throw away the handshake
                    return true;
                }

                int messageSize = m_receiveBufferReader.ReadInt32();
                if (messageSize < 0)
                {
                    Disconnect();
                    o_packet = Packet.Disconnect;
                    return true;
                }

                var message = m_receiveBufferReader.ReadBytes(messageSize);
                o_packet = new Packet(new ByteString(message));
                return true;
            }
            o_packet = default(Packet);
            return false;
        }

        public void SendMessage(ByteString message)
        {
            CheckConnected();
            m_sendBufferWriter.Write(message.Length);
            m_sendBufferWriter.Write(message);
        }

        public void Flush()
        {
            CheckConnected();
        }

        public void Disconnect()
        {
            CheckNotDisconnected();
            m_state = ConnectionState.Disconnected;
            m_sendBufferWriter.Write(-1);
        }

        private void CheckConnected()
        {
            App.Assert(State == ConnectionState.Connected);
        }

        private void CheckNotDisconnected()
        {
            App.Assert(State != ConnectionState.Disconnected);
        }
    }
}
