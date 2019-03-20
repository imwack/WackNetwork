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

        private readonly Dictionary<int, Connection> clientDict = new Dictionary<int, Connection>();
        private readonly SwapContainer<Queue<Connection>> waitClient = new SwapContainer<Queue<Connection>>();
        private readonly SwapContainer<Queue<Connection>> closeClient = new SwapContainer<Queue<Connection>>();

        private Socket listenSocket;

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
                    lock (waitClient.Lock)
                    {
                        waitClient.In.Enqueue(clientConnection);
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
            
        }

        public void Close()
        {
        }
    }
}
