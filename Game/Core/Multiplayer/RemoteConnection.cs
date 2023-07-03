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
        private const int MAX_PACKET_SIZE = 8192;
        private const int PING_INTERVAL = 5000;

        private ConnectionStatus m_status;

        private TcpClient m_tcpClient;
        private IAsyncResult m_pendingTcpConnect;
        private NetworkStream m_tcpStream;
        private BufferedStream m_bufferedTcpStream;

        private byte[] m_sendBuffer;
        private NetworkWriter m_sendBufferWriter;

        private byte[] m_receiveBuffer;
        private int m_receiveSize;
        private int m_receiveProgress;
        private NetworkReader m_receiveBufferReader;

        private int m_lastPingTime;
        private ushort m_lastPingID;
        private int m_ping;

        private Exception m_pendingError;

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

        public RemoteConnection(string hostname, int tcpPort)
        {
            // Construct buffers
            m_sendBuffer = new byte[MAX_PACKET_SIZE];
            m_sendBufferWriter = new NetworkWriter(new MemoryStream(m_sendBuffer, true));

            m_receiveBuffer = new byte[MAX_PACKET_SIZE];
            m_receiveProgress = -1;
            m_receiveSize = -1;
            m_receiveBufferReader = new NetworkReader(new MemoryStream(m_receiveBuffer, false));

            // Start connecting
            m_status = ConnectionStatus.Connecting;

            m_tcpClient = new TcpClient();
            m_tcpClient.NoDelay = true;
            m_tcpClient.ReceiveTimeout = 10000;
            m_tcpClient.SendTimeout = 10000;

            try
            {
                m_pendingTcpConnect = m_tcpClient.BeginConnect(hostname, tcpPort, null, null);
                m_tcpStream = null;
                m_bufferedTcpStream = null;
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        internal RemoteConnection(TcpClient tcpClient)
        {
            // Construct buffers
            m_sendBuffer = new byte[MAX_PACKET_SIZE + 2];
            m_sendBufferWriter = new NetworkWriter(new MemoryStream(m_sendBuffer, true));

            m_receiveBuffer = new byte[MAX_PACKET_SIZE];
            m_receiveProgress = -1;
            m_receiveSize = -1;
            m_receiveBufferReader = new NetworkReader(new MemoryStream(m_receiveBuffer, false));

            // Assume already connected
            m_status = ConnectionStatus.Connecting;
            m_tcpClient = tcpClient;

            try
            {
                // Open the stream
                m_tcpStream = tcpClient.GetStream();
                m_bufferedTcpStream = new BufferedStream(m_tcpStream);

                // Send the handshake and the first ping
                SendHandshake();
                SendPing();
                m_bufferedTcpStream.Flush();
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        public virtual void Dispose()
        {
            if (m_status != ConnectionStatus.Disconnected)
            {
                Disconnect();
            }
        }

        public void Send(IMessage message)
        {
            // Check state
            CheckConnected();

            // Leave space for the size
            m_sendBufferWriter.Position = 0;
            m_sendBufferWriter.Write((ushort)0);

            // Encode the message
            MessageFactory.Encode(message, m_sendBufferWriter);
            int size = (int)m_sendBufferWriter.Position - 2;

            // Go back and fill the size in
            m_sendBufferWriter.Position = 0;
            m_sendBufferWriter.Write((ushort)(size + 3));

            // Transmit the packet
            try
            {
                // Include the size
                m_bufferedTcpStream.Write(m_sendBuffer, 0, 2 + size);
            }
            catch (IOException e)
            {
                m_pendingError = e;
            }
        }

        public void SendPing()
        {
            // Build the packet
            m_sendBufferWriter.Position = 0;
            m_sendBufferWriter.Write((ushort)1);
            m_sendBufferWriter.Write(++m_lastPingID);

            // Send the packet
            m_lastPingTime = Environment.TickCount;
            m_bufferedTcpStream.Write(m_sendBuffer, 0, 4);
        }

        private void SendPong(ushort id)
        {
            // Build the packet
            m_sendBufferWriter.Position = 0;
            m_sendBufferWriter.Write((ushort)2);
            m_sendBufferWriter.Write(id);

            // Send the packet
            m_bufferedTcpStream.Write(m_sendBuffer, 0, 4);
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

                        // Open the streak
                        m_tcpStream = m_tcpClient.GetStream();
                        m_bufferedTcpStream = new BufferedStream(m_tcpStream);

                        // Send the handshake and the first ping
                        SendHandshake();
                        SendPing();
                        m_bufferedTcpStream.Flush();
                    }
                    else
                    {
                        // Not connected yet
                        o_packet = default(Packet);
                        return false;
                    }
                }

                // Check TCP packets
                if (m_tcpStream.DataAvailable)
                {
                    if (m_status == ConnectionStatus.Connecting)
                    {
                        // Verify the handshake
                        int result = VerifyHandshake();
                        if (result < 0)
                        {
                            // Disconnect
                            Disconnect();
                            o_packet = Packet.Disconnect;
                            return true;
                        }
                        else
                        {
                            // Return a connection packet
                            m_status = ConnectionStatus.Connected;
                            o_packet = Packet.Connect;
                            return true;
                        }
                    }

                    if (m_receiveProgress < 0)
                    {
                        // Start a new packet:
                        // Get the size
                        int header = ReadShort();
                        if (header <= 0)
                        {
                            // Disconnect
                            Disconnect();
                            o_packet = Packet.Disconnect;
                            return true;
                        }
                        else if (header == 1)
                        {
                            // Ping
                            int id = ReadShort();
                            if (id < 0)
                            {
                                // Disconnect
                                Disconnect();
                                o_packet = Packet.Disconnect;
                                return true;
                            }
                            else
                            {
                                // Reply to the ping
                                SendPong((ushort)id);
                                o_packet = Packet.Ping;
                                return true;
                            }
                        }
                        else if (header == 2)
                        {
                            // Pong
                            int id = ReadShort();
                            if (id < 0)
                            {
                                // Disconnect
                                Disconnect();
                                o_packet = Packet.Disconnect;
                                return true;
                            }
                            else
                            {
                                // Measure the ping
                                if (id == m_lastPingID)
                                {
                                    var now = Environment.TickCount;
                                    m_ping = (now - m_lastPingTime);
                                }
                                o_packet = Packet.Pong;
                                return true;
                            }
                        }

                        // Read some bytes
                        int size = header - 3;
                        if (size > MAX_PACKET_SIZE)
                        {
                            throw new IOException("Message too large");
                        }

                        var bytesRead = m_tcpStream.Read(m_receiveBuffer, 0, size);
                        if (bytesRead == 0)
                        {
                            Disconnect();
                            o_packet = Packet.Disconnect;
                            return true;
                        }

                        // See if we got the whole packet or not
                        if (bytesRead == size)
                        {
                            // If so, decode it immediately
                            m_receiveBufferReader.Position = 0;
                            var message = MessageFactory.Decode(m_receiveBufferReader);
                            o_packet = new Packet(message);
                            return true;
                        }
                        else
                        {
                            // Otherwise, record the progress so we can resume next frame
                            m_receiveSize = size;
                            m_receiveProgress = bytesRead;
                        }
                    }
                    else
                    {
                        // Continue an existing packet:
                        // Read some bytes
                        var bytesRead = m_tcpStream.Read(m_receiveBuffer, m_receiveProgress, m_receiveSize - m_receiveProgress);
                        if (bytesRead == 0)
                        {
                            Disconnect();
                            o_packet = Packet.Disconnect;
                            return true;
                        }

                        // See if we completed the packet
                        if (bytesRead + m_receiveProgress == m_receiveSize)
                        {
                            // If so, decode it immediately
                            m_receiveBufferReader.Position = 0;
                            var message = MessageFactory.Decode(m_receiveBufferReader);
                            o_packet = new Packet(message);

                            // Reset the receive progress
                            m_receiveProgress = -1;
                            m_receiveSize = -1;
                            return true;
                        }
                        else
                        {
                            // Otherwise, record the progress so we can resume next frame
                            m_receiveProgress += bytesRead;
                        }
                    }
                }

                // No packets received
                // See if it's time to send a ping
                if ((Environment.TickCount - m_lastPingTime) > PING_INTERVAL)
                {
                    SendPing();
                }
                o_packet = default(Packet);
                return false;
            }
            catch (SocketException e)
            {
                // Return a socket error
                Disconnect();
                o_packet = Packet.Error(e.SocketErrorCode.ToString());
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
                return true;
            }
        }

        public void Flush()
        {
            if (m_status == ConnectionStatus.Connected)
            {
                try
                {
                    m_bufferedTcpStream.Flush();
                }
                catch (IOException e)
                {
                    m_pendingError = e;
                }
            }
        }

        public void Disconnect()
        {
            CheckNotDisconnected();
            try
            {
                if (m_tcpStream != null)
                {
                    // Send zero to signal disconnection
                    m_sendBufferWriter.Position = 0;
                    m_sendBufferWriter.Write((ushort)0);
                    m_tcpStream.Write(m_sendBuffer, 0, 2);

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
            m_pendingTcpConnect = null;
            m_status = ConnectionStatus.Disconnected;
        }

        private void CheckConnected()
        {
            App.Assert(Status == ConnectionStatus.Connected);
        }

        private void CheckNotDisconnected()
        {
            App.Assert(Status == ConnectionStatus.Disconnected);
        }

        private int BuildHandshake()
        {
            m_sendBufferWriter.Position = 0;
			m_sendBufferWriter.Write((App.Info.Title + App.Info.Version).StableHash());
            return 4;
        }

        private void SendHandshake()
        {
            int size = BuildHandshake();
            m_bufferedTcpStream.Write(m_sendBuffer, 0, size);
        }

        private int VerifyHandshake()
        {
            int size = BuildHandshake();
            for (int i = 0; i < size; ++i)
            {
                var b = m_tcpStream.ReadByte();
                if (b < 0)
                {
                    return -1;
                }

                if (b != m_sendBuffer[i])
                {
                    throw new IOException("Handshake failed");
                }
            }
            return 0;
        }

        private int ReadShort()
        {
            var b1 = m_tcpStream.ReadByte();
            if (b1 < 0)
            {
                return -1;
            }

            var b2 = m_tcpStream.ReadByte();
            if (b2 < 0)
            {
                return -1;
            }

            return (b1 | (b2 << 8));
        }
    }
}
