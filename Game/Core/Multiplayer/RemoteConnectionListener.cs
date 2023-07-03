using Dan200.Core.Main;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Dan200.Core.Multiplayer
{
    internal class RemoteConnectionListener : IConnectionListener
    {
        private TcpListener m_tcpListener;
        private bool m_open;

        public bool IsOpen
        {
            get
            {
                return m_open;
            }
        }

        public RemoteConnectionListener(int port)
        {
            m_tcpListener = new TcpListener(IPAddress.Any, port);
            if (!App.Debug)
            {
                m_tcpListener.ExclusiveAddressUse = true;
            }
        }

        public void Dispose()
        {
            if (m_open)
            {
                Close();
            }
        }

        public void Open()
        {
            CheckClosed();
            m_open = true;
            m_tcpListener.Start();
        }

        public void Close()
        {
            CheckOpen();
            m_tcpListener.Stop();
            m_open = false;
        }

        public bool Accept(out IConnection o_connection)
        {
            RemoteConnection connection;
            if (Accept(out connection))
            {
                o_connection = connection;
                return true;
            }
            o_connection = null;
            return false;
        }

        public bool Accept(out RemoteConnection o_connection)
        {
            CheckOpen();
            try
            {
                // See if a new client has connected
                if (m_tcpListener.Pending())
                {
                    // Get the client
                    var tcpClient = m_tcpListener.AcceptTcpClient();
                    tcpClient.NoDelay = true;
                    tcpClient.ReceiveTimeout = 10000;
                    tcpClient.SendTimeout = 10000;
                    o_connection = new RemoteConnection(tcpClient);
                    return true;
                }
            }
            catch (IOException)
            {
                // TODO: Report these errors somehow?
            }
            o_connection = null;
            return false;
        }

        private void CheckOpen()
        {
            App.Assert(m_open);
        }

        private void CheckClosed()
        {
            App.Assert(!m_open);
        }
    }
}
