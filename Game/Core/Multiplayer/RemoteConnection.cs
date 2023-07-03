using Dan200.Core.Main;
using Dan200.Core.Math;
using System;
using System.IO;
using System.Net.Sockets;
using Dan200.Core.Util;

namespace Dan200.Core.Multiplayer
{
    internal class RemoteConnection : IConnection, IDisposable
    {
        private const int MAX_MESSAGE_SIZE = 8192;
        private const int PING_INTERVAL = 5000;
        private const int TIMEOUT = 10000;

        private const byte PACKETCODE_HANDSHAKE = 0;
        private const byte PACKETCODE_DISCONNECT = 1;
        private const byte PACKETCODE_PING = 2;
        private const byte PACKETCODE_PONG = 3;
        private const byte PACKETCODE_MESSAGE = 4;

        private ConnectionState m_state;

        private TcpClient m_tcpClient;
        private IAsyncResult m_pendingTcpConnect;
        private NetworkStream m_tcpStream;
        private BufferedStream m_bufferedTcpStream;
        private BinaryWriter m_bufferedTcpStreamWriter;

        private byte[] m_receiveBuffer;
        private int m_receiveBufferProgress;
        private int m_receivedPacketType;
        private int m_receivedMessageSize;
        private BinaryReader m_receiveBufferReader;

        private int m_lastPingTimeInMillis;
        private ushort m_lastPingID;
        private int m_pingInMillis;

        private Exception m_pendingError;

        public bool IsLocal
        {
            get
            {
                return false;
            }
        }

        public float PingTime
        {
            get
            {
                return m_pingInMillis / 1000.0f;
            }
        }

        public ConnectionState State
        {
            get
            {
                return m_state;
            }
        }

        private void RegisterMethod<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method)
        {

        }

