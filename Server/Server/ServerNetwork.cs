using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Util;

namespace Server
{
    public class ServerNetwork : INetwork
    {
        private int currentConnectionId = 1;
        private int minConnectionId = 1;
        private int maxConnectionId = int.MaxValue;
        private Socket listenSocket;

        private readonly Dictionary<int, Connection> clientDict = new Dictionary<int, Connection>();
        private readonly SwapContainer<Queue<Connection>> waitClientQueue = new SwapContainer<Queue<Connection>>();
        private readonly SwapContainer<Queue<Connection>> closeClientQueue = new SwapContainer<Queue<Connection>>();

        public delegate void ServerNetworkClientConnectedHandler(IRemote remote);
        public delegate void ServerNetworkClientDisconnectedHandler(IRemote remote);
        public delegate void ServerNetworkClientMessageReceivedHandler(IRemote remote, Message msg);

        public ServerNetworkClientConnectedHandler OnClientConnected;
        public ServerNetworkClientDisconnectedHandler OnClientDisconnected;
        public ServerNetworkClientMessageReceivedHandler OnReceiveClientMessage;


        public ServerNetwork(int port)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //current only tcp
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            listenSocket.Listen(10);
        }
        public void BeginAccept()
        {
            listenSocket.BeginAccept(OnAcceptCallback, null);
        }

        //on client be accepted
        private void OnAcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = listenSocket.EndAccept(ar);
                clientSocket.SendTimeout = 500;
                clientSocket.ReceiveTimeout = 500;
                clientSocket.NoDelay = true;

                var id = GetConnectId();
                if (id != -1)
                {
                    var clientConnection = new Connection(clientSocket, id);
                    lock (waitClientQueue.Lock)
                    {
                        waitClientQueue.In.Enqueue(clientConnection);
                    }
                    Logger.Info("OnAcceptCallback add wait client id={0}", id);
                }
                // continue to accept
                listenSocket.BeginAccept(OnAcceptCallback, null);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

        private int GetConnectId()
        {
            int id = currentConnectionId;
            if (currentConnectionId + 1 > maxConnectionId)
            {
                currentConnectionId = minConnectionId;
            }
            else
            {
                ++currentConnectionId;
            }
            if (clientDict.ContainsKey(id))
            {
                Logger.Error("GetConnectId alread exist connection id = {0}", id);
                return -1;
            }
            return id;
        }
        public void OneLoop()
        {
            try
            {
                // todo heartbeat

                ProcessClientConnectMessageQueue();
                RefreshClientList();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void Close()
        {
            listenSocket?.Close();
            Logger.Debug("ServerSocket.Dispose");
        }

        private void ProcessClientConnectMessageQueue()
        {
            foreach (var clientConnection in clientDict.Values)
            {
                clientConnection.ProcessMessageQueue((c, msg) =>
                {
                    OnReceiveClientMessage?.Invoke(c, msg);
                });
            }
        }

        private void RefreshClientList()
        {
            // accept client connection
            if (waitClientQueue.Out.Count == 0)
            {
                waitClientQueue.Swap();

                foreach (var clientConnection in waitClientQueue.Out)
                {
                    // todo race condition, when add conn to clientConnectorsDict
                    if (clientDict.ContainsKey(clientConnection.connectionId))
                    {
                        Logger.Warn("ServerNetwork.RefreshClientList connector exist id={0}", clientConnection.connectionId);
                        return;
                    }

                    clientDict.Add(clientConnection.connectionId, clientConnection);
                    OnClientConnected?.Invoke(clientConnection);

                    // client.sysSocket.BeginReceive
                    clientConnection.BeginReceive();
                }
                waitClientQueue.Out.Clear();
            }

            // remove client connection
            if (closeClientQueue.Out.Count == 0)
            {
                closeClientQueue.Swap();
                foreach (var clientConnection in closeClientQueue.Out)
                {
                    if (clientDict.ContainsKey(clientConnection.connectionId))
                    {
                        clientDict.Remove(clientConnection.connectionId);
                        clientConnection.Close();
                        OnClientDisconnected?.Invoke(clientConnection);
                    }
                }
                closeClientQueue.Out.Clear();
            }

            foreach (var client in clientDict.Values)
            {
                client.Send();
                if (client.DefferedClose)
                {
                    // close any clients which failed sending data
                    CloseClient(client, NetworkCloseMode.DefferedClose);
                }
            }
        }

        private void CloseClient(Connection connection, NetworkCloseMode mode)
        {
            if (connection == null)
            {
                Logger.Warn("ServerNetwork.CloseClient socket is null");
                return;
            }
            lock (closeClientQueue.Lock)
            {
                closeClientQueue.In.Enqueue(connection);
                Logger.Debug("ServerNetwork.CloseClient connectId={0}", mode, connection.connectionId);
            }
        }

        public enum NetworkCloseMode
        {
            HeartbeatTimeout = 1,
            DefferedClose = 2,
            Dispose = 3,
        }
    }
}
