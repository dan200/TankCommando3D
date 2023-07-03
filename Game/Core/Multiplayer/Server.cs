using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Core.Multiplayer
{
    internal class Server : IDisposable
    {
        private List<IConnection> m_clients;
        private List<IConnection> m_connectedClients;
        private List<IConnectionListener> m_listeners;
        private bool m_open;

        internal struct ClientCollection : IReadOnlyCollection<IConnection>
        {
            private Server m_owner;

            public int Count
            {
                get
                {
                    return m_owner.m_connectedClients.Count;
                }
            }

            public ClientCollection(Server owner)
            {
                m_owner = owner;
            }

            public List<IConnection>.Enumerator GetEnumerator()
            {
                return m_owner.m_connectedClients.GetEnumerator();
            }

            IEnumerator<IConnection> IEnumerable<IConnection>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal struct ListenerCollection : IReadOnlyCollection<IConnectionListener>
        {
            private Server m_owner;

            public int Count
            {
                get
                {
                    return m_owner.m_listeners.Count;
                }
            }

            public ListenerCollection(Server owner)
            {
                m_owner = owner;
            }

            public void Add(IConnectionListener listener)
            {
                if (m_owner.IsOpen || listener.IsOpen)
                {
                    throw new Exception("Both the listener and the server must be closed before adding a listener");
                }
                m_owner.m_listeners.Add(listener);
            }

            public void Remove(IConnectionListener listener)
            {
                if (m_owner.m_listeners.UnorderedRemove(listener))
                {
                    if (m_owner.IsOpen)
                    {
                        listener.Close();
                    }
                }
            }

            public List<IConnectionListener>.Enumerator GetEnumerator()
            {
                return m_owner.m_listeners.GetEnumerator();
            }

            IEnumerator<IConnectionListener> IEnumerable<IConnectionListener>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public ClientCollection Clients
        {
            get
            {
                return new ClientCollection(this);
            }
        }

        public ListenerCollection Listeners
        {
            get
            {
                return new ListenerCollection(this);
            }
        }

        public bool IsOpen
        {
            get
            {
                return m_open;
            }
        }

        public Server()
        {
            m_clients = new List<IConnection>();
            m_connectedClients = new List<IConnection>();
            m_listeners = new List<IConnectionListener>();
            m_open = false;
        }

        public void Dispose()
        {
            // Disconnect all clients
            DisconnectAll();

            // Close all listeners
            if (m_open)
            {
                m_open = false;
                foreach (var listener in m_listeners)
                {
                    listener.Close();
                }
            }
        }

        public void Open()
        {
            App.Assert(!m_open);

            // Open the listeners
            m_open = true;
            foreach (var listener in m_listeners)
            {
                listener.Open();
            }
        }

        public void Close(bool disconnectRemoteClients = false)
        {
            App.Assert(m_open);

            // Close the listeners
            m_open = false;
            foreach (var listener in m_listeners)
            {
                listener.Close();
            }

            // Disconnect existing remote clients
            if (disconnectRemoteClients)
            {
                DisconnectRemote();
            }
        }

        public void DisconnectAll()
        {
            // Disconnect all clients (including local)
            foreach (var client in m_clients)
            {
                if (client.State != ConnectionState.Disconnected)
                {
                    client.Disconnect();
                }
            }
            m_connectedClients.Clear();
            m_clients.Clear();
        }

        public void DisconnectRemote()
        {
            // Disconnect all remote clients
            for (int i = m_clients.Count - 1; i >= 0; --i)
            {
                var client = m_clients[i];
                if (client.State != ConnectionState.Disconnected && !client.IsLocal)
                {
                    client.Disconnect();
                    m_clients.UnorderedRemoveAt(i);
                    m_connectedClients.UnorderedRemove(client);
                }
            }
        }

        public void BroadcastMessage(ByteString message)
        {
            foreach (var client in m_connectedClients)
            {
                client.SendMessage(message);
            }
        }

        public bool ReceiveAny(out Packet o_packet, out IConnection o_sender)
        {
            // Check for new clients
            foreach (var listener in m_listeners)
            {
                IConnection connection;
                while (listener.Accept(out connection))
                {
                    m_clients.Add(connection);
                }
            }

            // Check for new packets
            for (int i = 0; i < m_clients.Count; ++i)
            {
                Packet packet;
                var client = m_clients[i];
                if (client.Receive(out packet))
                {
                    if (packet.Type == PacketType.Connect)
                    {
                        m_connectedClients.Add(client);
                    }
                    else if (packet.Type == PacketType.Disconnect || packet.Type == PacketType.Error)
                    {
                        m_connectedClients.UnorderedRemove(client);
                        m_clients.UnorderedRemoveAt(i);
                    }
                    o_packet = packet;
                    o_sender = client;
                    return true;
                }
            }

            o_packet = default(Packet);
            o_sender = default(IConnection);
            return false;
        }

        public void FlushAll()
        {
            foreach (var client in m_connectedClients)
            {
                client.Flush();
            }
        }

        public LocalConnection CreateLocalConnection()
        {
            LocalConnection serverSide, clientSide;
            CreateLocalConnectionPair(out serverSide, out clientSide);
            m_clients.Add(serverSide);
            return clientSide;
        }

        private void CreateLocalConnectionPair(out LocalConnection o_serverConnectionToClient, out LocalConnection o_clientConnectionToServer)
        {
            var clientPacketBuffer = new RingBuffer();
            var serverPacketBuffer = new RingBuffer();
            o_serverConnectionToClient = new LocalConnection(clientPacketBuffer, serverPacketBuffer);
            o_clientConnectionToServer = new LocalConnection(serverPacketBuffer, clientPacketBuffer);
        }
    }
}