        public RemoteConnection(string hostname, int tcpPort)
        {
            // Construct buffers
            m_receiveBuffer = new byte[MAX_MESSAGE_SIZE];
            m_receiveBufferProgress = 0;
            m_receivedPacketType = -1;
            m_receivedMessageSize = -1;
            m_receiveBufferReader = new BinaryReader(new MemoryStream(m_receiveBuffer, false));

            // Start connecting
            m_state = ConnectionState.Connecting;

            m_tcpClient = new TcpClient();
            m_tcpClient.NoDelay = true;
            m_tcpClient.ReceiveTimeout = TIMEOUT;
            m_tcpClient.SendTimeout = TIMEOUT;
            try
            {
                m_pendingTcpConnect = m_tcpClient.BeginConnect(hostname, tcpPort, null, null);
                m_tcpStream = null;
                m_bufferedTcpStream = null;
                m_bufferedTcpStreamWriter = null;
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        internal RemoteConnection(TcpClient tcpClient)
        {
            // Construct buffers
            m_receiveBuffer = new byte[MAX_MESSAGE_SIZE];
            m_receiveBufferProgress = 0;
            m_receivedPacketType = -1;
            m_receivedMessageSize = -1;
            m_receiveBufferReader = new BinaryReader(new MemoryStream(m_receiveBuffer, false));

            // Assume already connected
            m_state = ConnectionState.Connecting;
            m_tcpClient = tcpClient;

            try
            {
                // Open the stream
                m_tcpStream = tcpClient.GetStream();
                m_bufferedTcpStream = new BufferedStream(m_tcpStream);
                m_bufferedTcpStreamWriter = new BinaryWriter(m_bufferedTcpStream);

                // Send the handshake and the first ping
                SendHandshake();
                SendPing();
                m_bufferedTcpStreamWriter.Flush();
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        public virtual void Dispose()
        {
            if (m_state != ConnectionState.Disconnected)
            {
                Disconnect();
            }
        }

        public void SendMessage(ByteString message)
        {
            // Check state
            CheckConnected();
            App.Assert(message.Length <= MAX_MESSAGE_SIZE);

            // Transmit the packet
            try
            {
                m_bufferedTcpStreamWriter.Write(PACKETCODE_MESSAGE);
                m_bufferedTcpStreamWriter.Write((ushort)message.Length);
                m_bufferedTcpStreamWriter.Write(message);
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        private void SendPing()
        {
            // Record the ping time
            m_lastPingTimeInMillis = Environment.TickCount;

            // Transmit the packet
            ushort pingID = ++m_lastPingID;
            m_bufferedTcpStreamWriter.Write(PACKETCODE_PING);
            m_bufferedTcpStreamWriter.Write(pingID);
        }

        private void SendPong(ushort pingID)
        {
            // Send the packet
            m_bufferedTcpStreamWriter.Write(PACKETCODE_PONG);
            m_bufferedTcpStreamWriter.Write(pingID);
        }

        public bool Receive(out Packet o_packet)
        {
            // Send the state
            CheckNotDisconnected();
            try
            {
                // Report an error that occurred during Send() or Flush()
                if (m_pendingError != null)
                {
                    throw m_pendingError;
                }

                // Finish connecting
                if (m_pendingTcpConnect != null)
                {
                    if (m_pendingTcpConnect.IsCompleted)
                    {
                        // Finish connecting
                        m_tcpClient.EndConnect(m_pendingTcpConnect);
                        m_pendingTcpConnect = null;

                        // Open the stream
                        m_tcpStream = m_tcpClient.GetStream();
                        m_bufferedTcpStream = new BufferedStream(m_tcpStream);
                        m_bufferedTcpStreamWriter = new BinaryWriter(m_bufferedTcpStream);

                        // Send the handshake and the first ping
                        SendHandshake();
                        SendPing();
                        m_bufferedTcpStreamWriter.Flush();
                    }
                    else
                    {
                        // Not connected yet
                        o_packet = default(Packet);
                        return false;
                    }
                }

                // See if it's time to send a ping
                if ((Environment.TickCount - m_lastPingTimeInMillis) > PING_INTERVAL)
                {
                    SendPing();
                }

                // Wait for a packet
                if(m_receivedPacketType < 0)
                {
                    if(WaitForBytes(1))
                    {
                        m_receivedPacketType = m_receiveBuffer[0];
                    }
                }

                // Parse the packet contents
                if (m_receivedPacketType >= 0)
                {
                    if (m_state == ConnectionState.Connecting)
                    {
                        // Connecting: Wait for handshake
                        switch (m_receivedPacketType)
                        {
                            case PACKETCODE_HANDSHAKE:
                                {
                                    // Handshake
                                    if (WaitForBytes(4))
                                    {
                                        // Verify the handshake is correct
                                        int handshake = m_receiveBufferReader.ReadInt32();
                                        if (VerifyHandshake(handshake))
                                        {
                                            // Correct handshake: connect
                                            m_state = ConnectionState.Connected;
                                            o_packet = Packet.Connect;
                                            EndPacket();
                                            return true;
                                        }
                                        else
                                        {
                                            // Failed handshake: disconnect
                                            Disconnect();
                                            o_packet = Packet.Error("Handshake failed");
                                            EndPacket();
                                            return true;
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    // Invalid packet: disconnect
                                    Disconnect();
                                    o_packet = Packet.Error("Invalid packet type");
                                    EndPacket();
                                    return true;
                                }
                        }
                    }
                    else
                    {
                        // Disconnecting: Wait for anything
                        switch(m_receivedPacketType)
                        {
                            case PACKETCODE_PING:
                                {
                                    // Ping
                                    if (WaitForBytes(2))
                                    {
                                        // Reply to the ping
                                        ushort pingID = m_receiveBufferReader.ReadUInt16();
                                        SendPong(pingID);
                                        o_packet = Packet.Ping;
                                        EndPacket();
                                        return true;
                                    }
                                    break;
                                }
                            case PACKETCODE_PONG:
                                {
                                    // Pong
                                    if (WaitForBytes(2))
                                    {
                                        // Measure the ping
                                        ushort pingID = m_receiveBufferReader.ReadUInt16();
                                        if (pingID == m_lastPingID)
                                        {
                                            var now = Environment.TickCount;
                                            m_pingInMillis = (now - m_lastPingTimeInMillis);
                                        }
                                        o_packet = Packet.Pong;
                                        EndPacket();
                                        return true;
                                    }
                                    break;
                                }
                            case PACKETCODE_MESSAGE:
                                {
                                    // Message
                                    if(m_receivedMessageSize < 0)
                                    {
                                        if(WaitForBytes(2))
                                        {
                                            // Store the message size
                                            ushort messageSize = m_receiveBufferReader.ReadUInt16();
                                            if(messageSize > MAX_MESSAGE_SIZE)
                                            {
                                                Disconnect();
                                                o_packet = Packet.Error("Message too large");
                                                EndPacket();
                                                return true;
                                            }
                                            else
                                            {
                                                m_receivedMessageSize = messageSize;
                                            }
                                        }
                                    }
                                    if(m_receivedMessageSize >= 0)
                                    {
                                        int messageSize = m_receivedMessageSize;
                                        if(WaitForBytes(messageSize))
                                        {
                                            o_packet = new Packet(new ByteString(m_receiveBuffer, 0, messageSize));
                                            EndPacket();
                                            return true;
                                        }
                                    }
                                    break;
                                }
                            case PACKETCODE_DISCONNECT:
                                {
                                    Disconnect();
                                    o_packet = Packet.Disconnect;
                                    EndPacket();
                                    break;
                                }
                            default:
                                {
                                    // Invalid packet: disconnect
                                    Disconnect();
                                    o_packet = Packet.Error("Invalid packet type");
                                    EndPacket();
                                    return true;
                                }
                        }
                    }
                }

                // No packets received
                o_packet = default(Packet);
                return false;
            }
            catch (SocketException e)
            {
                // Return a socket error
                Disconnect();
                o_packet = Packet.Error(e.SocketErrorCode.ToString());
                EndPacket();
                return true;
            }
            catch (IOException e)
            {
                // Return a I/O error
                Disconnect();
                if (e.InnerException is SocketException)
                {
                    var inner = (SocketException)e.InnerException;
                    o_packet = Packet.Error(inner.SocketErrorCode.ToString());
                }
                else
                {
                    o_packet = Packet.Error(e.Message);
                }
                EndPacket();
                return true;
            }
        }

        public void Flush()
        {
            CheckConnected();
            try
            {
                m_bufferedTcpStreamWriter.Flush();
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        public void Disconnect()
        {
            CheckNotDisconnected();
            try
            {
                if (m_tcpStream != null)
                {
                    // Send a packet to signal disconnection
                    m_bufferedTcpStreamWriter.Write(PACKETCODE_DISCONNECT);

                    // Close the stream
                    m_tcpStream.Close();
                }
                m_tcpClient.Close();
            }
            catch (IOException)
            {
                // Ignore this, seeing as we're closing the stream anyway
            }
            m_tcpClient = null;
            m_tcpStream = null;
            m_bufferedTcpStream = null;
            m_bufferedTcpStreamWriter = null;
            m_pendingTcpConnect = null;
            m_state = ConnectionState.Disconnected;
        }

        private void CheckConnected()
        {
            App.Assert(State == ConnectionState.Connected);
        }

        private void CheckNotDisconnected()
        {
            App.Assert(State != ConnectionState.Disconnected);
        }

        private int BuildHandshake()
        {
            return (App.Info.Title + App.Info.Version).StableHash();
        }

        private void SendHandshake()
        {
            int handshake = BuildHandshake();
            m_bufferedTcpStreamWriter.Write(PACKETCODE_HANDSHAKE);
            m_bufferedTcpStreamWriter.Write(handshake);
        }

        private bool VerifyHandshake(int receivedHandshake)
        {
            int handshake = BuildHandshake();
            return receivedHandshake == handshake;
        }

        private bool WaitForBytes(int size)
        {
            int bytesRemaining = size - m_receiveBufferProgress;
            App.Assert(bytesRemaining > 0);
            if (m_tcpStream.DataAvailable)
            {
                int bytesRead = m_tcpStream.Read(m_receiveBuffer, m_receiveBufferProgress, bytesRemaining);
                m_receiveBufferProgress += bytesRead;
                bytesRemaining -= bytesRead;
                if (bytesRemaining <= 0)
                {
                    m_receiveBufferReader.BaseStream.Position = 0;
                    m_receiveBufferProgress = 0;
                    return true;
                }
            }
            return false;
        }

        private void EndPacket()
        {
            m_receivedPacketType = -1;
            m_receivedMessageSize = -1;
        }
    }
}
